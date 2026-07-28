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
    /// 初始化世界：生成默认 10 个互不重叠的三角形，并打印顶点坐标。
    /// </summary>
    private void InitWorld()
    {
        int target = DefaultTriangleCount;
        int w = (int)Config.Map.X;
        int h = (int)Config.Map.Y;

        var rng = new global::System.Random(WorldSeed);
        int attempts = 0;

        var shapes = SimState.Shapes;
        while (shapes.Count < target && attempts < MaxGenerateAttempts)
        {
            attempts++;

            var tri = RandomTriangle(rng, w, h);
            if (tri.IsDegenerate)
            {
                continue;
            }

            // 与已有任一形状重叠则丢弃重试
            bool overlap = false;
            foreach (var exist in shapes)
            {
                if (tri.OverlapsWith(exist))
                {
                    overlap = true;
                    break;
                }
            }

            if (!overlap)
            {
                shapes.Add(tri);
            }
        }

        Log.Info($"[BattleOfCellV1] 初始化世界完成: 生成三角形 {shapes.Count} 个 (试探 {attempts} 次)");

        for (int i = 0; i < shapes.Count; i++)
        {
            if (shapes[i] is Triangle t)
            {
                Log.Info($"[BattleOfCellV1] 三角形#{i} 顶点: ({t.A.X},{t.A.Y}) ({t.B.X},{t.B.Y}) ({t.C.X},{t.C.Y})");
            }
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
}
