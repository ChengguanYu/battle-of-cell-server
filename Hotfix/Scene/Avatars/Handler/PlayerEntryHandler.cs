using Entity.Common;
using Entity.Models;
using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;
using Hotfix.Scene.Http.Repositories;
using Hotfix.Scene.Avatars.Service;
using Hotfix.Utils;
namespace Hotfix.Scene.Avatars.Handler;

public class PlayerEntryHandler : MessageRPC<PlayerEntryReq,PlayerEntryResp>
{
    protected override async FTask Run(Session session,PlayerEntryReq req , PlayerEntryResp resp , Action reply)
    {
         User? user = await UserDao.FindByIdAsync(req.userId);
         if (user == null)
         {
             resp.SetError(StatusCode.PlayerNotFound);
             reply();
             return;
         }
         var srv  = session.Scene.GetComponent<AvatarsService>();
         // 加载到内存中
         var result = await srv.LoadPlayer(user.Id);

         if (!result.IsSuccess)
         {
             resp.SetError(StatusCode.LoadPlayerFailed);
             reply();
             return;
         }
    }
}

