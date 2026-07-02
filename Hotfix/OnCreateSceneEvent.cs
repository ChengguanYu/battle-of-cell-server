using Fantasy;
using Fantasy.Async;
using Fantasy.Event;

namespace Hotfix;

public sealed class OnCreateSceneEvent : AsyncEventSystem<OnCreateScene>
{
    protected override async FTask Handler(OnCreateScene self)
    {
        var scene = self.Scene;

        switch (scene.SceneType)
        {
            case SceneType.Gate:
                Log.Info($"[Gate] scene started. sceneId={scene.SceneConfigId} runtimeId={scene.RuntimeId}");
                break;
            case SceneType.Http:
                Log.Info($"[Http] server started. sceneId={scene.SceneConfigId}");
                break;
        }
        await FTask.CompletedTask;
    }
}
