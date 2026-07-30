using Entity.Simulation;
using Entity.Simulation.Shape;
using Hotfix.Simulation.Abstractions.Config;

namespace Hotfix.Simulation.Abstractions;

public abstract class SimBase : ISimulation
{
    /// <summary>模拟器内部状态实体（Entity 层，跨热更）。逻辑层只读写不持有副本。</summary>
    public SimStateEntity SimState { get; }

    public SimBase(SimulateConfig config, SimStateEntity simState)
    {
        _config = config;
        SimState = simState;
    }

    public SimulateConfig Config => _config;
    protected SimulateConfig _config;
    public void Run()
    {
        if (SimState.State != Entity.Simulation.SimState.Create)
        {
            throw new SimStateException(SimState.State, Entity.Simulation.SimState.Create, nameof(Run));
        }
        SimState.State = Entity.Simulation.SimState.Running;
    }

    public void Stop()
    {
        if (SimState.State != Entity.Simulation.SimState.Running)
        {
            throw new SimStateException(SimState.State, Entity.Simulation.SimState.Running, nameof(Stop));
        }
        SimState.State = Entity.Simulation.SimState.Stop;
    }

    public abstract void SimTick();

    /// <summary>
    /// 检查世界坐标是否合法：在地图边界内，且不在任何形状内部。
    /// </summary>
    public bool IsCoordValid(Vec2D<uint> point)
    {
        if (point.X > Config.World.Map.X || point.Y > Config.World.Map.Y)
            return false;

        foreach (var shape in SimState.Shapes)
        {
            if (shape.PointIsInShape(point))
                return false;
        }

       return true;
   }

    /// <summary>默认安全半径（px），确保生成坐标与障碍物保持此距离。</summary>
    public const uint DefaultSpawnRadius = 20;

    /// <summary>默认最大重试次数。</summary>
    public const int DefaultSpawnMaxAttempts = 100;

    /// <summary>
    /// 在世界范围内随机生成一个合法出生坐标。
    /// 合法条件：坐标在以 radius 为半径的圆范围内不越过地图边界，且不与任何形状相交。
    /// </summary>
    /// <param name="coord">生成的合法坐标，失败时为 default</param>
    /// <param name="radius">安全半径（默认 20px）</param>
    /// <param name="maxAttempts">最大重试次数（默认 100）</param>
    /// <returns>成功返回 true，超过重试次数返回 false</returns>
    public abstract bool TryGenerateCoord(out Vec2D<uint> coord, uint radius = DefaultSpawnRadius, int maxAttempts = DefaultSpawnMaxAttempts);

    /// <summary>
    /// 检查以 <paramref name="center"/> 为圆心、<paramref name="radius"/> 为半径的
    /// 逻辑圆是否合法：完全在地图边界内，且不与任何形状相交（含圆在形状内部）。
    /// </summary>
    public bool IsCircleValid(Vec2D<uint> center, uint radius)
    {
        ulong uRadius = radius;
        if (center.X < uRadius || center.X + uRadius > Config.World.Map.X) return false;
        if (center.Y < uRadius || center.Y + uRadius > Config.World.Map.Y) return false;

        foreach (var shape in SimState.Shapes)
        {
            if (CircleOverlapsShape(shape, center, radius).Hit)
                return false;
        }

        return true;
    }

