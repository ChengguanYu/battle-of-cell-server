using Fantasy;
using Fantasy.Entitas.Interface;

namespace Hotfix.Database;

/// <summary>
/// Scene 销毁时释放该 Scene 的独立 Redis 连接。
/// </summary>
public sealed class RedisComponentDestroySystem : DestroySystem<RedisComponent>
{
    protected override void Destroy(RedisComponent self)
    {
        self.Close();
        Log.Info(
            $"[Redis] Scene 连接已释放 sceneType={self.Scene?.SceneType}, sceneId={self.Scene?.SceneConfigId}, runtimeId={self.Scene?.RuntimeId}");
    }
}
