using Entity.Managers;
using Hotfix.Simulation.Abstractions;
using Entity.Simulation;
using Fantasy;
using Hotfix.Simulation.Abstractions.Config;
using Hotfix.Simulation.Versions.V1;

namespace Hotfix.Simulation.System;

    /// <summary>
    /// 为房间创建模拟器。roomId 已存在则失败。成功时通过 sim 返回实例，自动启动独立循环。
    /// </summary>
/// </summary>
public static class SimulationManagerSystem
{
    /// <summary>
    /// 为房间创建模拟器。roomId 已存在则失败。成功时通过 sim 返回实例，由调用方负责状态转移。
    /// </summary>
    public static bool Create(this SimulationManagerEntity self, uint roomId, out ISimulation? sim)
    {
        sim = null;
        if (roomId == 0)
        {
            Log.Warning($"SimulationManager.Create 失败：roomId 非法, roomId={roomId}");
            return false;
        }

        var state = new SimStateEntity();
        var newSim = new BattleOfCellV1(new SimulateConfig(), state);

        // 两表同 key 同生命周期：sim 和 state 必须原子写入，失败则不留残骸
        if (self.SimByRoomId.TryAdd(roomId, newSim))
        {
            if (!self.StateByRoomId.TryAdd(roomId, state))
            {
                // 极端竞态：sim 写入成功但 state 冲突，回滚 sim
                self.SimByRoomId.TryRemove(roomId, out _);
                Log.Warning($"SimulationManager.Create 状态写入冲突，已回滚: roomId={roomId}");
                return false;
            }

            sim = newSim;
            ((SimBase)newSim).SetScene(self.Scene);
            newSim.Run();
            return true;
        }

        Log.Warning($"SimulationManager.Create 失败：已存在, roomId={roomId}");
        return false;
    }

    /// <summary>
    /// 销毁房间对应的模拟器并停止。
    /// </summary>
    public static bool Remove(this SimulationManagerEntity self, uint roomId)
    {
        if (!self.SimByRoomId.TryRemove(roomId, out var sim) || sim == null)
        {
            return false;
        }

        sim.Stop();
        self.StateByRoomId.TryRemove(roomId, out _);
        Log.Info($"SimulationManager.Remove 成功: roomId={roomId}");
        return true;
    }

    public static bool TryGet(this SimulationManagerEntity self, uint roomId, out ISimulation? sim)
    {
        sim = null;
        if (roomId == 0)
        {
            return false;
        }

        return self.SimByRoomId.TryGetValue(roomId, out sim) && sim != null;
    }

    /// <summary>
    /// 清空全部模拟器。仅供 Scene 销毁时调用。
    /// </summary>
    public static void Clear(this SimulationManagerEntity self)
    {
        foreach (var roomId in self.SimByRoomId.Keys.ToArray())
        {
            self.Remove(roomId);
        }
    }
}
