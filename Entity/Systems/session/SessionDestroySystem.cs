using Entity.Managers;
using Fantasy;
using Fantasy.Entitas.Interface;
using Fantasy.Network;

namespace Entity.Systems.session;

/// <summary>
/// Session 销毁时查找绑定的 WsSession，并迁移到 TimedOut。
/// 放在 Entity 程序集：会话生命周期基础设施，不随 Hotfix 热更。
/// </summary>
public sealed class SessionDestroySystem : DestroySystem<Session>
{
    protected override void Destroy(Session self)
    {
        if (!SessionManager.Instance.MarkTimedOutBySession(self))
        {
            return;
        }

        Log.Info($"Session 断开，WsSession 已迁移到 TimedOut: sessionId={self.Id}");
    }
}
