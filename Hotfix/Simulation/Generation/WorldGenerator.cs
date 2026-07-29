using System;
using System.Collections.Generic;
using Entity.Simulation;
using Entity.Simulation.Shape;
using Fantasy;
using Hotfix.Simulation.Abstractions.Config;

namespace Hotfix.Simulation.Generation;

/// <summary>
/// 世界形状生成模块。从 <see cref="ShapeGenConfig"/> 读取参数，
/// 向 <see cref="SimStateEntity.Shapes"/> 灌入互不重叠的形状。
/// 与模拟器版本解耦，任何 <see cref="ISimulation"/> 实现均可调用。
/// </summary>
public static class WorldGenerator
{
    /// <summary>
    /// 生成 world 的形状集合。调用方保证 Generate 在 Run() 之前只调用一次。
    /// </summary>
    public static void Generate(SimStateEntity state, WorldConfig world, int seed)
    {
        int target = world.ShapeGen.TotalCount;
        int w = (int)world.Map.X;
        int h = (int)world.Map.Y;
        long targetArea2 = world.ShapeGen.TargetShapeArea2;

        var rng = new Random(seed);
        int attempts = 0;
        int triGenerated = 0, concaveGenerated = 0, convexGenerated = 0;

        int[] vertexPool = world.ShapeGen.VertexPool;

        var shapes = state.Shapes;
        while (shapes.Count < target && attempts < world.ShapeGen.MaxGenerateAttempts)
        {
            attempts++;

            AbstShape? shape = null;
            int n = vertexPool[rng.Next(vertexPool.Length)];

            if (n == 3)
            {
                var tri = RandomTriangle(rng, w, h, targetArea2, world.ShapeGen);
                if (tri == null || tri.IsDegenerate) continue;
                shape = tri;
            }
            else
            {
                bool isConcave = n >= 5 && rng.Next(2) == 0;
                shape = isConcave
                    ? RandomConcavePolygon(rng, w, h, targetArea2, n, world.ShapeGen)
                    : RandomConvexPolygon(rng, w, h, targetArea2, n, world.ShapeGen);
                if (shape == null) continue;
            }

            // 与已有任一形状重叠则丢弃重试
            bool overlap = false;
            foreach (var exist in shapes)
            {
                if (shape.OverlapsWith(exist))
                {
                    overlap = true;
                    break;
                }
            }

            if (!overlap)
            {
                shapes.Add(shape);
                switch (shape)
                {
                    case Triangle: triGenerated++; break;
                    case ConcavePolygon: concaveGenerated++; break;
                    case ConvexPolygon: convexGenerated++; break;
                }
            }
        }

        Log.Info($"[WorldGenerator] 生成世界完成: 形状 {shapes.Count} 个 (三角 {triGenerated}, 凹 {concaveGenerated}, 凸 {convexGenerated}, 试探 {attempts} 次)");

        for (int i = 0; i < shapes.Count; i++)
        {
            var verts = shapes[i].Vertices;
            var sb = new global::System.Text.StringBuilder();
            for (int v = 0; v < verts.Count; v++)
            {
                if (v > 0) sb.Append(' ');
                sb.Append('(').Append(verts[v].X).Append(',').Append(verts[v].Y).Append(')');
            }
            Log.Info($"[WorldGenerator] 形状#{i} {shapes[i].GetType().Name} 顶点: {sb}");
        }
    }

