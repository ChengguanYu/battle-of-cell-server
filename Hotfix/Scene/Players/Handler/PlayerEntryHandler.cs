using Entity.Models;
using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;
using Hotfix.Scene.Http.Repositories;
using Hotfix.Scene.Players.Service;
using Microsoft.AspNetCore.SignalR;

namespace Hotfix.Scene.Players.Handler;

public class PlayerEntryHandler : MessageRPC<PlayerEntryReq,PlayerEntryResp>
{
    protected override async FTask Run(Session session,PlayerEntryReq req , PlayerEntryResp resp , Action reply)
    {
         User? user = await UserDao.FindByIdAsync(req.userId);
         if (user == null)
         {
             resp.ErrorCode = 1;
             reply();
             return;
         }
         var srv  = session.Scene.GetComponent<PlayersService>();
         // 加载到内存中
         var result = await srv.LoadPlayer(user.Id);
         
         if (!result.IsSuccess)
         {
             resp.ErrorCode = 1;
             reply();
             return;
         }
    }
}

