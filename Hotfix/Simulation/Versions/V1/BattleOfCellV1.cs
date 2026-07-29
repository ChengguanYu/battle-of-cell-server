using Entity.Simulation;
using Entity.Simulation.Shape;
using Fantasy;
using Hotfix.Simulation.Abstractions;
using Hotfix.Simulation.Abstractions.Config;

namespace Hotfix.Simulation.Versions.V1;

public class BattleOfCellV1 : SimBase
{
    /// <summary>世界初始化给出的默认三角形数量。</summary>
    private const int DefaultTriangleCount = 10;
    /// <summary>世界中凹多边形的数量；总数仍为 DefaultTriangleCount。</summary>
    private const int DefaultConcaveCount = 3;

    /// <summary>
    /// 世界随机种子。固定值保证帧同步可复现；
    /// 如需每局不同可由 SimulateConfig 注入 roomId 派生种子。
    /// </summary>
    private const int WorldSeed = 20260728;

    private const int MapMargin = 50;
    private const int TriangleMinSpan = 20;
    /// <summary>骨架散布上限；地图 5000×5000 下控制在 500。</summary>
    private const int TriangleMaxSpan = 500;
    private const int MaxGenerateAttempts = 5000;
    /// <summary>目标面积与 500 跨度骨架同量级；2×面积计量，PickScale 在 k=1/2 间择优。</summary>
    private const long TargetShapeArea = 50000;
    /// <summary>targetArea2 = 2 × TargetShapeArea，与 ShoelaceArea2 计量对齐。</summary>
    private const long TargetShapeArea2 = 2L * TargetShapeArea;
    /// <summary>三角形最小内角（弧度）；20°，过滤过窄的退化三角形。</summary>
    private const double MinInteriorAngleRad = 15.0 * global::System.Math.PI / 180.0;

    public BattleOfCellV1(SimulateConfig config, SimStateEntity simState) : base(config, simState)
    {
        InitWorld();
    }

    public override void SimTick()
    {
        // TODO: 实现 V1 的仿真步进逻辑
        // 形状状态已在构造期初始化，仿真步进逻辑待后续实现
    }

    /// <summary>
    /// 初始化世界：生成默认 10 个互不重叠的形状，
    /// 其中前 DefaultConcaveCount 个为凹多边形，其余为三角形，并打印顶点坐标。
    /// </summary>
    private void InitWorld()
    {
        int target = DefaultTriangleCount;
        int targetConcave = DefaultConcaveCount;
        int w = (int)Config.Map.X;
        int h = (int)Config.Map.Y;

        var rng = new global::System.Random(WorldSeed);
        int attempts = 0;
        int concaveGenerated = 0;

        var shapes = SimState.Shapes;
        while (shapes.Count < target && attempts < MaxGenerateAttempts)
        {
            attempts++;

            AbstShape? shape = null;
            if (concaveGenerated < targetConcave)
            {
                shape = RandomConcavePolygon(rng, w, h, TargetShapeArea2);
                if (shape == null)
                {
                    continue;
                }
            }
            else
            {
                var tri = RandomTriangle(rng, w, h, TargetShapeArea2);
                if (tri == null || tri.IsDegenerate)
                {
                    continue;
                }
                shape = tri;
            }

            // 与已有任一形状重叠则丢弃重试（凸/凹混合由基类 OverlapsWith 统一分派）
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
                if (shape is ConcavePolygon)
                {
                    concaveGenerated++;
                }
            }
        }

        Log.Info($"[BattleOfCellV1] 初始化世界完成: 生成形状 {shapes.Count} 个 (凹形 {concaveGenerated}, 试探 {attempts} 次)");

