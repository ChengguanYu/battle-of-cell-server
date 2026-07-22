using LightProto;
using MemoryPack;
using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;
using Fantasy;
using Fantasy.Pool;
using Fantasy.Network.Interface;
using Fantasy.Serialize;

// ReSharper disable InconsistentNaming
// ReSharper disable CollectionNeverUpdated.Global
// ReSharper disable RedundantTypeArgumentsOfMethod
// ReSharper disable PartialTypeWithSinglePart
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable PreferConcreteValueOverDefault
// ReSharper disable RedundantNameQualifier
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable CheckNamespace
// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable RedundantUsingDirective
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8618
namespace Fantasy
{
    /// <summary>
    /// 玩家进入请求
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class PlayerEntryReq : AMessage, IAddressRequest
    {
        public static PlayerEntryReq Create(bool autoReturn = true)
        {
            var playerEntryReq = MessageObjectPool<PlayerEntryReq>.Rent();
            playerEntryReq.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                playerEntryReq.SetIsPool(false);
            }
            
            return playerEntryReq;
        }
        
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        public void Dispose()
        {
            if (!IsPool()) return; 
            userId = default;
            MessageObjectPool<PlayerEntryReq>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.PlayerEntryReq; } 
        [ProtoIgnore]
        public PlayerEntryResp ResponseType { get; set; }
        [ProtoMember(1)]
        public long userId { get; set; }
    }
    /// <summary>
    /// 玩家进入响应
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class PlayerEntryResp : AMessage, IAddressResponse
    {
        public static PlayerEntryResp Create(bool autoReturn = true)
        {
            var playerEntryResp = MessageObjectPool<PlayerEntryResp>.Rent();
            playerEntryResp.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                playerEntryResp.SetIsPool(false);
            }
            
            return playerEntryResp;
        }
        
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        public void Dispose()
        {
            if (!IsPool()) return; 
            ErrorCode = 0;
            MessageObjectPool<PlayerEntryResp>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.PlayerEntryResp; } 
        [ProtoMember(1)]
        public uint ErrorCode { get; set; }
    }
    /// <summary>
    /// Gate -> Avatar 匹配请求
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class AvatarMatchReq : AMessage, IAddressRequest
    {
        public static AvatarMatchReq Create(bool autoReturn = true)
        {
            var avatarMatchReq = MessageObjectPool<AvatarMatchReq>.Rent();
            avatarMatchReq.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                avatarMatchReq.SetIsPool(false);
            }
            
            return avatarMatchReq;
        }
        
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        public void Dispose()
        {
            if (!IsPool()) return; 
            userId = default;
            MessageObjectPool<AvatarMatchReq>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.AvatarMatchReq; } 
        [ProtoIgnore]
        public AvatarMatchResp ResponseType { get; set; }
        [ProtoMember(1)]
        public long userId { get; set; }
    }
    /// <summary>
    /// Gate -> Avatar 匹配响应
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class AvatarMatchResp : AMessage, IAddressResponse
    {
        public static AvatarMatchResp Create(bool autoReturn = true)
        {
            var avatarMatchResp = MessageObjectPool<AvatarMatchResp>.Rent();
            avatarMatchResp.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                avatarMatchResp.SetIsPool(false);
            }
            
            return avatarMatchResp;
        }
        
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        public void Dispose()
        {
            if (!IsPool()) return; 
            ErrorCode = 0;
            room_id = default;
            MessageObjectPool<AvatarMatchResp>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.AvatarMatchResp; } 
        [ProtoMember(1)]
        public uint ErrorCode { get; set; }
        /// <summary>
        /// 匹配成功后的房间 ID；失败时为 0
        /// </summary>
        [ProtoMember(2)]
        public long room_id { get; set; }
    }
    /// <summary>
    /// Gate -> Avatar 清理玩家通知（WsSession 清理后）
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class AvatarCleanupNotify : AMessage, IAddressMessage
    {
        public static AvatarCleanupNotify Create(bool autoReturn = true)
        {
            var avatarCleanupNotify = MessageObjectPool<AvatarCleanupNotify>.Rent();
            avatarCleanupNotify.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                avatarCleanupNotify.SetIsPool(false);
            }
            
            return avatarCleanupNotify;
        }
        
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        public void Dispose()
        {
            if (!IsPool()) return; 
            userId = default;
            reason = default;
            MessageObjectPool<AvatarCleanupNotify>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.AvatarCleanupNotify; } 
        [ProtoMember(1)]
        public long userId { get; set; }
        /// <summary>
        /// 清理原因，如 timed_out_grace_expired
        /// </summary>
        [ProtoMember(2)]
        public string reason { get; set; }
    }
    /// <summary>
    /// Avatar -> Match 匹配请求（旧链路，保留兼容）
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class MatchReq : AMessage, IAddressRequest
    {
        public static MatchReq Create(bool autoReturn = true)
        {
            var matchReq = MessageObjectPool<MatchReq>.Rent();
            matchReq.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                matchReq.SetIsPool(false);
            }
            
            return matchReq;
        }
        
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        public void Dispose()
        {
            if (!IsPool()) return; 
            userId = default;
            MessageObjectPool<MatchReq>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.MatchReq; } 
        [ProtoIgnore]
        public MatchResp ResponseType { get; set; }
        [ProtoMember(1)]
        public long userId { get; set; }
    }
    /// <summary>
    /// Avatar -> Match 匹配响应（旧链路，保留兼容）
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class MatchResp : AMessage, IAddressResponse
    {
        public static MatchResp Create(bool autoReturn = true)
        {
            var matchResp = MessageObjectPool<MatchResp>.Rent();
            matchResp.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                matchResp.SetIsPool(false);
            }
            
            return matchResp;
        }
        
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        public void Dispose()
        {
            if (!IsPool()) return; 
            ErrorCode = 0;
            room_id = default;
            MessageObjectPool<MatchResp>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.MatchResp; } 
        [ProtoMember(1)]
        public uint ErrorCode { get; set; }
        /// <summary>
        /// 匹配成功后的房间 ID；失败时为 0
        /// </summary>
        [ProtoMember(2)]
        public long room_id { get; set; }
    }
    /// <summary>
    /// Avatar -> Match 新匹配请求（Match 侧编排：查房列表 / Join / Create）
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class NewMatchReq : AMessage, IAddressRequest
    {
        public static NewMatchReq Create(bool autoReturn = true)
        {
            var newMatchReq = MessageObjectPool<NewMatchReq>.Rent();
            newMatchReq.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                newMatchReq.SetIsPool(false);
            }
            
            return newMatchReq;
        }
        
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        public void Dispose()
        {
            if (!IsPool()) return; 
            userId = default;
            MessageObjectPool<NewMatchReq>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.NewMatchReq; } 
        [ProtoIgnore]
        public NewMatchResp ResponseType { get; set; }
        [ProtoMember(1)]
        public long userId { get; set; }
    }
    /// <summary>
    /// Avatar -> Match 新匹配响应
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class NewMatchResp : AMessage, IAddressResponse
    {
        public static NewMatchResp Create(bool autoReturn = true)
        {
            var newMatchResp = MessageObjectPool<NewMatchResp>.Rent();
            newMatchResp.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                newMatchResp.SetIsPool(false);
            }
            
            return newMatchResp;
        }
        
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        public void Dispose()
        {
            if (!IsPool()) return; 
            ErrorCode = 0;
            room_id = default;
            MessageObjectPool<NewMatchResp>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.NewMatchResp; } 
        [ProtoMember(1)]
        public uint ErrorCode { get; set; }
        /// <summary>
        /// 匹配成功后的房间 ID；失败时为 0
        /// </summary>
        [ProtoMember(2)]
        public long room_id { get; set; }
    }
    /// <summary>
    /// Match/Avatar -> Rooms 进入房间请求（旧链路 MatchOrCreate，保留兼容）
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class RoomsEnterReq : AMessage, IAddressRequest
    {
        public static RoomsEnterReq Create(bool autoReturn = true)
        {
            var roomsEnterReq = MessageObjectPool<RoomsEnterReq>.Rent();
            roomsEnterReq.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                roomsEnterReq.SetIsPool(false);
            }
            
            return roomsEnterReq;
        }
        
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        public void Dispose()
        {
            if (!IsPool()) return; 
            userId = default;
            MessageObjectPool<RoomsEnterReq>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.RoomsEnterReq; } 
        [ProtoIgnore]
        public RoomsEnterResp ResponseType { get; set; }
        [ProtoMember(1)]
        public long userId { get; set; }
    }
    /// <summary>
    /// Match/Avatar -> Rooms 进入房间响应（旧链路）
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class RoomsEnterResp : AMessage, IAddressResponse
    {
        public static RoomsEnterResp Create(bool autoReturn = true)
        {
            var roomsEnterResp = MessageObjectPool<RoomsEnterResp>.Rent();
            roomsEnterResp.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                roomsEnterResp.SetIsPool(false);
            }
            
            return roomsEnterResp;
        }
        
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        public void Dispose()
        {
            if (!IsPool()) return; 
            ErrorCode = 0;
            room_id = default;
            MessageObjectPool<RoomsEnterResp>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.RoomsEnterResp; } 
        [ProtoMember(1)]
        public uint ErrorCode { get; set; }
        /// <summary>
        /// 进入成功后的房间 ID；失败时为 0
        /// </summary>
        [ProtoMember(2)]
        public long room_id { get; set; }
    }
    /// <summary>
    /// Avatar -> Rooms 玩家离房检查（会话清理等）
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class RoomsPlayerLeaveNotify : AMessage, IAddressMessage
    {
        public static RoomsPlayerLeaveNotify Create(bool autoReturn = true)
        {
            var roomsPlayerLeaveNotify = MessageObjectPool<RoomsPlayerLeaveNotify>.Rent();
            roomsPlayerLeaveNotify.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                roomsPlayerLeaveNotify.SetIsPool(false);
            }
            
            return roomsPlayerLeaveNotify;
        }
        
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        public void Dispose()
        {
            if (!IsPool()) return; 
            userId = default;
            reason = default;
            MessageObjectPool<RoomsPlayerLeaveNotify>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.RoomsPlayerLeaveNotify; } 
        [ProtoMember(1)]
        public long userId { get; set; }
        /// <summary>
        /// 离房原因，如 timed_out_grace_expired
        /// </summary>
        [ProtoMember(2)]
        public string reason { get; set; }
    }
    /// <summary>
    /// 房间列表快照条目（非权威，仅供 Match 选房线索）
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class RoomSnapItem : AMessage, IMessage
    {
        public static RoomSnapItem Create(bool autoReturn = true)
        {
            var roomSnapItem = MessageObjectPool<RoomSnapItem>.Rent();
            roomSnapItem.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                roomSnapItem.SetIsPool(false);
            }
            
            return roomSnapItem;
        }
        
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        public void Dispose()
        {
            if (!IsPool()) return; 
            room_id = default;
            member_count = default;
            capacity = default;
            state = default;
            MessageObjectPool<RoomSnapItem>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.RoomSnapItem; } 
        [ProtoMember(1)]
        public long room_id { get; set; }
        [ProtoMember(2)]
        public int member_count { get; set; }
        [ProtoMember(3)]
        public int capacity { get; set; }
        /// <summary>
        /// RoomState 枚举底层值：Created=0, Opened=1, Closed=2
        /// </summary>
        [ProtoMember(4)]
        public int state { get; set; }
    }
    /// <summary>
    /// Match -> Rooms 拉取可观察房间列表快照（只读线索，Join 结果才是权威）
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class RoomsGetRoomListSnapReq : AMessage, IAddressRequest
    {
        public static RoomsGetRoomListSnapReq Create(bool autoReturn = true)
        {
            var roomsGetRoomListSnapReq = MessageObjectPool<RoomsGetRoomListSnapReq>.Rent();
            roomsGetRoomListSnapReq.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                roomsGetRoomListSnapReq.SetIsPool(false);
            }
            
            return roomsGetRoomListSnapReq;
        }
        
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        public void Dispose()
        {
            if (!IsPool()) return; 
            MessageObjectPool<RoomsGetRoomListSnapReq>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.RoomsGetRoomListSnapReq; } 
        [ProtoIgnore]
        public RoomsGetRoomListSnapResp ResponseType { get; set; }
    }
    /// <summary>
    /// Match -> Rooms 房间列表快照响应
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class RoomsGetRoomListSnapResp : AMessage, IAddressResponse
    {
        public static RoomsGetRoomListSnapResp Create(bool autoReturn = true)
        {
            var roomsGetRoomListSnapResp = MessageObjectPool<RoomsGetRoomListSnapResp>.Rent();
            roomsGetRoomListSnapResp.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                roomsGetRoomListSnapResp.SetIsPool(false);
            }
            
            return roomsGetRoomListSnapResp;
        }
        
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        public void Dispose()
        {
            if (!IsPool()) return; 
            ErrorCode = 0;
            foreach (var __t in rooms) __t.Dispose();
            rooms.Clear();
            IsEmpty = default;
            MessageObjectPool<RoomsGetRoomListSnapResp>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.RoomsGetRoomListSnapResp; } 
        [ProtoMember(1)]
        public uint ErrorCode { get; set; }
        [ProtoMember(2)]
        public List<RoomSnapItem> rooms { get; set; } = new List<RoomSnapItem>();
        /// <summary>
        /// 是否为空列表；true 表示当前无可观察房间
        /// </summary>
        [ProtoMember(3)]
        public bool IsEmpty { get; set; }
    }
    /// <summary>
    /// Match -> Rooms 加入指定房间
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class RoomsJoinReq : AMessage, IAddressRequest
    {
        public static RoomsJoinReq Create(bool autoReturn = true)
        {
            var roomsJoinReq = MessageObjectPool<RoomsJoinReq>.Rent();
            roomsJoinReq.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                roomsJoinReq.SetIsPool(false);
            }
            
            return roomsJoinReq;
        }
        
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        public void Dispose()
        {
            if (!IsPool()) return; 
            userId = default;
            room_id = default;
            MessageObjectPool<RoomsJoinReq>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.RoomsJoinReq; } 
        [ProtoIgnore]
        public RoomsJoinResp ResponseType { get; set; }
        [ProtoMember(1)]
        public long userId { get; set; }
        [ProtoMember(2)]
        public long room_id { get; set; }
    }
    /// <summary>
    /// Match -> Rooms 加入指定房间响应
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class RoomsJoinResp : AMessage, IAddressResponse
    {
        public static RoomsJoinResp Create(bool autoReturn = true)
        {
            var roomsJoinResp = MessageObjectPool<RoomsJoinResp>.Rent();
            roomsJoinResp.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                roomsJoinResp.SetIsPool(false);
            }
            
            return roomsJoinResp;
        }
        
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        public void Dispose()
        {
            if (!IsPool()) return; 
            ErrorCode = 0;
            room_id = default;
            MessageObjectPool<RoomsJoinResp>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.RoomsJoinResp; } 
        [ProtoMember(1)]
        public uint ErrorCode { get; set; }
        /// <summary>
        /// 加入成功后的房间 ID；失败时为 0
        /// </summary>
        [ProtoMember(2)]
        public long room_id { get; set; }
    }
    /// <summary>
    /// Match -> Rooms 创建房间并加入首位成员
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class RoomsCreateReq : AMessage, IAddressRequest
    {
        public static RoomsCreateReq Create(bool autoReturn = true)
        {
            var roomsCreateReq = MessageObjectPool<RoomsCreateReq>.Rent();
            roomsCreateReq.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                roomsCreateReq.SetIsPool(false);
            }
            
            return roomsCreateReq;
        }
        
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        public void Dispose()
        {
            if (!IsPool()) return; 
            userId = default;
            capacity = default;
            MessageObjectPool<RoomsCreateReq>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.RoomsCreateReq; } 
        [ProtoIgnore]
        public RoomsCreateResp ResponseType { get; set; }
        [ProtoMember(1)]
        public long userId { get; set; }
        /// <summary>
        /// 容量；0 表示使用服务端默认
        /// </summary>
        [ProtoMember(2)]
        public int capacity { get; set; }
    }
    /// <summary>
    /// Match -> Rooms 创建房间响应
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class RoomsCreateResp : AMessage, IAddressResponse
    {
        public static RoomsCreateResp Create(bool autoReturn = true)
        {
            var roomsCreateResp = MessageObjectPool<RoomsCreateResp>.Rent();
            roomsCreateResp.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                roomsCreateResp.SetIsPool(false);
            }
            
            return roomsCreateResp;
        }
        
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        public void Dispose()
        {
            if (!IsPool()) return; 
            ErrorCode = 0;
            room_id = default;
            MessageObjectPool<RoomsCreateResp>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.RoomsCreateResp; } 
        [ProtoMember(1)]
        public uint ErrorCode { get; set; }
        /// <summary>
        /// 创建成功后的房间 ID；失败时为 0
        /// </summary>
        [ProtoMember(2)]
        public long room_id { get; set; }
    }
}