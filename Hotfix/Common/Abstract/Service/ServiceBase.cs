using Fantasy.Async;
using Fantasy.Network.Interface;

namespace Hotfix.Common.Abstract.Service;

/// <summary>
/// 所有 Scene 级服务的抽象基类（挂在 Scene 上，全 Handler 共享同一实例）。
/// </summary>
public abstract class ServiceBase : Fantasy.Entitas.Entity
{
    /// <summary>
    /// 向指定 <paramref name="address"/> 的目标 Scene 发起跨 Scene RPC，返回强类型响应。
    /// </summary>
    /// <remarks>
    /// 仅封装“发起 RPC + 强转响应 + 归还请求池”三步，不处理任何错误：
    /// 地址缺失、RPC 异常、响应 <c>ErrorCode</c> 非法等一律透传，由上层决定降级策略。
    /// 请求对象在 <c>finally</c> 中归还消息池；响应对象同样池化，由调用方负责释放。
    /// </remarks>
    /// <param name="address">目标 Scene 的 Address（可经 <c>Scene.GetSceneAddress(...)</c> 获取）。</param>
    /// <param name="request">请求对象（来自消息池，方法内负责归还）。</param>
    /// <typeparam name="TReq">请求类型，须实现 <see cref="IAddressRequest"/>。</typeparam>
    /// <typeparam name="TResp">响应类型，须实现 <see cref="IAddressResponse"/>。</typeparam>
    /// <returns>强类型响应对象（池化，调用方用完需 <see cref="IDisposable.Dispose"/>）。</returns>
    protected async FTask<TResp> Call<TReq, TResp>(long address, TReq request)
        where TReq : IAddressRequest
        where TResp : IAddressResponse
    {
        try
        {
            return (TResp)await Scene.Call(address, request);
        }
        finally
        {
            request.Dispose();
        }
    }

    /// <summary>
    /// 向指定 <paramref name="address"/> 的目标 Scene 发送单向消息（无响应、无 <c>reply()</c>）。
    /// </summary>
    /// <remarks>
    /// 仅封装“发起 Send + 归还消息池”两步。<c>Scene.Send</c> 为同步返回：同进程路径在返回前同步
    /// 反序列化出新对象并入队异步派发，跨进程路径在返回前同步序列化进缓冲区并入队异步发送，
    /// 两条路径返回后均不再持有原 <paramref name="message"/>，故可在 <c>finally</c> 安全归还消息池。
    /// 与 <see cref="Call{TReq,TResp}"/> 不同，<c>Send</c> 不抛异常：目标地址为 0 时框架仅记录
    /// <c>Log.Error</c> 后静默返回，调用方无法感知投递失败，必要时需自行确认地址有效。
    /// </remarks>
    /// <param name="address">目标 Scene 的 Address（可经 <c>Scene.GetSceneAddress(...)</c> 获取）。</param>
    /// <param name="message">消息对象（来自消息池，方法内负责归还）。</param>
    /// <typeparam name="T">消息类型，须实现 <see cref="IAddressMessage"/>（单向消息基类，<see cref="IAddressRequest"/> 亦派生自此）。</typeparam>
    protected void Send<T>(long address, T message) where T : IAddressMessage
    {
        try
        {
            Scene.Send(address, message);
        }
        finally
        {
            message.Dispose();
        }
    }
}