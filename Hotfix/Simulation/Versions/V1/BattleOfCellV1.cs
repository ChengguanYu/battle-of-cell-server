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
    private const int TriangleMaxSpan = 200;
    private const int MaxGenerateAttempts = 5000;

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
                shape = RandomConcavePolygon(rng, w, h);
                if (shape == null)
                {
                    continue;
                }
            }
            else
            {
                var tri = RandomTriangle(rng, w, h);
                if (tri.IsDegenerate)
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
    /// 在地图范围内随机生成一个三角形：以一点为中心，在半径区间内散布顶点。
    /// 坐标经 Clamp 处理，避免越界与 uint 下溢。
    /// </summary>
    private static Triangle RandomTriangle(global::System.Random rng, int w, int h)
    {
        int cx = rng.Next(MapMargin, w - MapMargin);
        int cy = rng.Next(MapMargin, h - MapMargin);

        uint ax = (uint)global::System.Math.Clamp(cx + rng.Next(-TriangleMaxSpan, TriangleMaxSpan), MapMargin, w - MapMargin);
        uint ay = (uint)global::System.Math.Clamp(cy + rng.Next(-TriangleMaxSpan, TriangleMaxSpan), MapMargin, h - MapMargin);
        uint bx = (uint)global::System.Math.Clamp(cx + rng.Next(-TriangleMaxSpan, TriangleMaxSpan), MapMargin, w - MapMargin);
        uint by = (uint)global::System.Math.Clamp(cy + rng.Next(-TriangleMaxSpan, TriangleMaxSpan), MapMargin, h - MapMargin);
        uint ccx = (uint)global::System.Math.Clamp(cx + rng.Next(-TriangleMaxSpan, TriangleMaxSpan), MapMargin, w - MapMargin);
        uint ccy = (uint)global::System.Math.Clamp(cy + rng.Next(-TriangleMaxSpan, TriangleMaxSpan), MapMargin, h - MapMargin);

        return new Triangle(new Vec2D<uint>(ax, ay), new Vec2D<uint>(bx, by), new Vec2D<uint>(ccx, ccy));
    }

    /// <summary>
    /// 在地图范围内基于模板随机生成一个凹多边形：整数缩放 + 平移，不做旋转（保整数确定性）。
    /// 凹性自检失败返回 null，由调用方重试。
    /// </summary>
    private static ConcavePolygon? RandomConcavePolygon(global::System.Random rng, int w, int h)
    {
        var template = ConcaveTemplates[rng.Next(ConcaveTemplates.Length)];
        int scale = rng.Next(1, 3); // 1 或 2

        int spanMaxX = 0;
        int spanMaxY = 0;
        for (int i = 0; i < template.Length; i++)
        {
            spanMaxX = global::System.Math.Max(spanMaxX, template[i].X);
            spanMaxY = global::System.Math.Max(spanMaxY, template[i].Y);
        }

        int finalMaxX = spanMaxX * scale;
        int finalMaxY = spanMaxY * scale;
        if (finalMaxX > w - 2 * MapMargin || finalMaxY > h - 2 * MapMargin)
        {
            return null;
        }

        int ox = rng.Next(MapMargin, global::System.Math.Max(MapMargin + 1, w - MapMargin - finalMaxX));
        int oy = rng.Next(MapMargin, global::System.Math.Max(MapMargin + 1, h - MapMargin - finalMaxY));

        var verts = new global::System.Collections.Generic.List<Vec2D<uint>>(template.Length);
        for (int i = 0; i < template.Length; i++)
        {
            uint x = (uint)global::System.Math.Clamp(ox + template[i].X * scale, MapMargin, w - MapMargin);
            uint y = (uint)global::System.Math.Clamp(oy + template[i].Y * scale, MapMargin, h - MapMargin);
            verts.Add(new Vec2D<uint>(x, y));
        }

        var poly = new ConcavePolygon(verts);
        return poly.IsConcaveShape ? poly : null;
    }

    /// <summary>凹多边形顶点模板（整数相对坐标），均经过凹性验证。</summary>
    private static readonly Vec2D<int>[][] ConcaveTemplates =
    {
        // L 形（6 顶点）
        new[]
        {
            new Vec2D<int>(0, 0), new Vec2D<int>(200, 0),
            new Vec2D<int>(200, 100), new Vec2D<int>(100, 100),
            new Vec2D<int>(100, 200), new Vec2D<int>(0, 200)
        },
        // 箭头形（5 顶点）
        new[]
        {
            new Vec2D<int>(0, 0), new Vec2D<int>(200, 0),
            new Vec2D<int>(200, 100), new Vec2D<int>(120, 60),
            new Vec2D<int>(0, 100)
        }
    };
}
