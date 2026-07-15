using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;
using Fantasy.Platform.Net;
using Hotfix.Scene.Gate.Service;
using Hotfix.Utils;

namespace Hotfix.Scene.Gate.Handler.Home;
public class EntryHomeHandler : MessageRPC<EntryHomeReq, EntryHomeResp>
{
    protected override async FTask Run(Session session, EntryHomeReq request, EntryHomeResp response, Action reply)
    {
        var userId = JwtHelper.GetUserIdFromToken(request.token);
        if (userId == null)
        {
            ReplyError(session, response, reply, StatusCode.TokenInvalid, StatusCode.TokenInvalid.ToMessage());
            return;
        }

        Log.Info($"用户 {userId} ws 连接建立, remoteEndPoint {session.RemoteEndPoint}");

        var sessionService = session.Scene.GetComponent<SessionService>();
        var result = await sessionService.EntryHome(userId.Value);
        if (!result.IsSuccess)
        {
            Log.Warning($"用户 {userId} 进入失败：{result.Reason}");
            ReplyError(session, response, reply, StatusCode.SessionEntryFailed, result.Reason);
            return;
        }

        ReplyOk(response, reply);
        response.ok = true;
    }

    /// <summary>标记响应成功并回复。</summary>
    private static void ReplyOk(EntryHomeResp response, Action reply)
    {
        response.SetOk();
        reply();
    }

    /// <summary>设置状态码与错误详情后回复并断开连接。</summary>
    private static void ReplyError(Session session, EntryHomeResp response, Action reply, StatusCode code, string reason)
    {
        response.SetStatus(code);
        var error = RespError.Create();
        error.message = reason;
        response.AddError(error);
        reply();
        session.Dispose();
    }
}
