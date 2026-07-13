using Entity.Common;
using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;
using Fantasy.Platform.Net;
using Hotfix.Scene.Gate.Service;
using Hotfix.Utils;

namespace Hotfix.Scene.Gate.Handler.Home;

public class EntryHomeHandler : MessageRPC<EntryHomeReq, EntryHomeRes>
{
    protected override async FTask Run(Session session, EntryHomeReq request, EntryHomeRes response, Action reply)
    {
        // 1. 验证 token
        var userId = JwtHelper.GetUserIdFromToken(request.token);
        if (userId == null)
        {
            response.SetError(ErrorCode.TokenInvalid);
            reply(); // 必须回复，即使失败
            return;
        }

        Log.Info($"用户 {userId} ws 连接建立, remoteEndPoint {session.RemoteEndPoint}");

        // 从 Scene 取全局组件（OnCreateScene 时挂上）
        var sessionService = session.Scene.GetComponent<SessionService>();
        if (!await sessionService.EntryHome(userId.Value))
        {
            response.SetError(ErrorCode.SessionEntryFailed);
            reply();
            return;
        }

        // 成功
        response.ok = true;
        response.SetOk();
        reply(); // 发送响应
    }
}
