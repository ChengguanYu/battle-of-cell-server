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
    public partial class SessionHeartbeatPing : AMessage, IRequest
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
            timestamp = default;
            MessageObjectPool<SessionHeartbeatPing>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.SessionHeartbeatPing; } 
        [ProtoIgnore]
        public SessionHeartbeatPong ResponseType { get; set; }
        [ProtoMember(1)]
        public ulong timestamp { get; set; }
    }
    /// <summary>
    /// 服务端心跳确认。sequence 原样回显 SessionHeartbeatPing.sequence。
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class SessionHeartbeatPong : AMessage, IResponse
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
            ErrorCode = 0;
            timestamp = default;
            MessageObjectPool<SessionHeartbeatPong>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.SessionHeartbeatPong; } 
        [ProtoMember(2)]
        public uint ErrorCode { get; set; }
        [ProtoMember(1)]
        public ulong timestamp { get; set; }
    }
    [Serializable]
    [ProtoContract]
    public partial class vec2d : AMessage, IDisposable
    {
        public static vec2d Create(bool autoReturn = true)
        {
            var vec2d = MessageObjectPool<vec2d>.Rent();
            vec2d.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                vec2d.SetIsPool(false);
            }
            
            return vec2d;
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
            x = default;
            y = default;
            MessageObjectPool<vec2d>.Return(this);
        }
        [ProtoMember(1)]
        public long x { get; set; }
        [ProtoMember(2)]
        public long y { get; set; }
    }
    [Serializable]
    [ProtoContract]
    public partial class position2d : AMessage, IDisposable
    {
        public static position2d Create(bool autoReturn = true)
        {
            var position2d = MessageObjectPool<position2d>.Rent();
            position2d.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                position2d.SetIsPool(false);
            }
            
            return position2d;
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
            x = default;
            y = default;
            MessageObjectPool<position2d>.Return(this);
        }
        [ProtoMember(1)]
        public int x { get; set; }
        [ProtoMember(2)]
        public int y { get; set; }
    }
    [Serializable]
    [ProtoContract]
    public partial class player : AMessage, IDisposable
    {
        public static player Create(bool autoReturn = true)
        {
            var player = MessageObjectPool<player>.Rent();
            player.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                player.SetIsPool(false);
            }
            
            return player;
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
            if (direction != null)
            {
                direction.Dispose();
                direction = null;
            }
            speed = default;
            if (position != null)
            {
                position.Dispose();
                position = null;
            }
            eid = default;
            MessageObjectPool<player>.Return(this);
        }
        [ProtoMember(1)]
        public vec2d direction { get; set; }
        [ProtoMember(2)]
        public long speed { get; set; }
        [ProtoMember(3)]
        public position2d position { get; set; }
        [ProtoMember(4)]
        public uint eid { get; set; }
    }
    [Serializable]
    [ProtoContract]
    public partial class frame : AMessage, IMessage
    {
        public static frame Create(bool autoReturn = true)
        {
            var frame = MessageObjectPool<frame>.Rent();
            frame.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                frame.SetIsPool(false);
            }
            
            return frame;
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
            op = default;
            if (data != null)
            {
                data.Dispose();
                data = null;
            }
            MessageObjectPool<frame>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.frame; } 
        [ProtoMember(2)]
        public Op op { get; set; }
        [ProtoMember(3)]
        public player data { get; set; }
    }
    [Serializable]
    [ProtoContract]
    public partial class server_frame : AMessage, IDisposable
    {
        public static server_frame Create(bool autoReturn = true)
        {
            var server_frame = MessageObjectPool<server_frame>.Rent();
            server_frame.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                server_frame.SetIsPool(false);
            }
            
            return server_frame;
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
            foreach (var __t in frames) __t.Dispose();
            frames.Clear();
            frame_number = default;
            randomSeed = default;
            if (meta != null)
            {
                meta.Dispose();
                meta = null;
            }
            MessageObjectPool<server_frame>.Return(this);
        }
        [ProtoMember(1)]
        public List<frame> frames { get; set; } = new List<frame>();
        [ProtoMember(2)]
        public ulong frame_number { get; set; }
        [ProtoMember(3)]
        public uint randomSeed { get; set; }
        [ProtoMember(4)]
        public MetaData meta { get; set; }
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
            room_id = default;
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
        /// <summary>
        /// 匹配成功后的房间 ID；失败时为 0
        /// </summary>
        [ProtoMember(5)]
        public long room_id { get; set; }
    }
}