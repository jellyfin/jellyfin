using System;
using System.Globalization;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

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
