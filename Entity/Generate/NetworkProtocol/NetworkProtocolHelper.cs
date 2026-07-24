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
		public static async FTask<PlayerMatchResp> PlayerMatchReq(this Session session, PlayerMatchReq PlayerMatchReq_request)
		{
			return (PlayerMatchResp)await session.Call(PlayerMatchReq_request);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<PlayerMatchResp> PlayerMatchReq(this Session session)
		{
			using var PlayerMatchReq_request = Fantasy.PlayerMatchReq.Create();
			return (PlayerMatchResp)await session.Call(PlayerMatchReq_request);
		}

   }
}