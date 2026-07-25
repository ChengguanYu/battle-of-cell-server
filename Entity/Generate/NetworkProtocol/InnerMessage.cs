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
    /// 玩家进入请求（Avatar 本域：加载玩家）
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
            user_id = default;
            MessageObjectPool<PlayerEntryReq>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.PlayerEntryReq; } 
        [ProtoIgnore]
        public PlayerEntryResp ResponseType { get; set; }
        [ProtoMember(1)]
        public long user_id { get; set; }
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
    /// Gate -> Avatar 客户端匹配转发请求（对应 Outer MatchReq）
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class AvatarRelayClientMatchReq : AMessage, IAddressRequest
    {
        public static AvatarRelayClientMatchReq Create(bool autoReturn = true)
        {
            var avatarRelayClientMatchReq = MessageObjectPool<AvatarRelayClientMatchReq>.Rent();
            avatarRelayClientMatchReq.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                avatarRelayClientMatchReq.SetIsPool(false);
            }
            
            return avatarRelayClientMatchReq;
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
            user_id = default;
            match_type = default;
            MessageObjectPool<AvatarRelayClientMatchReq>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.AvatarRelayClientMatchReq; } 
        [ProtoIgnore]
        public AvatarRelayClientMatchResp ResponseType { get; set; }
        [ProtoMember(1)]
        public long user_id { get; set; }
        [ProtoMember(2)]
        public MatchType match_type { get; set; }
    }
    /// <summary>
    /// Gate -> Avatar 客户端匹配响应
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class AvatarRelayClientMatchResp : AMessage, IAddressResponse
    {
        public static AvatarRelayClientMatchResp Create(bool autoReturn = true)
        {
            var avatarRelayClientMatchResp = MessageObjectPool<AvatarRelayClientMatchResp>.Rent();
            avatarRelayClientMatchResp.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                avatarRelayClientMatchResp.SetIsPool(false);
            }
            
            return avatarRelayClientMatchResp;
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
            MessageObjectPool<AvatarRelayClientMatchResp>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.AvatarRelayClientMatchResp; } 
        [ProtoMember(1)]
        public uint ErrorCode { get; set; }
    }
    /// <summary>
    /// Gate -> Avatar 主动退出房间转发请求（Avatar 门禁后转发到 Rooms）
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class AvatarRelayLeaveRoomReq : AMessage, IAddressRequest
    {
        public static AvatarRelayLeaveRoomReq Create(bool autoReturn = true)
        {
            var avatarRelayLeaveRoomReq = MessageObjectPool<AvatarRelayLeaveRoomReq>.Rent();
            avatarRelayLeaveRoomReq.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                avatarRelayLeaveRoomReq.SetIsPool(false);
            }
            
            return avatarRelayLeaveRoomReq;
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
            user_id = default;
            MessageObjectPool<AvatarRelayLeaveRoomReq>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.AvatarRelayLeaveRoomReq; } 
        [ProtoIgnore]
        public AvatarRelayLeaveRoomResp ResponseType { get; set; }
        [ProtoMember(1)]
        public long user_id { get; set; }
    }
    /// <summary>
    /// Gate -> Avatar 主动退出房间转发响应
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class AvatarRelayLeaveRoomResp : AMessage, IAddressResponse
    {
        public static AvatarRelayLeaveRoomResp Create(bool autoReturn = true)
        {
            var avatarRelayLeaveRoomResp = MessageObjectPool<AvatarRelayLeaveRoomResp>.Rent();
            avatarRelayLeaveRoomResp.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                avatarRelayLeaveRoomResp.SetIsPool(false);
            }
            
            return avatarRelayLeaveRoomResp;
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
            MessageObjectPool<AvatarRelayLeaveRoomResp>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.AvatarRelayLeaveRoomResp; } 
        [ProtoMember(1)]
        public uint ErrorCode { get; set; }
        /// <summary>
        /// 离开成功后的房间 ID；失败时为 0
        /// </summary>
        [ProtoMember(2)]
        public long room_id { get; set; }
    }
    /// <summary>
    /// Gate -> Avatar 客户端进房转发请求（对应 Outer EntryRoomReq）
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class AvatarRelayEntryRoomReq : AMessage, IAddressRequest
    {
        public static AvatarRelayEntryRoomReq Create(bool autoReturn = true)
        {
            var avatarRelayEntryRoomReq = MessageObjectPool<AvatarRelayEntryRoomReq>.Rent();
            avatarRelayEntryRoomReq.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                avatarRelayEntryRoomReq.SetIsPool(false);
            }
            
            return avatarRelayEntryRoomReq;
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
            user_id = default;
            MessageObjectPool<AvatarRelayEntryRoomReq>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.AvatarRelayEntryRoomReq; } 
        [ProtoIgnore]
        public AvatarRelayEntryRoomResp ResponseType { get; set; }
        [ProtoMember(1)]
        public long user_id { get; set; }
    }
    /// <summary>
    /// Gate -> Avatar 客户端进房转发响应
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class AvatarRelayEntryRoomResp : AMessage, IAddressResponse
    {
        public static AvatarRelayEntryRoomResp Create(bool autoReturn = true)
        {
            var avatarRelayEntryRoomResp = MessageObjectPool<AvatarRelayEntryRoomResp>.Rent();
            avatarRelayEntryRoomResp.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                avatarRelayEntryRoomResp.SetIsPool(false);
            }
            
            return avatarRelayEntryRoomResp;
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
            MessageObjectPool<AvatarRelayEntryRoomResp>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.AvatarRelayEntryRoomResp; } 
        [ProtoMember(1)]
        public uint ErrorCode { get; set; }
        /// <summary>
        /// 进入成功后的房间 ID；失败时为 0
        /// </summary>
        [ProtoMember(2)]
        public long room_id { get; set; }
    }
    /// <summary>
    /// Gate -> Avatar 清理玩家通知（Avatar 本域下线编排，非跨 Scene 业务转发）
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
            user_id = default;
            reason = default;
            MessageObjectPool<AvatarCleanupNotify>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.AvatarCleanupNotify; } 
        [ProtoMember(1)]
        public long user_id { get; set; }
        /// <summary>
        /// 清理原因，如 timed_out_grace_expired
        /// </summary>
        [ProtoMember(2)]
        public string reason { get; set; }
    }
    /// <summary>
    /// Gate -> Avatar 客户端帧转发通知（Avatar 门禁后转发到 Rooms）
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class AvatarRelayClientFrameNotify : AMessage, IAddressMessage
    {
        public static AvatarRelayClientFrameNotify Create(bool autoReturn = true)
        {
            var avatarRelayClientFrameNotify = MessageObjectPool<AvatarRelayClientFrameNotify>.Rent();
            avatarRelayClientFrameNotify.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                avatarRelayClientFrameNotify.SetIsPool(false);
            }
            
            return avatarRelayClientFrameNotify;
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
            user_id = default;
            frame_number = default;
            frames.Clear();
            MessageObjectPool<AvatarRelayClientFrameNotify>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.AvatarRelayClientFrameNotify; } 
        [ProtoMember(1)]
        public long user_id { get; set; }
        [ProtoMember(2)]
        public ulong frame_number { get; set; }
        /// <summary>
        /// 客户端本帧操作列表（与 Outer ClientFrame.frames 同构）
        /// </summary>
        [ProtoMember(3)]
        public List<Frame> frames { get; set; } = new List<Frame>();
    }
    /// <summary>
    /// Avatar -> Match 客户端匹配转发请求（对应 Outer MatchReq）
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class InnerClientMatchReq : AMessage, IAddressRequest
    {
        public static InnerClientMatchReq Create(bool autoReturn = true)
        {
            var innerClientMatchReq = MessageObjectPool<InnerClientMatchReq>.Rent();
            innerClientMatchReq.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                innerClientMatchReq.SetIsPool(false);
            }
            
            return innerClientMatchReq;
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
            user_id = default;
            match_type = default;
            MessageObjectPool<InnerClientMatchReq>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.InnerClientMatchReq; } 
        [ProtoIgnore]
        public InnerClientMatchResp ResponseType { get; set; }
        [ProtoMember(1)]
        public long user_id { get; set; }
        [ProtoMember(2)]
        public MatchType match_type { get; set; }
    }
    /// <summary>
    /// Avatar -> Match 客户端匹配转发响应
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class InnerClientMatchResp : AMessage, IAddressResponse
    {
        public static InnerClientMatchResp Create(bool autoReturn = true)
        {
            var innerClientMatchResp = MessageObjectPool<InnerClientMatchResp>.Rent();
            innerClientMatchResp.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                innerClientMatchResp.SetIsPool(false);
            }
            
            return innerClientMatchResp;
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
            MessageObjectPool<InnerClientMatchResp>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.InnerClientMatchResp; } 
        [ProtoMember(1)]
        public uint ErrorCode { get; set; }
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
            user_id = default;
            reason = default;
            MessageObjectPool<RoomsPlayerLeaveNotify>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.RoomsPlayerLeaveNotify; } 
        [ProtoMember(1)]
        public long user_id { get; set; }
        /// <summary>
        /// 离房原因，如 timed_out_grace_expired
        /// </summary>
        [ProtoMember(2)]
        public string reason { get; set; }
    }
    /// <summary>
    /// Avatar -> Rooms 主动离房请求（在线退出，等待结果）
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class RoomsLeaveReq : AMessage, IAddressRequest
    {
        public static RoomsLeaveReq Create(bool autoReturn = true)
        {
            var roomsLeaveReq = MessageObjectPool<RoomsLeaveReq>.Rent();
            roomsLeaveReq.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                roomsLeaveReq.SetIsPool(false);
            }
            
            return roomsLeaveReq;
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
            user_id = default;
            reason = default;
            MessageObjectPool<RoomsLeaveReq>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.RoomsLeaveReq; } 
        [ProtoIgnore]
        public RoomsLeaveResp ResponseType { get; set; }
        [ProtoMember(1)]
        public long user_id { get; set; }
        /// <summary>
        /// 离房原因，如 client_leave
        /// </summary>
        [ProtoMember(2)]
        public string reason { get; set; }
    }
    /// <summary>
    /// Avatar -> Rooms 主动离房响应
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class RoomsLeaveResp : AMessage, IAddressResponse
    {
        public static RoomsLeaveResp Create(bool autoReturn = true)
        {
            var roomsLeaveResp = MessageObjectPool<RoomsLeaveResp>.Rent();
            roomsLeaveResp.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                roomsLeaveResp.SetIsPool(false);
            }
            
            return roomsLeaveResp;
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
            MessageObjectPool<RoomsLeaveResp>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.RoomsLeaveResp; } 
        [ProtoMember(1)]
        public uint ErrorCode { get; set; }
        /// <summary>
        /// 离开成功后的房间 ID；失败时为 0
        /// </summary>
        [ProtoMember(2)]
        public long room_id { get; set; }
    }
    /// <summary>
    /// Avatar -> Rooms 客户端帧转发
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class RoomsClientFrameNotify : AMessage, IAddressMessage
    {
        public static RoomsClientFrameNotify Create(bool autoReturn = true)
        {
            var roomsClientFrameNotify = MessageObjectPool<RoomsClientFrameNotify>.Rent();
            roomsClientFrameNotify.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                roomsClientFrameNotify.SetIsPool(false);
            }
            
            return roomsClientFrameNotify;
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
            user_id = default;
            frame_number = default;
            frames.Clear();
            MessageObjectPool<RoomsClientFrameNotify>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.RoomsClientFrameNotify; } 
        [ProtoMember(1)]
        public long user_id { get; set; }
        [ProtoMember(2)]
        public ulong frame_number { get; set; }
        /// <summary>
        /// 客户端本帧操作列表（与 Outer ClientFrame.frames 同构）
        /// </summary>
        [ProtoMember(3)]
        public List<Frame> frames { get; set; } = new List<Frame>();
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
            is_empty = default;
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
        public bool is_empty { get; set; }
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
            user_id = default;
            MessageObjectPool<RoomsCreateReq>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.RoomsCreateReq; } 
        [ProtoIgnore]
        public RoomsCreateResp ResponseType { get; set; }
        [ProtoMember(1)]
        public long user_id { get; set; }
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
    /// <summary>
    /// Avatar -> Rooms 客户端进房请求（读 Redis 匹配结果后 Entry）
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class RoomsEntryRoomReq : AMessage, IAddressRequest
    {
        public static RoomsEntryRoomReq Create(bool autoReturn = true)
        {
            var roomsEntryRoomReq = MessageObjectPool<RoomsEntryRoomReq>.Rent();
            roomsEntryRoomReq.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                roomsEntryRoomReq.SetIsPool(false);
            }
            
            return roomsEntryRoomReq;
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
            user_id = default;
            MessageObjectPool<RoomsEntryRoomReq>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.RoomsEntryRoomReq; } 
        [ProtoIgnore]
        public RoomsEntryRoomResp ResponseType { get; set; }
        [ProtoMember(1)]
        public long user_id { get; set; }
    }
    /// <summary>
    /// Avatar -> Rooms 客户端进房响应
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class RoomsEntryRoomResp : AMessage, IAddressResponse
    {
        public static RoomsEntryRoomResp Create(bool autoReturn = true)
        {
            var roomsEntryRoomResp = MessageObjectPool<RoomsEntryRoomResp>.Rent();
            roomsEntryRoomResp.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                roomsEntryRoomResp.SetIsPool(false);
            }
            
            return roomsEntryRoomResp;
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
            MessageObjectPool<RoomsEntryRoomResp>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.RoomsEntryRoomResp; } 
        [ProtoMember(1)]
        public uint ErrorCode { get; set; }
        /// <summary>
        /// 进入成功后的房间 ID；失败时为 0
        /// </summary>
        [ProtoMember(2)]
        public long room_id { get; set; }
    }
}