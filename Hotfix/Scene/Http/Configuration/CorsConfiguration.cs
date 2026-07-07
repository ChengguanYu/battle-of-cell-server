using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Hotfix.Scene.Http.Configuration;

/// <summary>
/// CORS 跨域配置，允许 localhost 和 127.0.0.1 来源的 WebSocket 和 HTTP 请求。
/// </summary>
public static class CorsConfiguration
{
    public const string PolicyName = "AllowLocalDev";

    public static void ConfigureServices(IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy(PolicyName, policy =>
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
    }

    public static void ConfigureApplication(IApplicationBuilder app)
    {
        app.UseCors(PolicyName);
    }
}
