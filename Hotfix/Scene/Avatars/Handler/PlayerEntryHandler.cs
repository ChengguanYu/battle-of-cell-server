using Entity.Models;
using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;
using Hotfix.Scene.Http.Repositories;
using Hotfix.Scene.Avatars.Service;
using Hotfix.Utils;
using FScene = Fantasy.Scene;

namespace Hotfix.Scene.Avatars.Handler;

public sealed class PlayerEntryHandler : AddressRPC<FScene, PlayerEntryReq, PlayerEntryResp>
{
    protected override async FTask Run(FScene scene, PlayerEntryReq req, PlayerEntryResp resp, Action reply)
    {
        User? user = await UserDao.FindByIdAsync(req.user_id);
        if (user == null)
        {
            resp.SetError(StatusCode.PlayerNotFound);
            reply();
            return;
        }
        IAvatarsService avatarsService = scene.GetComponent<AvatarsService>();
        // 加载到内存中
        var result = await avatarsService.LoadPlayer(user.Id);
        if (!result.IsSuccess)
        {
            resp.SetError(StatusCode.LoadPlayerFailed);
            reply();
            return;
        }

        resp.SetOk();
        reply();
    }
}
