using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;
using Hotfix.Utils;

namespace Hotfix.Scene.Gate.Handler.Home;
public class EntryHomeHandler : Message<EntryHomeReq>
{
    protected override async FTask Run(Session session, EntryHomeReq message)
    {
        var userId = JwtHelper.GetUserIdFromToken(message.token);
        Log.Info($" 用户 {userId} ws 连接建立 , session.address {session.Address}");
    }
}