using Entity.Managers;
using Entity.VOs.room;
using Fantasy;
using Hotfix.Scene.Rooms.System;
using Hotfix.Database;

namespace Hotfix.Scene.Rooms.Service;

public sealed partial class RoomsService
{
    /// <summary>
    /// Opened 空房：按占位 max TTL 走状态机 Hold，或正式 Close 并清 key。
    /// 由 RoomManager.Leave 在空员时回调；Leave 业务层不直接决策。
    /// </summary>
    public void OnRoomEmpty(uint roomId, string? reason)
    {
        if (roomId == 0)
        {
            return;
        }

        if (!Manager.TryGetById(roomId, out var room) || room == null)
        {
            Log.Debug($"Rooms OnRoomEmpty 忽略：房间不存在, roomId={roomId}, reason={reason}");
            return;
        }

        if (!room.IsOpened())
        {
            Log.Debug($"Rooms OnRoomEmpty 忽略：非 Opened, roomId={roomId}, state={room.State}, reason={reason}");
            return;
        }

        if (room.MemberCount > 0)
        {
            Log.Debug(
                $"Rooms OnRoomEmpty 忽略：已有成员, roomId={roomId}, memberCount={room.MemberCount}, reason={reason}");
            return;
        }

        var closeReason = string.IsNullOrWhiteSpace(reason) ? "empty" : reason;

        if (_redis == null)
        {
            Log.Warning($"Rooms OnRoomEmpty：Redis 缺失，正式关房, roomId={roomId}, reason={closeReason}");
            CloseRoomAndClearPlaceholders(roomId, closeReason);
            return;
        }

        if (!MatchResultDao.TryGetMaxRemainingTtlMs(_redis, roomId, out var remainMs, out var ttlError))
        {
            Log.Warning(
                $"Rooms OnRoomEmpty 查占位 TTL 失败，正式关房: roomId={roomId}, error={ttlError}, reason={closeReason}");
            CloseRoomAndClearPlaceholders(roomId, closeReason);
            return;
        }

        if (remainMs <= 0)
        {
            Log.Info($"Rooms OnRoomEmpty 无占位，正式关房: roomId={roomId}, reason={closeReason}");
            CloseRoomAndClearPlaceholders(roomId, closeReason);
            return;
        }

        if (!Manager.HoldRoom(roomId, remainMs))
        {
            Log.Warning(
                $"Rooms OnRoomEmpty Hold 失败，正式关房: roomId={roomId}, remainMs={remainMs}, reason={closeReason}");
            CloseRoomAndClearPlaceholders(roomId, closeReason);
            return;
        }

        Log.Info(
            $"Rooms OnRoomEmpty 进入 Holding: roomId={roomId}, remainMs={remainMs}, reason={closeReason}");
    }

    /// <summary>
    /// Holding 超时：复核成员/占位；有人则 Resume，有占位则续命，否则清 key 并正式关房。
    /// 由 RoomHoldTimer 经 RoomManager 回调，约定在 Rooms Scene 线程触发。
    /// </summary>
    public void OnHoldTimeout(uint roomId)
    {
        if (roomId == 0)
        {
            return;
        }

        if (!Manager.TryGetById(roomId, out var room) || room == null)
        {
            Log.Debug($"Rooms OnHoldTimeout 忽略：房间不存在, roomId={roomId}");
            return;
        }

        if (!room.IsHolding())
        {
            Log.Debug($"Rooms OnHoldTimeout 忽略：非 Holding, roomId={roomId}, state={room.State}");
            return;
        }

        if (room.MemberCount > 0)
        {
            if (!Manager.ResumeRoom(roomId))
            {
                Log.Warning(
                    $"Rooms OnHoldTimeout 防御 Resume 失败: roomId={roomId}, memberCount={room.MemberCount}, state={room.State}");
            }
            else
            {
                Log.Info(
                    $"Rooms OnHoldTimeout 房间有人，Resume: roomId={roomId}, memberCount={room.MemberCount}");
            }

            return;
        }

        if (_redis == null)
        {
            Log.Warning($"Rooms OnHoldTimeout：Redis 缺失，正式关房, roomId={roomId}");
            CloseRoomAndClearPlaceholders(roomId, "hold_timeout_no_redis");
            return;
        }

        if (!MatchResultDao.TryGetMaxRemainingTtlMs(_redis, roomId, out var remainMs, out var ttlError))
        {
            Log.Warning(
                $"Rooms OnHoldTimeout 查占位 TTL 失败，正式关房: roomId={roomId}, error={ttlError}");
            CloseRoomAndClearPlaceholders(roomId, "hold_timeout_ttl_error");
            return;
        }

        if (remainMs > 0)
        {
            if (!Manager.HoldRoom(roomId, remainMs))
            {
                Log.Warning(
                    $"Rooms OnHoldTimeout 续命失败，正式关房: roomId={roomId}, remainMs={remainMs}");
                CloseRoomAndClearPlaceholders(roomId, "hold_timeout_renew_failed");
                return;
            }

            Log.Info($"Rooms OnHoldTimeout 续命: roomId={roomId}, remainMs={remainMs}");
            return;
        }

        Log.Info($"Rooms OnHoldTimeout 无占位，正式关房: roomId={roomId}");
        CloseRoomAndClearPlaceholders(roomId, "hold_timeout_no_placeholder");
    }

    private void CloseRoomAndClearPlaceholders(uint roomId, string reason)
    {
        if (_redis != null)
        {
            if (!MatchResultDao.TryDeleteByRoom(_redis, roomId, out var deleted, out var delError))
            {
                Log.Warning($"Rooms 正式关房清理占位失败: roomId={roomId}, error={delError}, reason={reason}");
            }
            else
            {
                Log.Info($"Rooms 正式关房清理占位: roomId={roomId}, deleted={deleted}, reason={reason}");
            }
        }

        if (!Manager.RemoveRoom(roomId, reason: reason))
        {
            Log.Warning($"Rooms 正式关房失败: roomId={roomId}, reason={reason}");
            return;
        }

        Log.Info($"Rooms 正式关房完成: roomId={roomId}, reason={reason}");
    }
}