        for (int i = 0; i < shapes.Count; i++)
        {
            var verts = shapes[i].Vertices;
            var sb = new global::System.Text.StringBuilder();
            for (int v = 0; v < verts.Count; v++)
            {
                if (v > 0) sb.Append(' ');
                sb.Append('(').Append(verts[v].X).Append(',').Append(verts[v].Y).Append(')');
            }
            Log.Info($"[BattleOfCellV1] 形状#{i} {shapes[i].GetType().Name} 顶点: {sb}");
        }
    }

    /// <summary>
    /// 在地图范围内生成一个三角形，面积尽量靠近 <paramref name="targetArea2"/>（2×面积）。
    /// 流程：散布骨架（角点锚定原点）→ 鞋带算骨架 2×面积 → 整数缩放 k 使 k²×A0 靠近目标，
    /// 其中 k 在 floor/ceil 中选逼近更优者 → 随机平移落位。退化三角形照常返回交调用方判定。
    /// </summary>
    private static Triangle? RandomTriangle(global::System.Random rng, int w, int h, long targetArea2)
    {
        // 骨架："角点锚定原点"的相对坐标，三个顶点散布在 [TriangleMinSpan, TriangleMaxSpan]
        // 取正区间保证相对偏移非负，缩放不改变符号、避免 uint 下溢。
        long ax = rng.Next(TriangleMinSpan, TriangleMaxSpan + 1);
        long ay = rng.Next(TriangleMinSpan, TriangleMaxSpan + 1);
        long bx = rng.Next(TriangleMinSpan, TriangleMaxSpan + 1);
        long by = rng.Next(TriangleMinSpan, TriangleMaxSpan + 1);
        long cx = rng.Next(TriangleMinSpan, TriangleMaxSpan + 1);
        long cy = rng.Next(TriangleMinSpan, TriangleMaxSpan + 1);

        // 骨架 2×面积（带符号取绝对值）；0 表示共线退化。
        long a0_2 = global::System.Math.Abs((bx - ax) * (cy - ay) - (cx - ax) * (by - ay));
        if (a0_2 == 0)
        {
            return null;
        }

        // 最小内角约束：任一内角 < 20° 视为过窄，丢弃重试。
        if (MinAngle(ax, ay, bx, by, cx, cy) < MinInteriorAngleRad)
        {
            return null;
        }

        int k = PickScale(targetArea2, a0_2);

        // 缩放后骨架跨度（相对原点），用于随机平移的安全区间。
        long spanMaxX = global::System.Math.Max(global::System.Math.Max(ax, bx), cx) * k;
        long spanMaxY = global::System.Math.Max(global::System.Math.Max(ay, by), cy) * k;
        if (spanMaxX > w - 2 * MapMargin || spanMaxY > h - 2 * MapMargin)
        {
            // 缩放过头放不下，跌回 k=1 用原骨架跨度重算平移区间。
            k = 1;
            spanMaxX = global::System.Math.Max(global::System.Math.Max(ax, bx), cx);
            spanMaxY = global::System.Math.Max(global::System.Math.Max(ay, by), cy);
        }

        int ox = rng.Next(MapMargin, global::System.Math.Max(MapMargin + 1, (int)(w - MapMargin - spanMaxX)));
        int oy = rng.Next(MapMargin, global::System.Math.Max(MapMargin + 1, (int)(h - MapMargin - spanMaxY)));

        uint sax = (uint)global::System.Math.Clamp((int)(ox + ax * k), MapMargin, w - MapMargin);
        uint say = (uint)global::System.Math.Clamp((int)(oy + ay * k), MapMargin, h - MapMargin);
        uint sbx = (uint)global::System.Math.Clamp((int)(ox + bx * k), MapMargin, w - MapMargin);
        uint sby = (uint)global::System.Math.Clamp((int)(oy + by * k), MapMargin, h - MapMargin);
        uint scx = (uint)global::System.Math.Clamp((int)(ox + cx * k), MapMargin, w - MapMargin);
        uint scy = (uint)global::System.Math.Clamp((int)(oy + cy * k), MapMargin, h - MapMargin);

        return new Triangle(new Vec2D<uint>(sax, say), new Vec2D<uint>(sbx, sby), new Vec2D<uint>(scx, scy));
    }

    /// <summary>
    /// 选整数缩放系数使 k²×a0_2 尽量靠近 targetArea2。
    /// 在 floor(sqrt) 与 ceil(sqrt) 二者间取更近者；下界 1。
    /// </summary>
    private static int PickScale(long targetArea2, long a0_2)
    {
        double root = global::System.Math.Sqrt((double)targetArea2 / a0_2);
        int kf = (int)global::System.Math.Floor(root);
        if (kf < 1) kf = 1;
        int kc = kf + 1;
        long af = a0_2 * (long)kf * kf;
        long ac = a0_2 * (long)kc * kc;
        return global::System.Math.Abs(af - targetArea2) <= global::System.Math.Abs(ac - targetArea2) ? kf : kc;
    }

    /// <summary>
    /// 返回三角形三个内角中的最小值（弧度）。
    /// 用点积+叉积计算，避免 acos 精度问题：atan2(|叉|, 点) 直接给夹角。
    /// </summary>
    private static double MinAngle(long ax, long ay, long bx, long by, long cx, long cy)
    {
        double angA = VertexAngle(ax, ay, bx, by, cx, cy);
        double angB = VertexAngle(bx, by, ax, ay, cx, cy);
        double angC = VertexAngle(cx, cy, ax, ay, bx, by);
        return global::System.Math.Min(angA, global::System.Math.Min(angB, angC));
    }

    /// <summary>顶点 (px,py) 处两条边 (→a) 和 (→b) 的夹角（弧度）。</summary>
    private static double VertexAngle(long px, long py, long ax, long ay, long bx, long by)
    {
        long ux = ax - px, uy = ay - py;
        long vx = bx - px, vy = by - py;
        double cross = (double)(ux * vy - uy * vx);
        double dot = (double)(ux * vx + uy * vy);
        return global::System.Math.Abs(global::System.Math.Atan2(cross, dot));
    }

    /// <summary>Vec2D&lt;int&gt; 模板的鞋带 2×面积（long 运算，无除法）。</summary>
    private static long ShoelaceInt2(Vec2D<int>[] t)
    {
        long sum = 0;
        int n = t.Length;
        for (int i = 0; i < n; i++)
        {
            int j = (i + 1) % n;
            sum += (long)t[i].X * t[j].Y - (long)t[j].X * t[i].Y;
        }
        return sum < 0 ? -sum : sum;
    }

    /// <summary>
    /// 随机生成一个凹多边形，面积尽量靠近 <paramref name="targetArea2"/>（2×面积）。
    /// 流程：圆周散点（角度排序保证简单多边形）→ 取整锚定原点 →
    /// 将一个顶点压向质心制造凹角 → 凹性自检 → 面积缩放 → 平移落位。
    /// 顶点数随机 5~8，每次形状各异。浮点仅用于散布角度/半径，
    /// 最终坐标取整落定，不影响帧同步确定性（仅服务器生成）。
    /// </summary>
    private static ConcavePolygon? RandomConcavePolygon(global::System.Random rng, int w, int h, long targetArea2)
    {
        int n = rng.Next(5, 9); // 5..8 顶点
        double cx = TriangleMaxSpan * 0.5;
        double cy = TriangleMaxSpan * 0.5;
        double baseR = TriangleMaxSpan * 0.4;

        // 圆周散点 + 角度抖动 + 半径抖动 → 角度排序后为简单多边形（近凸）
        var pts = new (double x, double y, double ang)[n];
        for (int i = 0; i < n; i++)
        {
            double ang = 2.0 * global::System.Math.PI * i / n + rng.NextDouble() * (global::System.Math.PI / n);
            double r = baseR * (0.65 + 0.35 * rng.NextDouble());
            pts[i] = (cx + r * global::System.Math.Cos(ang), cy + r * global::System.Math.Sin(ang), ang);
        }
        global::System.Array.Sort(pts, (a, b) => a.ang.CompareTo(b.ang));

        // 取整 + 锚定原点
        double minX = double.MaxValue, minY = double.MaxValue;
        for (int i = 0; i < n; i++)
        {
            if (pts[i].x < minX) minX = pts[i].x;
            if (pts[i].y < minY) minY = pts[i].y;
        }
        var skeleton = new Vec2D<int>[n];
        long sumX = 0, sumY = 0;
        for (int i = 0; i < n; i++)
        {
            int ix = (int)global::System.Math.Round(pts[i].x - minX);
            int iy = (int)global::System.Math.Round(pts[i].y - minY);
            skeleton[i] = new Vec2D<int>(ix, iy);
            sumX += ix;
            sumY += iy;
        }

        // 将一个顶点压向质心制造凹角
        double ctrX = (double)sumX / n;
        double ctrY = (double)sumY / n;
        int notch = rng.Next(n);
        double push = 0.3 + 0.5 * rng.NextDouble(); // 30%~80% 压向质心
        int nx = (int)global::System.Math.Round(skeleton[notch].X * (1.0 - push) + ctrX * push);
        int ny = (int)global::System.Math.Round(skeleton[notch].Y * (1.0 - push) + ctrY * push);
        if (nx < 0) nx = 0;
        if (ny < 0) ny = 0;
        skeleton[notch] = new Vec2D<int>(nx, ny);

        // 凹性验证（缩放保号，验证骨架即可）
        if (!IsConcaveInt(skeleton))
        {
            return null;
        }

        // 面积缩放 + 平移落位
        long a0_2 = ShoelaceInt2(skeleton);
        if (a0_2 == 0) return null;
        int k = PickScale(targetArea2, a0_2);

        long spanMaxX = 0, spanMaxY = 0;
        for (int i = 0; i < n; i++)
        {
            long sxk = (long)skeleton[i].X * k;
            long syk = (long)skeleton[i].Y * k;
            if (sxk > spanMaxX) spanMaxX = sxk;
            if (syk > spanMaxY) spanMaxY = syk;
        }
        if (spanMaxX > w - 2 * MapMargin || spanMaxY > h - 2 * MapMargin) return null;

        int ox = rng.Next(MapMargin, global::System.Math.Max(MapMargin + 1, (int)(w - MapMargin - spanMaxX)));
        int oy = rng.Next(MapMargin, global::System.Math.Max(MapMargin + 1, (int)(h - MapMargin - spanMaxY)));

        var verts = new global::System.Collections.Generic.List<Vec2D<uint>>(n);
        for (int i = 0; i < n; i++)
        {
            uint x = (uint)global::System.Math.Clamp((int)(ox + (long)skeleton[i].X * k), MapMargin, w - MapMargin);
            uint y = (uint)global::System.Math.Clamp((int)(oy + (long)skeleton[i].Y * k), MapMargin, h - MapMargin);
            verts.Add(new Vec2D<uint>(x, y));
        }

        var poly = new ConcavePolygon(verts);
        return poly.IsConcaveShape ? poly : null;
    }

    /// <summary>Vec2D&lt;int&gt; 骨架的凹性判定：相邻边叉积符号有正有负即为凹。</summary>
    private static bool IsConcaveInt(Vec2D<int>[] v)
    {
        int n = v.Length;
        if (n < 4) return false;
        int pos = 0, neg = 0;
        for (int i = 0; i < n; i++)
        {
            int j = (i + 1) % n;
            int kn = (i + 2) % n;
            long cr = (long)(v[j].X - v[i].X) * (v[kn].Y - v[j].Y) - (long)(v[j].Y - v[i].Y) * (v[kn].X - v[j].X);
            if (cr > 0) pos++;
            else if (cr < 0) neg++;
        }
        return pos > 0 && neg > 0;
    }
}
