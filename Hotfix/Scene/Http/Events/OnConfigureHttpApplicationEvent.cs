using Fantasy.Async;
using Fantasy.Event;
using Fantasy.Network.HTTP;
using Microsoft.AspNetCore.Builder;

namespace Hotfix;

public sealed class OnConfigureHttpApplicationEvent : AsyncEventSystem<OnConfigureHttpApplication>
{
    protected override async FTask Handler(OnConfigureHttpApplication self)
    {
        self.Application.UseCors();

        await FTask.CompletedTask;
    }
}
