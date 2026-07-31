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
    private ulong _logicFrameIndex;

   public BattleOfCellV1(SimulateConfig config, SimStateEntity simState) : base(config, simState)
   {
        WorldGenerator.Generate(SimState, Config.World, Config.Seed);
   }

    /// <summary>
    /// 从 RoomSystem 外部 tick 调用，仅报告状态，不做物理模拟。
    /// 物理模拟由内部独立循环（20Hz 定时器 OnSimTimerTick）驱动。
    /// </summary>
    public override async FTask SimTickAsync()
    {
        await FTask.CompletedTask;
    }

    /// <summary>
    /// 内部独立循环（定时器回调）：一个完整的逻辑帧。
    /// </summary>
    protected override void OnSimTimerTick()
    {
        _logicFrameIndex++;
        bool consumed = ConsumePendingFrame();
        TickPhysics();
        if (consumed)
        {
            LogPositions();
        }
    }

    private bool ConsumePendingFrame()
    {
        var frame = SimState.PendingSimFrame;
        if (frame == null) return false;

        var log = $"聚合帧 #{frame.frame_number}";
        if (frame.frames is { Count: > 0 })
        {
            foreach (var op in frame.frames)
            {
                log += $" [op={op.op} eid={op.data?.eid}]";

                if (op.op == Op.LAUNCH && op.data != null && op.data.direction != null && SimState.PlayerSimData.TryGetValue(op.data.eid, out var pd))
                {
                    pd.Vx += (op.data.direction.x * op.data.speed) / FIXED_SCALE;
                    pd.Vy += (op.data.direction.y * op.data.speed) / FIXED_SCALE;
                }
            }
        }
        else
        {
            log += " 无玩家操作";
        }
        Log.Debug(log);

        frame.Dispose();
        SimState.PendingSimFrame = null;
        return true;
    }

    private void TickPhysics()
    {
        const long dt = 50; // 0.05s × 1000
        long mapX = (long)Config.World.Map.X * FIXED_SCALE;
        long mapY = (long)Config.World.Map.Y * FIXED_SCALE;
        long radius = DEFAULT_RADIUS;

        foreach (var kv in SimState.PlayerSimData)
        {
            var pd = kv.Value;

            // 减速
            long speed = FixedHypot(pd.Vx, pd.Vy);
            if (speed > 0)
            {
                long decelAmount = FixedMul(DEFAULT_DECEL, dt);
                if (decelAmount >= speed)
                {
                    pd.Vx = 0;
                    pd.Vy = 0;
                }
                else
                {
                    long newSpeed = speed - decelAmount;
                    long ratio = FixedDiv(newSpeed, speed);
                    pd.Vx = FixedMul(pd.Vx, ratio);
                    pd.Vy = FixedMul(pd.Vy, ratio);
                }
            }

            // 位置更新
            pd.X += FixedMul(pd.Vx, dt);
            pd.Y += FixedMul(pd.Vy, dt);

            // 世界边界 clamp（停止不反弹）
            if (pd.X < radius) { pd.X = radius; pd.Vx = 0; }
            if (pd.X > mapX - radius) { pd.X = mapX - radius; pd.Vx = 0; }
            if (pd.Y < radius) { pd.Y = radius; pd.Vy = 0; }
            if (pd.Y > mapY - radius) { pd.Y = mapY - radius; pd.Vy = 0; }
        }
    }

    private void LogPositions()
    {
        if (SimState.Players.Count == 0)
        {
            Log.Debug("房间内无玩家");
        }
        else
        {
            var posLog = "玩家位置:";
            foreach (var kv in SimState.Players)
            {
                if (SimState.PlayerSimData.TryGetValue(kv.Key, out var pd))
                {
                    posLog += $" [{kv.Key}]({pd.X / FIXED_SCALE},{pd.Y / FIXED_SCALE})";
                }
                else
                {
                    posLog += $" [{kv.Key}]({kv.Value.X},{kv.Value.Y})";
                }
            }
            Log.Debug(posLog);
        }
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