    /// <summary>
    /// 判定圆是否与多边形相交（含内含），返回碰撞信息对齐客户端 circleVsPolygon。
    /// 内部使用 ×1000 定点数精度计算，对齐客户端 FP=1000 语义。
    /// </summary>
    private static CircleHit CircleOverlapsShape(AbstShape shape, Vec2D<uint> c, long r)
    {
        var verts = shape.Vertices;
        long cx1000 = (long)c.X * 1000;
        long cy1000 = (long)c.Y * 1000;
        long r1000 = r * 1000;
        int n = verts.Count;
        if (n < 3) return CircleHit.NoHit;

        // 逐边找最近点（×1000 精度）
        long bestDSq = -1;
        long bestDx1000 = 0, bestDy1000 = 0;
        for (int i = 0; i < n; i++)
        {
            var a = verts[i];
            var b = verts[(i + 1) % n];
            var p = ClosestPointOnSegment(cx1000, cy1000,
                (long)a.X * 1000, (long)a.Y * 1000,
                (long)b.X * 1000, (long)b.Y * 1000);
            long dx1000 = cx1000 - p.X;
            long dy1000 = cy1000 - p.Y;
            long dSq = dx1000 * dx1000 + dy1000 * dy1000;
            if (bestDSq < 0 || dSq < bestDSq)
            {
                bestDSq = dSq;
                bestDx1000 = dx1000;
                bestDy1000 = dy1000;
            }
        }

        long d = SqrtU64(bestDSq);
        bool inside = shape.PointIsInShape(c);

        if (d > r1000 && !inside) return CircleHit.NoHit;

        if (d == 0 && !inside) return new CircleHit { Hit = true, Nx = 1000, Ny = 0, Penetration = 0 };

        if (inside)
        {
            const long EPS_OUT = 10;
            long minExit = -1;
            long exitNx = 0, exitNy = 0;
            for (int i = 0; i < n; i++)
            {
                var a = verts[i];
                var b = verts[(i + 1) % n];
                long ex = (long)b.X - a.X;
                long ey = (long)b.Y - a.Y;
                long elen = SqrtU64(ex * ex + ey * ey);
                if (elen == 0) continue;
                ex = ex * 1000 / elen;
                ey = ey * 1000 / elen;

                long nx = -ey;
                long ny = ex;
                long mx1000 = ((long)a.X * 1000 + (long)b.X * 1000 + 1) >> 1;
                long my1000 = ((long)a.Y * 1000 + (long)b.Y * 1000 + 1) >> 1;
                // ×1000 精度偏移，对齐客户端 idiv(nx * EPS_OUT_FP, FP)
                long off_x1000 = mx1000 + (nx * EPS_OUT) / 1000;
                long off_y1000 = my1000 + (ny * EPS_OUT) / 1000;
                if (shape.PointIsInShape1000(off_x1000, off_y1000))
                {
                    nx = -nx;
                    ny = -ny;
                }

                var p = ClosestPointOnSegment(cx1000, cy1000,
                    (long)a.X * 1000, (long)a.Y * 1000,
                    (long)b.X * 1000, (long)b.Y * 1000);
                long sdist = ((p.X - cx1000) * nx + (p.Y - cy1000) * ny) / 1000;
                long exit = sdist + r1000;
                if (minExit < 0 || exit < minExit)
                {
                    minExit = exit;
                    exitNx = nx;
                    exitNy = ny;
                }
            }

            if (minExit >= 0)
            {
                return new CircleHit { Hit = true, Nx = exitNx, Ny = exitNy, Penetration = minExit };
            }

            long fd = d == 0 ? 1000 : d;
            return new CircleHit { Hit = true, Nx = -bestDx1000 * 1000 / fd, Ny = -bestDy1000 * 1000 / fd, Penetration = r1000 + d };
        }

        return new CircleHit
        {
            Hit = true,
            Nx = bestDx1000 * 1000 / d,
            Ny = bestDy1000 * 1000 / d,
            Penetration = r1000 - d
        };
    }

    /// <summary>整数平方根（向下取整），对齐客户端 isqrt。</summary>
    private static long SqrtU64(long n)
    {
        if (n < 2) return n;
        long bit = 1;
        while (bit * bit <= n) bit <<= 1;
        bit >>= 1;
        long r = 0;
        while (bit > 0)
        {
            long t = r + bit;
            if (t * t <= n) r = t;
            bit >>= 1;
        }
        return r;
    }

    /// <summary>
    /// 线段 a→b 上离点 p 最近的点（含端点）。
    /// 对齐客户端 closestPointOnSegment，使用 ×1000 定点数保证 t 精度。
    /// </summary>
    private static Vec2D<long> ClosestPointOnSegment(long px, long py, long ax, long ay, long bx, long by)
    {
        long abx = bx - ax;
        long aby = by - ay;
        long abSq = abx * abx + aby * aby;
        if (abSq == 0) return new Vec2D<long>(ax, ay);
        long dot = (px - ax) * abx + (py - ay) * aby;
        long t = (dot * 1000) / abSq;
        if (t < 0) t = 0;
        else if (t > 1000) t = 1000;
        return new Vec2D<long>(ax + (t * abx) / 1000, ay + (t * aby) / 1000);
    }
}

/// <summary>圆与多边形碰撞结果，对齐客户端 CircleHit。</summary>
public struct CircleHit
{
    /// <summary>是否发生碰撞。</summary>
    public bool Hit;
    /// <summary>推出法线 x（单位向量 ×1000）。</summary>
    public long Nx;
    /// <summary>推出法线 y（单位向量 ×1000）。</summary>
    public long Ny;
    /// <summary>推出深度（px，×1000）。</summary>
    public long Penetration;

    public static readonly CircleHit NoHit = new() { Hit = false, Nx = 0, Ny = 0, Penetration = 0 };
}

