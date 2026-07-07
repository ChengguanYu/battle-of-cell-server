using Fantasy.Async;
using Fantasy.Event;
using Fantasy.Network.HTTP;
using Hotfix.Scene.Http.Configuration;

namespace Hotfix;

public sealed class OnConfigureHttpServicesEvent : AsyncEventSystem<OnConfigureHttpServices>
{
    protected override async FTask Handler(OnConfigureHttpServices self)
    {
        CorsConfiguration.ConfigureServices(self.Builder.Services);
        await FTask.CompletedTask;
    }
}
