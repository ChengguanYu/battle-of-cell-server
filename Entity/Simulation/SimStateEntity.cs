using System.Collections.Generic;
using Entity.Simulation.Shape;
using Fantasy;

namespace Entity.Simulation;

/// <summary>
/// 模拟器状态实体（POCO）：持有模拟器内部状态，跨热更存活。
/// 仿 <see cref="Entity.VOs.room.RoomEntity"/> 的字段壳子模式，
/// 定义在 Entity 层，逻辑由 Hotfix 层的 System 扩展方法操作。
/// </summary>
public sealed class SimStateEntity
{
    /// <summary>模拟器运行态。</summary>
    public SimState State = SimState.Create;

    /// <summary>世界中存在的全部形状（三角形等），即模拟器内部状态。</summary>
    public readonly List<AbstShape> Shapes = new();

    /// <summary>形状只读视图。</summary>
    public IReadOnlyList<AbstShape> ShapeView => Shapes;

    /// <summary>玩家实体位置表：实体 uid -> 坐标。与 RoomUidGeneratorEntity 生成的 uid 同值。</summary>
    public readonly Dictionary<uint, Vec2D<uint>> Players = new();

    /// <summary>userId -> uid 正向映射（仅 Player 实体需要，供 LeaveRoom 回溯）。</summary>
    public readonly Dictionary<long, uint> UidByUserId = new();

    /// <summary>uid -> userId 反向映射（仅 Player 实体需要）。</summary>
    public readonly Dictionary<uint, long> UserIdByUid = new();

    /// <summary>延迟帧广播后写入的聚合帧。SimTickAsync 消费后 Dispose 并置 null。</summary>
    public ServerFrame? PendingSimFrame { get; set; }
}
