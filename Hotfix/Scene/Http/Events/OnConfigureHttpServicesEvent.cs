using Fantasy.Async;
using Fantasy.Event;
using Fantasy.Network.HTTP;
using Microsoft.Extensions.DependencyInjection;

namespace Hotfix;

public sealed class OnConfigureHttpServicesEvent : AsyncEventSystem<OnConfigureHttpServices>
{
    protected override async FTask Handler(OnConfigureHttpServices self)
    {
        self.Builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.SetIsOriginAllowed(origin =>
                    {
                        var uri = new Uri(origin);
                        var host = uri.Host;
                        return host is "localhost" or "127.0.0.1";
                    })
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        await FTask.CompletedTask;
    }
}
