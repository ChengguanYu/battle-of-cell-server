using Entity.Simulation;
using Entity.Simulation.Shape;
using Fantasy;
using Fantasy.Async;
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

    public override async FTask SimTickAsync()
    {
        // 打玩家位置（与聚合帧无关）
        if (SimState.Players.Count == 0)
        {
            Log.Debug("房间内无玩家");
        }
        else
        {
            var posLog = "玩家位置:";
            foreach (var kv in SimState.Players)
            {
                posLog += $" [{kv.Key}]({kv.Value.X},{kv.Value.Y})";
            }
            Log.Debug(posLog);
        }

        // 消费聚合帧
        var frame = SimState.PendingSimFrame;
        if (frame != null)
        {
            var log = $"聚合帧 #{frame.frame_number}";
            if (frame.frames is { Count: > 0 })
            {
                foreach (var op in frame.frames)
                {
                    log += $" [op={op.op} eid={op.data?.eid}]";
                }
            }
            else
            {
                log += " 无玩家操作";
            }
            Log.Debug(log);

            frame.Dispose();
            SimState.PendingSimFrame = null;
        }

        await FTask.CompletedTask;
    }

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
