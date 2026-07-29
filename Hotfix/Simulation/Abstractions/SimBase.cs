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
        if (point.X >= Config.World.Map.X || point.Y >= Config.World.Map.Y)
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
        if (center.X < uRadius || center.X + uRadius >= Config.World.Map.X) return false;
        if (center.Y < uRadius || center.Y + uRadius >= Config.World.Map.Y) return false;

        long r2 = (long)radius * radius;
        foreach (var shape in SimState.Shapes)
        {
            if (CircleOverlapsShape(shape, center, r2))
                return false;
        }

        return true;
    }

   /// <summary>
   /// 判定圆（圆心 <paramref name="c"/>、半径平方 <paramref name="r2"/>）
    /// 是否与多边形 <paramref name="shape"/> 相交（含内含）。
    /// </summary>
    private static bool CircleOverlapsShape(AbstShape shape, Vec2D<uint> c, long r2)
    {
        // 圆心在形状内部 → 相交
        if (shape.PointIsInShape(c)) return true;

        var verts = shape.Vertices;
        long cx = c.X, cy = c.Y;
        int n = verts.Count;

        for (int i = 0; i < n; i++)
        {
            int j = (i + 1) % n;
            long ax = verts[i].X, ay = verts[i].Y;
            long bx = verts[j].X, by = verts[j].Y;

            long abx = bx - ax, aby = by - ay;
            long apx = cx - ax, apy = cy - ay;
            long dot = apx * abx + apy * aby;
            long len2 = abx * abx + aby * aby;

            long dx, dy;
            if (len2 == 0 || dot <= 0)
            {
                dx = cx - ax; dy = cy - ay;
            }
            else if (dot >= len2)
            {
                dx = cx - bx; dy = cy - by;
            }
            else
            {
                // 用叉积算点到直线距离的平方：|cross|^2 / len2 ≤ r2
                long cross = abx * apy - aby * apx;
                if (cross * cross <= r2 * len2) return true;
                continue;
            }

            if (dx * dx + dy * dy <= r2) return true;
        }

        return false;
    }
}
