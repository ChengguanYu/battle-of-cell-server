using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;
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
            response.ErrorCode = 1; // 错误码
            reply(); // 必须回复，即使失败
            return;
        }

        Log.Info($"用户 {userId} ws 连接建立, remoteEndPoint {session.RemoteEndPoint}");

        if (!await SessionService.EntryHome(userId.Value))
        {
            response.ErrorCode = 2;
            reply();
            return;
        }
        // 成功
        response.ok = true;
        response.ErrorCode = 0;
        reply(); // 发送响应
    }
}