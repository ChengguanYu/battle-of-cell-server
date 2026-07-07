using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;
using Fantasy.Protocol;

namespace Fantasy;

public sealed class LoginRequestHandler : Message<LoginRequest>
{
    protected override async FTask Run(Session session, LoginRequest request)
    {
        Log.Info($"[Gate] Login request account={request.Account}");

        var response = new LoginResponse();
        response.ErrorCode = 0;
        response.Token = $"token-{request.Account}-{Random.Shared.NextInt64()}";

        session.Send(response);
        await FTask.CompletedTask;
    }
}
