using Entity.Managers;
using Entity.Simulation;
using Fantasy;
using Hotfix.Simulation.Abstractions.Config;
using Hotfix.Simulation.Versions.V1;

namespace Hotfix.Simulation.System;

/// <summary>
/// 模拟器管理器扩展方法：房间与模拟器 1:1 绑定的逻辑。
/// 字段持有在 <see cref="SimulationManagerEntity"/>，逻辑在此。
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

        var newSim = new BattleOfCellV1(new SimulateConfig());
        if (self.SimByRoomId.TryAdd(roomId, newSim))
        {
            sim = newSim;
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
