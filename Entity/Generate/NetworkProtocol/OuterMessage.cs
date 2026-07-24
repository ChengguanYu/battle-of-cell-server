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
    public partial class ServerFrame : AMessage, IMessage
    {
        public static ServerFrame Create(bool autoReturn = true)
        {
            var serverFrame = MessageObjectPool<ServerFrame>.Rent();
            serverFrame.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                serverFrame.SetIsPool(false);
            }
            
            return serverFrame;
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
            random_seed = default;
            if (meta != null)
            {
                meta.Dispose();
                meta = null;
            }
            MessageObjectPool<ServerFrame>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.ServerFrame; } 
        [ProtoMember(1)]
        public List<Frame> frames { get; set; } = new List<Frame>();
        [ProtoMember(2)]
        public ulong frame_number { get; set; }
        [ProtoMember(3)]
        public uint random_seed { get; set; }
        [ProtoMember(4)]
        public MetaData meta { get; set; }
    }
    [Serializable]
    [ProtoContract]
    public partial class ClientFrame : AMessage, IMessage
    {
        public static ClientFrame Create(bool autoReturn = true)
        {
            var clientFrame = MessageObjectPool<ClientFrame>.Rent();
            clientFrame.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                clientFrame.SetIsPool(false);
            }
            
            return clientFrame;
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
            MessageObjectPool<ClientFrame>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.ClientFrame; } 
        [ProtoMember(1)]
        public List<Frame> frames { get; set; } = new List<Frame>();
        [ProtoMember(2)]
        public ulong frame_number { get; set; }
    }
    [Serializable]
    [ProtoContract]
    public partial class Vec2d : AMessage, IDisposable
    {
        public static Vec2d Create(bool autoReturn = true)
        {
            var vec2d = MessageObjectPool<Vec2d>.Rent();
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
            MessageObjectPool<Vec2d>.Return(this);
        }
        [ProtoMember(1)]
        public long x { get; set; }
        [ProtoMember(2)]
        public long y { get; set; }
    }
    [Serializable]
    [ProtoContract]
    public partial class Position2d : AMessage, IDisposable
    {
        public static Position2d Create(bool autoReturn = true)
        {
            var position2d = MessageObjectPool<Position2d>.Rent();
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
            MessageObjectPool<Position2d>.Return(this);
        }
        [ProtoMember(1)]
        public int x { get; set; }
        [ProtoMember(2)]
        public int y { get; set; }
    }
    [Serializable]
    [ProtoContract]
    public partial class Player : AMessage, IDisposable
    {
        public static Player Create(bool autoReturn = true)
        {
            var player = MessageObjectPool<Player>.Rent();
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
            MessageObjectPool<Player>.Return(this);
        }
        [ProtoMember(1)]
        public Vec2d direction { get; set; }
        [ProtoMember(2)]
        public long speed { get; set; }
        [ProtoMember(3)]
        public Position2d position { get; set; }
        [ProtoMember(4)]
        public uint eid { get; set; }
    }
    [Serializable]
    [ProtoContract]
    public partial class Frame : AMessage, IDisposable
    {
        public static Frame Create(bool autoReturn = true)
        {
            var frame = MessageObjectPool<Frame>.Rent();
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
            MessageObjectPool<Frame>.Return(this);
        }
        [ProtoMember(2)]
        public Op op { get; set; }
        [ProtoMember(3)]
        public Player data { get; set; }
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
    /// 客户端主动退出房间请求
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class PlayerLeaveRoomReq : AMessage, IRequest
    {
        public static PlayerLeaveRoomReq Create(bool autoReturn = true)
        {
            var playerLeaveRoomReq = MessageObjectPool<PlayerLeaveRoomReq>.Rent();
            playerLeaveRoomReq.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                playerLeaveRoomReq.SetIsPool(false);
            }
            
            return playerLeaveRoomReq;
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
            MessageObjectPool<PlayerLeaveRoomReq>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.PlayerLeaveRoomReq; } 
        [ProtoIgnore]
        public PlayerLeaveRoomResp ResponseType { get; set; }
    }
    [Serializable]
    [ProtoContract]
    public partial class PlayerLeaveRoomResp : AMessage, IResponse
    {
        public static PlayerLeaveRoomResp Create(bool autoReturn = true)
        {
            var playerLeaveRoomResp = MessageObjectPool<PlayerLeaveRoomResp>.Rent();
            playerLeaveRoomResp.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                playerLeaveRoomResp.SetIsPool(false);
            }
            
            return playerLeaveRoomResp;
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
            MessageObjectPool<PlayerLeaveRoomResp>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.PlayerLeaveRoomResp; } 
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
        /// 离开成功后的房间 ID；失败时为 0
        /// </summary>
        [ProtoMember(5)]
        public long room_id { get; set; }
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