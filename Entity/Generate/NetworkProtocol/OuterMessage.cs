using LightProto;
using System;
using MemoryPack;
using System.Collections.Generic;
using Fantasy;
using Fantasy.Pool;
using Fantasy.Network.Interface;
using Fantasy.Serialize;

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8618
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
namespace Fantasy
{
    /// <summary>
    /// 客户端进家园请求
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class EntryHomeReq : AMessage, IRequest
    {
        public static EntryHomeReq Create(bool autoReturn = true)
        {
            var entryHomeReq = MessageObjectPool<EntryHomeReq>.Rent();
            entryHomeReq.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                entryHomeReq.SetIsPool(false);
            }
            
            return entryHomeReq;
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
            token = default;
            MessageObjectPool<EntryHomeReq>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.EntryHomeReq; } 
        [ProtoIgnore]
        public EntryHomeResp ResponseType { get; set; }
        [ProtoMember(1)]
        public string token { get; set; }
    }
    [Serializable]
    [ProtoContract]
    public partial class EntryHomeResp : AMessage, IResponse
    {
        public static EntryHomeResp Create(bool autoReturn = true)
        {
            var entryHomeResp = MessageObjectPool<EntryHomeResp>.Rent();
            entryHomeResp.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                entryHomeResp.SetIsPool(false);
            }
            
            return entryHomeResp;
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
            if (meta != null)
            {
                meta.Dispose();
                meta = null;
            }
            foreach (var __t in error) __t.Dispose();
            error.Clear();
            ok = default;
            MessageObjectPool<EntryHomeResp>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.EntryHomeResp; } 
        [ProtoMember(4)]
        public uint ErrorCode { get; set; }
        [ProtoMember(1)]
        public MetaData meta { get; set; }
        [ProtoMember(2)]
        public List<RespError> error { get; set; } = new List<RespError>();
        /// <summary>
        /// 业务是否成功（与 meta 同级；true 时 LightProto 会写出该字段）
        /// </summary>
        [ProtoMember(3)]
        public bool ok { get; set; }
    }
    /// <summary>
    /// 客户端心跳。sequence 在单次连接内从 1 开始递增，0 保留。
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class SessionHeartbeatPing : AMessage, IMessage
    {
        public static SessionHeartbeatPing Create(bool autoReturn = true)
        {
            var sessionHeartbeatPing = MessageObjectPool<SessionHeartbeatPing>.Rent();
            sessionHeartbeatPing.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                sessionHeartbeatPing.SetIsPool(false);
            }
            
            return sessionHeartbeatPing;
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
            sequence = default;
            MessageObjectPool<SessionHeartbeatPing>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.SessionHeartbeatPing; } 
        [ProtoMember(1)]
        public uint sequence { get; set; }
    }
    /// <summary>
    /// 服务端心跳确认。sequence 原样回显 SessionHeartbeatPing.sequence。
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class SessionHeartbeatPong : AMessage, IMessage
    {
        public static SessionHeartbeatPong Create(bool autoReturn = true)
        {
            var sessionHeartbeatPong = MessageObjectPool<SessionHeartbeatPong>.Rent();
            sessionHeartbeatPong.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                sessionHeartbeatPong.SetIsPool(false);
            }
            
            return sessionHeartbeatPong;
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
            sequence = default;
            MessageObjectPool<SessionHeartbeatPong>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.SessionHeartbeatPong; } 
        [ProtoMember(1)]
        public uint sequence { get; set; }
    }
    [Serializable]
    [ProtoContract]
    public partial class MetaData : AMessage, IMessage
    {
        public static MetaData Create(bool autoReturn = true)
        {
            var metaData = MessageObjectPool<MetaData>.Rent();
            metaData.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                metaData.SetIsPool(false);
            }
            
            return metaData;
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
            status_code = default;
            timestamp = default;
            MessageObjectPool<MetaData>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.MetaData; } 
        [ProtoMember(1)]
        public uint status_code { get; set; }
        [ProtoMember(2)]
        public long timestamp { get; set; }
    }
    [Serializable]
    [ProtoContract]
    public partial class RespError : AMessage, IMessage
    {
        public static RespError Create(bool autoReturn = true)
        {
            var respError = MessageObjectPool<RespError>.Rent();
            respError.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                respError.SetIsPool(false);
            }
            
            return respError;
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
            message = default;
            args.Clear();
            MessageObjectPool<RespError>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.RespError; } 
        [ProtoMember(1)]
        public string message { get; set; }
        [ProtoMember(2)]
        public List<string> args { get; set; } = new List<string>();
    }
    /// <summary>
    /// 客户端发起匹配请求
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class PlayerMatchReq : AMessage, IRequest
    {
        public static PlayerMatchReq Create(bool autoReturn = true)
        {
            var playerMatchReq = MessageObjectPool<PlayerMatchReq>.Rent();
            playerMatchReq.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                playerMatchReq.SetIsPool(false);
            }
            
            return playerMatchReq;
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
            MessageObjectPool<PlayerMatchReq>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.PlayerMatchReq; } 
        [ProtoIgnore]
        public PlayerMatchResp ResponseType { get; set; }
    }
    [Serializable]
    [ProtoContract]
    public partial class PlayerMatchResp : AMessage, IResponse
    {
        public static PlayerMatchResp Create(bool autoReturn = true)
        {
            var playerMatchResp = MessageObjectPool<PlayerMatchResp>.Rent();
            playerMatchResp.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                playerMatchResp.SetIsPool(false);
            }
            
            return playerMatchResp;
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
            if (meta != null)
            {
                meta.Dispose();
                meta = null;
            }
            foreach (var __t in error) __t.Dispose();
            error.Clear();
            ok = default;
            MessageObjectPool<PlayerMatchResp>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.PlayerMatchResp; } 
        [ProtoMember(4)]
        public uint ErrorCode { get; set; }
        [ProtoMember(1)]
        public MetaData meta { get; set; }
        [ProtoMember(2)]
        public List<RespError> error { get; set; } = new List<RespError>();
        /// <summary>
        /// 业务是否成功（与 meta 同级；true 时 LightProto 会写出该字段）
        /// </summary>
        [ProtoMember(3)]
        public bool ok { get; set; }
    }
}