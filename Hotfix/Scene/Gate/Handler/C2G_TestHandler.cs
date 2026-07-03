using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;

namespace Fantasy;

public sealed class C2G_TestHandler : Message<C2G_TestMessage>
{
    protected override async FTask Run(Session session, C2G_TestMessage message)
    {
        Log.Info($"[Gate] received C2G_TestMessage from session={session.Id}");
        await FTask.CompletedTask;
    }
}
