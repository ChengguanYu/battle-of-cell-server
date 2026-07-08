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
		public static async FTask<EntryHomeRes> EntryHomeReq(this Session session, EntryHomeReq EntryHomeReq_request)
		{
			return (EntryHomeRes)await session.Call(EntryHomeReq_request);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<EntryHomeRes> EntryHomeReq(this Session session, string token)
		{
			using var EntryHomeReq_request = Fantasy.EntryHomeReq.Create();
			EntryHomeReq_request.token = token;
			return (EntryHomeRes)await session.Call(EntryHomeReq_request);
		}

   }
}