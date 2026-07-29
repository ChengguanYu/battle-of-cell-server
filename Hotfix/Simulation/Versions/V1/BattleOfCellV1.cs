using Entity.Simulation;
using Entity.Simulation.Shape;
using Fantasy;
using Hotfix.Simulation.Abstractions;
using Hotfix.Simulation.Abstractions.Config;
using Hotfix.Simulation.Generation;

namespace Hotfix.Simulation.Versions.V1;

public class BattleOfCellV1 : SimBase
{
    public BattleOfCellV1(SimulateConfig config, SimStateEntity simState) : base(config, simState)
    {
        WorldGenerator.Generate(SimState, Config.World, Config.Seed);
    }

    public override void SimTick()
    {
        // TODO: 实现 V1 的仿真步进逻辑
    }

    /// <summary>
    /// V1 随机生成：在安全区域内随机采样，通过 IsCircleValid 检测碰撞。
    /// </summary>
    public override bool TryGenerateCoord(out Vec2D<uint> coord, uint radius = DefaultSpawnRadius, int maxAttempts = DefaultSpawnMaxAttempts)
    {
        coord = default!;

        if (Config.World.Map.X == 0 || Config.World.Map.Y == 0)
        {
            Log.Warning($"[V1] TryGenerateCoord 失败：世界尺寸无效, x={Config.World.Map.X}, y={Config.World.Map.Y}");
            return false;
        }

        uint margin = radius;
        if (margin >= Config.World.Map.X || margin >= Config.World.Map.Y)
        {
            Log.Warning($"[V1] TryGenerateCoord 失败：半径 {radius} 超过地图尺寸 ({Config.World.Map.X}x{Config.World.Map.Y})");
            return false;
        }

        ulong xLimit = Config.World.Map.X - margin;
        ulong yLimit = Config.World.Map.Y - margin;

        for (int i = 0; i < maxAttempts; i++)
        {
            var candidate = new Vec2D<uint>(
                (uint)Random.Shared.NextInt64(margin, (long)xLimit),
                (uint)Random.Shared.NextInt64(margin, (long)yLimit)
            );

            if (IsCircleValid(candidate, radius))
            {
                coord = candidate;
                Log.Debug($"[V1] TryGenerateCoord 成功: x={coord.X}, y={coord.Y}, attempts={i + 1}, radius={radius}");
                return true;
            }
        }

        Log.Warning($"[V1] TryGenerateCoord 失败：超过最大重试次数 {maxAttempts}, radius={radius}, map={Config.World.Map.X}x{Config.World.Map.Y}");
        return false;
    }
}
