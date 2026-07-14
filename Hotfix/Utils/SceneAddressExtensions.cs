using Fantasy.Platform.Net;

namespace Hotfix.Utils;

/// <summary>
/// 跨 Scene 通信的地址获取封装。
/// Scene 本身也是 Entity，其 <c>Address</c>（= RuntimeId）可从
/// <see cref="SceneConfigData"/> 静态获得，是发起 <c>Scene.Call(address, req)</c>
/// 唯一已知的远程入口。
/// </summary>
public static class SceneAddressExtensions
{
    /// <summary>
    /// 按 <paramref name="sceneType"/> 获取一个目标 Scene 的 Address。
    /// 适用于全局只有一个目标 Scene、或调用方不关心选哪个实例的场景；
    /// 若存在多个实例，返回其中之一（当前为首个）。
    /// </summary>
    /// <param name="scene">发起调用的源 Scene（仅用于挂载扩展方法，不读取其状态）。</param>
    /// <param name="sceneType">目标 Scene 类型，如 <c>SceneType.Avatars</c>。</param>
    /// <returns>目标 Scene 的 Address。</returns>
    /// <exception cref="InvalidOperationException">不存在 sceneType 指定类型的 Scene（配置缺失）。</exception>
    public static long GetSceneAddress(this Fantasy.Scene scene, int sceneType)
    {
        var configs = SceneConfigData.Instance.GetSceneBySceneType(sceneType);
        if (configs.Count == 0)
        {
            throw new InvalidOperationException($"未找到 SceneType={sceneType} 的 Scene，请检查 Fantasy.config 是否配置该类型 Scene。");
        }
        return configs[0].Address;
    }

    /// <summary>
    /// 按 <paramref name="sceneType"/> 获取所有目标 Scene 的 Address 列表。
    /// 用于有多个同类 Scene 实例、需要由调用方自行选择（随机/负载均衡/按存档路由等）的场景。
    /// </summary>
    /// <param name="scene">发起调用的源 Scene（仅用于挂载扩展方法，不读取其状态）。</param>
    /// <param name="sceneType">目标 Scene 类型，如 <c>SceneType.Avatars</c>。</param>
    /// <returns>所有目标 Scene 的 Address 列表（至少含一个元素）。</returns>
    /// <exception cref="InvalidOperationException">不存在 sceneType 指定类型的 Scene（配置缺失）。</exception>
    public static List<long> GetSceneAddresses(this Fantasy.Scene scene, int sceneType)
    {
        var configs = SceneConfigData.Instance.GetSceneBySceneType(sceneType);
        if (configs.Count == 0)
        {
            throw new InvalidOperationException($"未找到 SceneType={sceneType} 的 Scene，请检查 Fantasy.config 是否配置该类型 Scene。");
        }
        return configs.Select(c => c.Address).ToList();
    }
}
