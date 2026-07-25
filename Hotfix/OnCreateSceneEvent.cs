using Fantasy;
using Fantasy.Async;
using Fantasy.Event;
using Entity.Managers;
using Hotfix.Database;
using Hotfix.Scene.Gate.Service;
using Hotfix.Scene.Avatars.Service;
using Hotfix.Scene.Match.Service;
using Hotfix.Scene.Rooms.Service;

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
                // 绑定 Gate Scene 作为 WsSession TimedOut 宽限期计时宿主
                SessionManager.Instance.SetTimerScene(scene);
                Log.Info($"[Gate] scene started. sceneId={scene.SceneConfigId} runtimeId={scene.RuntimeId}, SessionService attached");
                break;
            case SceneType.Http:
                Log.Info($"[Http] server started. sceneId={scene.SceneConfigId}");
                break;
            case SceneType.Avatars:
                scene.AddComponent<AvatarsService>();
                scene.AddComponent<Relay>();
                Log.Info($"[Avatars] scene started. sceneId={scene.SceneConfigId}");
                break;
            case SceneType.Match:
            {
                // Match 独立持有 Redis 连接，并直接绑定到服务层。
                var redis = AttachRedis(scene);
                var matchService = scene.AddComponent<MatchService>();
                matchService.BindRedis(redis);
                Log.Info($"[Match] scene started. sceneId={scene.SceneConfigId}");
                break;
            }
            case SceneType.Rooms:
            {
                // Rooms 独立持有 Redis 连接，并直接绑定到服务层。
                var redis = AttachRedis(scene);
                var roomsService = scene.AddComponent<RoomsService>();
                roomsService.BindRedis(redis);
                // 绑定 Rooms Scene 作为各房间私有 tick / hold 的定时器宿主
                RoomManager.Instance.SetTimerScene(scene);
                RoomManager.Instance.SetEmptyRoomHandler(roomsService.OnRoomEmpty);
                RoomManager.Instance.SetHoldTimeoutHandler(roomsService.OnHoldTimeout);
                Log.Info($"[Rooms] scene started. sceneId={scene.SceneConfigId} runtimeId={scene.RuntimeId}, RoomsService attached, RoomTimer/EmptyRoom/HoldTimeout bound");
                break;
            }
        }

        await FTask.CompletedTask;
    }

    private static RedisComponent AttachRedis(Fantasy.Scene scene)
    {
        var redis = scene.AddComponent<RedisComponent>();
        redis.Connect(abortOnFail: true);
        return redis;
    }
}