    /// <summary>
    /// 在地图范围内生成一个三角形，面积尽量靠近 2×targetArea2。
    /// 流程：散布骨架 → 鞋带算骨架 2×面积 → 整数缩放 k → 随机平移落位。
    /// </summary>
    private static Triangle? RandomTriangle(Random rng, int w, int h, long targetArea2, ShapeGenConfig cfg)
    {
        long ax = rng.Next(cfg.TriangleMinSpan, cfg.TriangleMaxSpan + 1);
        long ay = rng.Next(cfg.TriangleMinSpan, cfg.TriangleMaxSpan + 1);
        long bx = rng.Next(cfg.TriangleMinSpan, cfg.TriangleMaxSpan + 1);
        long by = rng.Next(cfg.TriangleMinSpan, cfg.TriangleMaxSpan + 1);
        long cx = rng.Next(cfg.TriangleMinSpan, cfg.TriangleMaxSpan + 1);
        long cy = rng.Next(cfg.TriangleMinSpan, cfg.TriangleMaxSpan + 1);

        long a0_2 = Math.Abs((bx - ax) * (cy - ay) - (cx - ax) * (by - ay));
        if (a0_2 == 0) return null;

        if (MinAngle(ax, ay, bx, by, cx, cy) < cfg.MinInteriorAngleRad)
            return null;

        int k = PickScale(targetArea2, a0_2);

        long spanMaxX = Math.Max(Math.Max(ax, bx), cx) * k;
        long spanMaxY = Math.Max(Math.Max(ay, by), cy) * k;
        if (spanMaxX > w - 2 * cfg.MapMargin || spanMaxY > h - 2 * cfg.MapMargin)
        {
            k = 1;
            spanMaxX = Math.Max(Math.Max(ax, bx), cx);
            spanMaxY = Math.Max(Math.Max(ay, by), cy);
        }

        int ox = rng.Next(cfg.MapMargin, Math.Max(cfg.MapMargin + 1, (int)(w - cfg.MapMargin - spanMaxX)));
        int oy = rng.Next(cfg.MapMargin, Math.Max(cfg.MapMargin + 1, (int)(h - cfg.MapMargin - spanMaxY)));

        uint sax = (uint)Math.Clamp((int)(ox + ax * k), cfg.MapMargin, w - cfg.MapMargin);
        uint say = (uint)Math.Clamp((int)(oy + ay * k), cfg.MapMargin, h - cfg.MapMargin);
        uint sbx = (uint)Math.Clamp((int)(ox + bx * k), cfg.MapMargin, w - cfg.MapMargin);
        uint sby = (uint)Math.Clamp((int)(oy + by * k), cfg.MapMargin, h - cfg.MapMargin);
        uint scx = (uint)Math.Clamp((int)(ox + cx * k), cfg.MapMargin, w - cfg.MapMargin);
        uint scy = (uint)Math.Clamp((int)(oy + cy * k), cfg.MapMargin, h - cfg.MapMargin);

        return new Triangle(new Vec2D<uint>(sax, say), new Vec2D<uint>(sbx, sby), new Vec2D<uint>(scx, scy));
    }

