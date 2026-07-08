using Entity.Models;

using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;
using Hotfix.Database;
using Hotfix.Scene.Gate.Service;
using Hotfix.Utils;
using Entity.VOs.session;
using Hotfix.Scene.Http.Repositories;

namespace Hotfix.Scene.Gate.Handler.Home;
public class EntryHomeHandler : Message<EntryHomeReq>
{
    protected override async FTask Run(Session session, EntryHomeReq message)
    {
        var userId = JwtHelper.GetUserIdFromToken(message.token);
        if (userId == null)
        {
            session.Dispose();
            return;
        }
        Log.Info($" 用户 {userId} ws 连接建立 , session.address {session.Address}");
        var user = UserDao.FindById(userId.Value);
        if (user == null)
        {
            Log.Warning($"用户 {userId} 不存在，断开连接");
            session.Dispose();
            return;
        }
        var usession = new WsSession(user);
        SessionManger.Instance.Add(usession);
    }
}