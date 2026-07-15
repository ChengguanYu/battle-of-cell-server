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

   }
}