    /// <summary>
    /// 随机生成凸 N 边形。圆周散点 + 角度排序保证凸性，面积缩放靠 PickScale 择优。
    /// </summary>
    private static ConvexPolygon? RandomConvexPolygon(Random rng, int w, int h, long targetArea2, int n, ShapeGenConfig cfg)
    {
        double cx = cfg.TriangleMaxSpan * 0.5;
        double cy = cfg.TriangleMaxSpan * 0.5;
        double baseR = cfg.TriangleMaxSpan * 0.4;

        var pts = new (double x, double y, double ang)[n];
        for (int i = 0; i < n; i++)
        {
            double ang = 2.0 * Math.PI * i / n + rng.NextDouble() * (Math.PI / (n * 2));
            double r = baseR * (0.75 + 0.25 * rng.NextDouble());
            pts[i] = (cx + r * Math.Cos(ang), cy + r * Math.Sin(ang), ang);
        }
        Array.Sort(pts, (a, b) => a.ang.CompareTo(b.ang));

        double minX = double.MaxValue, minY = double.MaxValue;
        for (int i = 0; i < n; i++) { if (pts[i].x < minX) minX = pts[i].x; if (pts[i].y < minY) minY = pts[i].y; }
        var skeleton = new Vec2D<int>[n];
        for (int i = 0; i < n; i++)
            skeleton[i] = new Vec2D<int>((int)Math.Round(pts[i].x - minX), (int)Math.Round(pts[i].y - minY));

        int sign = 0;
        for (int i = 0; i < n; i++)
        {
            int j = (i + 1) % n, kn = (i + 2) % n;
            long cr = (long)(skeleton[j].X - skeleton[i].X) * (skeleton[kn].Y - skeleton[j].Y)
                     - (long)(skeleton[j].Y - skeleton[i].Y) * (skeleton[kn].X - skeleton[j].X);
            if (cr > 0) { if (sign < 0) return null; sign = 1; }
            else if (cr < 0) { if (sign > 0) return null; sign = -1; }
            else return null;
        }

        long a0_2 = ShoelaceInt2(skeleton);
        if (a0_2 == 0) return null;
        int k = PickScale(targetArea2, a0_2);

        long spanMaxX = 0, spanMaxY = 0;
        for (int i = 0; i < n; i++)
        {
            long sxk = (long)skeleton[i].X * k, syk = (long)skeleton[i].Y * k;
            if (sxk > spanMaxX) spanMaxX = sxk;
            if (syk > spanMaxY) spanMaxY = syk;
        }
        if (spanMaxX > w - 2 * cfg.MapMargin || spanMaxY > h - 2 * cfg.MapMargin) return null;

        int ox = rng.Next(cfg.MapMargin, Math.Max(cfg.MapMargin + 1, (int)(w - cfg.MapMargin - spanMaxX)));
        int oy = rng.Next(cfg.MapMargin, Math.Max(cfg.MapMargin + 1, (int)(h - cfg.MapMargin - spanMaxY)));

        var verts = new List<Vec2D<uint>>(n);
        for (int i = 0; i < n; i++)
        {
            uint x = (uint)Math.Clamp((int)(ox + (long)skeleton[i].X * k), cfg.MapMargin, w - cfg.MapMargin);
            uint y = (uint)Math.Clamp((int)(oy + (long)skeleton[i].Y * k), cfg.MapMargin, h - cfg.MapMargin);
            verts.Add(new Vec2D<uint>(x, y));
        }
        return new ConvexPolygon(verts);
    }

