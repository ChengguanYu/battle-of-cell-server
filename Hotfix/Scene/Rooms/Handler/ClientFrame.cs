using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;
using Hotfix.Scene.Rooms.Service;
using FScene = Fantasy.Scene;

namespace Hotfix.Scene.Rooms.Handler;

/// <summary>
/// Rooms 接收客户端帧转发并写入帧窗口。
/// </summary>
public sealed class RoomsClientFrameNotifyHandler : Address<FScene, RoomsClientFrameNotify>
{
    protected override async FTask Run(FScene scene, RoomsClientFrameNotify message)
    {
        var roomsService = scene.GetComponent<RoomsService>();
        // FIXME: 手工转移 frames 所有权，待统一收口
        // 原因：Fantasy Address.Handle finally 会对入站 message.Dispose()，生成代码会级联
        // foreach Dispose frames。此处 OnClientFrame 深拷贝入窗后仍需持有入站 ops 直到拷贝结束，
        // 故先摘引用，再由本 Handler 在拷贝完成后主动 Dispose（父消息 Dispose 时 frames 已是空 list）。
        // 后续：边界深拷贝 / 明确单所有者 API，去掉每跳手写交接与手工 Dispose。
        var frames = message.frames;
        message.frames = new List<frame>();
        await roomsService.OnClientFrame(message.userId, message.frame_number, frames);
        // OnClientFrame 深拷贝入窗后释放入站 ops（与上 FIXME 配套）
        if (frames is { Count: > 0 })
        {
            foreach (var f in frames)
            {
                f?.Dispose();
            }

            frames.Clear();
        }
    }
}
