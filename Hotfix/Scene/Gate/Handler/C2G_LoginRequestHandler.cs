using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;

namespace Fantasy;

public sealed class C2G_LoginRequestHandler : Message<C2G_LoginRequest>
{
    protected override async FTask Run(Session session, C2G_LoginRequest request)
    {
        Log.Info($"[Gate] Login request account={request.Account}");

        var response = G2C_LoginResponse.Create();
        response.ErrorCode = 0;
        response.Token = $"token-{request.Account}-{Random.Shared.NextInt64()}";

        session.Send(response);
        await FTask.CompletedTask;
    }
}