    /// <summary>
    /// 随机生成凹 N 边形。圆周散点后将一个顶点压向质心制造凹角。
    /// </summary>
    private static ConcavePolygon? RandomConcavePolygon(Random rng, int w, int h, long targetArea2, int n, ShapeGenConfig cfg)
    {
        double cx = cfg.TriangleMaxSpan * 0.5, cy = cfg.TriangleMaxSpan * 0.5;
        double baseR = cfg.TriangleMaxSpan * 0.4;

        var pts = new (double x, double y, double ang)[n];
        for (int i = 0; i < n; i++)
        {
            double ang = 2.0 * Math.PI * i / n + rng.NextDouble() * (Math.PI / n);
            double r = baseR * (0.65 + 0.35 * rng.NextDouble());
            pts[i] = (cx + r * Math.Cos(ang), cy + r * Math.Sin(ang), ang);
        }
        Array.Sort(pts, (a, b) => a.ang.CompareTo(b.ang));

        double minX = double.MaxValue, minY = double.MaxValue;
        for (int i = 0; i < n; i++) { if (pts[i].x < minX) minX = pts[i].x; if (pts[i].y < minY) minY = pts[i].y; }
        var skeleton = new Vec2D<int>[n];
        long sumX = 0, sumY = 0;
        for (int i = 0; i < n; i++)
        {
            int ix = (int)Math.Round(pts[i].x - minX), iy = (int)Math.Round(pts[i].y - minY);
            skeleton[i] = new Vec2D<int>(ix, iy);
            sumX += ix; sumY += iy;
        }

        double ctrX = (double)sumX / n, ctrY = (double)sumY / n;
        int notch = rng.Next(n);
        double push = 0.3 + 0.5 * rng.NextDouble();
        int nx = (int)Math.Round(skeleton[notch].X * (1.0 - push) + ctrX * push);
        int ny = (int)Math.Round(skeleton[notch].Y * (1.0 - push) + ctrY * push);
        if (nx < 0) nx = 0; if (ny < 0) ny = 0;
        skeleton[notch] = new Vec2D<int>(nx, ny);

        if (!IsConcaveInt(skeleton)) return null;

        long a0_2 = ShoelaceInt2(skeleton);
        if (a0_2 == 0) return null;
        int k = PickScale(targetArea2, a0_2);

        long spanMaxX = 0, spanMaxY = 0;
        for (int i = 0; i < n; i++)
        {
            long sxk = (long)skeleton[i].X * k, syk = (long)skeleton[i].Y * k;
            if (sxk > spanMaxX) spanMaxX = sxk;
            if (syk > spanMaxY) spanMaxY = syk;
        }
        if (spanMaxX > w - 2 * cfg.MapMargin || spanMaxY > h - 2 * cfg.MapMargin) return null;

        int ox = rng.Next(cfg.MapMargin, Math.Max(cfg.MapMargin + 1, (int)(w - cfg.MapMargin - spanMaxX)));
        int oy = rng.Next(cfg.MapMargin, Math.Max(cfg.MapMargin + 1, (int)(h - cfg.MapMargin - spanMaxY)));

        var verts = new List<Vec2D<uint>>(n);
        for (int i = 0; i < n; i++)
        {
            uint x = (uint)Math.Clamp((int)(ox + (long)skeleton[i].X * k), cfg.MapMargin, w - cfg.MapMargin);
            uint y = (uint)Math.Clamp((int)(oy + (long)skeleton[i].Y * k), cfg.MapMargin, h - cfg.MapMargin);
            verts.Add(new Vec2D<uint>(x, y));
        }

        var poly = new ConcavePolygon(verts);
        return poly.IsConcaveShape ? poly : null;
    }

    // ---- 纯工具方法（无 config 依赖）----

    private static int PickScale(long targetArea2, long a0_2)
    {
        double root = Math.Sqrt((double)targetArea2 / a0_2);
        int kf = (int)Math.Floor(root);
        if (kf < 1) kf = 1;
        int kc = kf + 1;
        long af = a0_2 * (long)kf * kf, ac = a0_2 * (long)kc * kc;
        return Math.Abs(af - targetArea2) <= Math.Abs(ac - targetArea2) ? kf : kc;
    }

    private static double MinAngle(long ax, long ay, long bx, long by, long cx, long cy)
    {
        return Math.Min(VertexAngle(ax, ay, bx, by, cx, cy),
               Math.Min(VertexAngle(bx, by, ax, ay, cx, cy),
                        VertexAngle(cx, cy, ax, ay, bx, by)));
    }

    private static double VertexAngle(long px, long py, long ax, long ay, long bx, long by)
    {
        long ux = ax - px, uy = ay - py, vx = bx - px, vy = by - py;
        return Math.Abs(Math.Atan2((double)(ux * vy - uy * vx), (double)(ux * vx + uy * vy)));
    }

    private static long ShoelaceInt2(Vec2D<int>[] t)
    {
        long sum = 0; int n = t.Length;
        for (int i = 0; i < n; i++) { int j = (i + 1) % n; sum += (long)t[i].X * t[j].Y - (long)t[j].X * t[i].Y; }
        return sum < 0 ? -sum : sum;
    }

    private static bool IsConcaveInt(Vec2D<int>[] v)
    {
        int n = v.Length; if (n < 4) return false;
        int pos = 0, neg = 0;
        for (int i = 0; i < n; i++)
        {
            int j = (i + 1) % n, kn = (i + 2) % n;
            long cr = (long)(v[j].X - v[i].X) * (v[kn].Y - v[j].Y) - (long)(v[j].Y - v[i].Y) * (v[kn].X - v[j].X);
            if (cr > 0) pos++; else if (cr < 0) neg++;
        }
        return pos > 0 && neg > 0;
    }
}
