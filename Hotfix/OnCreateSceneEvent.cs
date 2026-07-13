using Fantasy;
using Fantasy.Async;
using Fantasy.Event;
using Hotfix.Scene.Gate.Service;
using Hotfix.Scene.Avatars.Service;

namespace Hotfix;

public sealed class OnCreateSceneEvent : AsyncEventSystem<OnCreateScene>
{
    protected override async FTask Handler(OnCreateScene self)
    {
        var scene = self.Scene;

        switch (scene.SceneType)
        {
            case SceneType.Gate:
                // Scene 级全局组件：该 Gate 下所有 Handler 通过 GetComponent 共享
                scene.AddComponent<SessionService>();
                Log.Info($"[Gate] scene started. sceneId={scene.SceneConfigId} runtimeId={scene.RuntimeId}, SessionService attached");
                break;
            case SceneType.Http:
                Log.Info($"[Http] server started. sceneId={scene.SceneConfigId}");
                break;
            case SceneType.Avatars:
                scene.AddComponent<AvatarsService>();
                Log.Info($"[Avatars] scene started. sceneId={scene.SceneConfigId}");
                break;
        }

        await FTask.CompletedTask;
    }
}
