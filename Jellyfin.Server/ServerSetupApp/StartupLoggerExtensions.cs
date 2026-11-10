using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Server.ServerSetupApp;

internal static class StartupLoggerExtensions
{
    public static IServiceCollection RegisterStartupLogger(this IServiceCollection services)
    {
        return services
            .AddTransient<IStartupLogger, StartupLogger<Startup>>()
            .AddTransient(typeof(IStartupLogger<>), typeof(StartupLogger<>));
    }
}
