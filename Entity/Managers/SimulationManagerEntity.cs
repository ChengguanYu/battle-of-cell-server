using System.Collections.Concurrent;
using Entity.Simulation;

namespace Entity.Managers;

/// <summary>
/// 模拟器管理器实体组件：维护 roomId -> ISimulation 的 1:1 映射。
/// 挂载到 Rooms Scene，字段壳子在 Entity 层，逻辑在 Hotfix 层扩展方法。
/// </summary>
public sealed class SimulationManagerEntity : Fantasy.Entitas.Entity
{
    public readonly ConcurrentDictionary<uint, ISimulation> SimByRoomId = new();

    /// <summary>roomId -> 模拟器内部状态（含 Shape 列表）。与 SimByRoomId 同 key 同生命周期。</summary>
    public readonly ConcurrentDictionary<uint, SimStateEntity> StateByRoomId = new();
}
