using Fantasy.Async;
using Fantasy.Event;
using Fantasy.Network.HTTP;
using Hotfix.Scene.Http.Configuration;

namespace Hotfix;

public sealed class OnConfigureHttpApplicationEvent : AsyncEventSystem<OnConfigureHttpApplication>
{
    protected override async FTask Handler(OnConfigureHttpApplication self)
    {
        CorsConfiguration.ConfigureApplication(self.Application);
        await FTask.CompletedTask;
    }
}
