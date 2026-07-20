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
    /// Avatar -> Match 匹配请求
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
    /// Avatar -> Match 匹配响应
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
    /// Match/Avatar -> Rooms 进入房间请求
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
    /// Match/Avatar -> Rooms 进入房间响应
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
}