using System.Runtime.CompilerServices;
using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using System.Collections.Generic;
#pragma warning disable CS8618
namespace Fantasy
{
   public static class NetworkProtocolHelper
   {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void ShapeVertex(this Session session, ShapeVertex ShapeVertex_message)
		{
			session.Send(ShapeVertex_message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void ShapeVertex(this Session session, long x, long y)
		{
			using var ShapeVertex_message = Fantasy.ShapeVertex.Create();
			ShapeVertex_message.x = x;
			ShapeVertex_message.y = y;
			session.Send(ShapeVertex_message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void ShapeData(this Session session, ShapeData ShapeData_message)
		{
			session.Send(ShapeData_message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void ShapeData(this Session session, List<ShapeVertex> vertices)
		{
			using var ShapeData_message = Fantasy.ShapeData.Create();
			ShapeData_message.vertices = vertices;
			session.Send(ShapeData_message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<EntryHomeResp> EntryHomeReq(this Session session, EntryHomeReq EntryHomeReq_request)
		{
			return (EntryHomeResp)await session.Call(EntryHomeReq_request);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<EntryHomeResp> EntryHomeReq(this Session session, string token)
		{
			using var EntryHomeReq_request = Fantasy.EntryHomeReq.Create();
			EntryHomeReq_request.token = token;
			return (EntryHomeResp)await session.Call(EntryHomeReq_request);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<SessionHeartbeatPong> SessionHeartbeatPing(this Session session, SessionHeartbeatPing SessionHeartbeatPing_request)
		{
			return (SessionHeartbeatPong)await session.Call(SessionHeartbeatPing_request);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<SessionHeartbeatPong> SessionHeartbeatPing(this Session session, ulong timestamp)
		{
			using var SessionHeartbeatPing_request = Fantasy.SessionHeartbeatPing.Create();
			SessionHeartbeatPing_request.timestamp = timestamp;
			return (SessionHeartbeatPong)await session.Call(SessionHeartbeatPing_request);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void ServerFrame(this Session session, ServerFrame ServerFrame_message)
		{
			session.Send(ServerFrame_message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void ServerFrame(this Session session, List<Frame> frames, ulong frame_number, uint random_seed, MetaData meta)
		{
			using var ServerFrame_message = Fantasy.ServerFrame.Create();
			ServerFrame_message.frames = frames;
			ServerFrame_message.frame_number = frame_number;
			ServerFrame_message.random_seed = random_seed;
			ServerFrame_message.meta = meta;
			session.Send(ServerFrame_message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void ClientFrame(this Session session, ClientFrame ClientFrame_message)
		{
			session.Send(ClientFrame_message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void ClientFrame(this Session session, List<Frame> frames, ulong frame_number)
		{
			using var ClientFrame_message = Fantasy.ClientFrame.Create();
			ClientFrame_message.frames = frames;
			ClientFrame_message.frame_number = frame_number;
			session.Send(ClientFrame_message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<PlayerLeaveRoomResp> PlayerLeaveRoomReq(this Session session, PlayerLeaveRoomReq PlayerLeaveRoomReq_request)
		{
			return (PlayerLeaveRoomResp)await session.Call(PlayerLeaveRoomReq_request);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<PlayerLeaveRoomResp> PlayerLeaveRoomReq(this Session session)
		{
			using var PlayerLeaveRoomReq_request = Fantasy.PlayerLeaveRoomReq.Create();
			return (PlayerLeaveRoomResp)await session.Call(PlayerLeaveRoomReq_request);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void MetaData(this Session session, MetaData MetaData_message)
		{
			session.Send(MetaData_message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void MetaData(this Session session, uint status_code, long timestamp)
		{
			using var MetaData_message = Fantasy.MetaData.Create();
			MetaData_message.status_code = status_code;
			MetaData_message.timestamp = timestamp;
			session.Send(MetaData_message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void RespError(this Session session, RespError RespError_message)
		{
			session.Send(RespError_message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void RespError(this Session session, string message, List<string> args)
		{
			using var RespError_message = Fantasy.RespError.Create();
			RespError_message.message = message;
			RespError_message.args = args;
			session.Send(RespError_message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void WorldInit(this Session session, WorldInit WorldInit_message)
		{
			session.Send(WorldInit_message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void WorldInit(this Session session, ulong x_size, ulong y_size, List<ShapeData> shapes)
		{
			using var WorldInit_message = Fantasy.WorldInit.Create();
			WorldInit_message.x_size = x_size;
			WorldInit_message.y_size = y_size;
			WorldInit_message.shapes = shapes;
			session.Send(WorldInit_message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void HeroInit(this Session session, HeroInit HeroInit_message)
		{
			session.Send(HeroInit_message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void HeroInit(this Session session, Position2d position, uint entity_id)
		{
			using var HeroInit_message = Fantasy.HeroInit.Create();
			HeroInit_message.position = position;
			HeroInit_message.entity_id = entity_id;
			session.Send(HeroInit_message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<MatchResp> MatchReq(this Session session, MatchReq MatchReq_request)
		{
			return (MatchResp)await session.Call(MatchReq_request);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<MatchResp> MatchReq(this Session session, MatchType match_type)
		{
			using var MatchReq_request = Fantasy.MatchReq.Create();
			MatchReq_request.match_type = match_type;
			return (MatchResp)await session.Call(MatchReq_request);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<EntryRoomResp> EntryRoomReq(this Session session, EntryRoomReq EntryRoomReq_request)
		{
			return (EntryRoomResp)await session.Call(EntryRoomReq_request);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<EntryRoomResp> EntryRoomReq(this Session session)
		{
			using var EntryRoomReq_request = Fantasy.EntryRoomReq.Create();
			return (EntryRoomResp)await session.Call(EntryRoomReq_request);
		}

   }
}