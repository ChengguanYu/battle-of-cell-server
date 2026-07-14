using Entity.Common;
using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;
using Fantasy.Platform.Net;
using Hotfix.Scene.Gate.Service;
using Hotfix.Utils;

namespace Hotfix.Scene.Gate.Handler.Home;
// 由于不是使用的官方客户端，ErrorCode 无法使用，在对客户端的响应中需要手动的携带状态码
public class EntryHomeHandler : MessageRPC<EntryHomeReq, EntryHomeRes>
{
    protected override async FTask Run(Session session, EntryHomeReq request, EntryHomeRes response, Action reply)
    {
        var userId = JwtHelper.GetUserIdFromToken(request.token);
        if (userId == null)
        {
            response.status = (uint)StatusCode.TokenInvalid;
            reply();
            session.Dispose();
            return;
        }

        Log.Info($"用户 {userId} ws 连接建立, remoteEndPoint {session.RemoteEndPoint}");

        var sessionService = session.Scene.GetComponent<SessionService>();
        if (!await sessionService.EntryHome(userId.Value))
        {
            response.status = (uint)StatusCode.SessionEntryFailed;
            reply();
            session.Dispose();
            return;
        }


        response.status = (uint)StatusCode.Ok;
        // response.SetOk();
        reply(); // 发送响应
    }
}
