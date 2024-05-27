using System;
using System.IO;
using System.Net;
using Jellyfin.Server.Helpers;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Extensions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Extensions;

/// <summary>
/// Extensions for configuring the web host builder.
/// </summary>
public static class WebHostBuilderExtensions
{
    /// <summary>
    /// Configure the web host builder.
    /// </summary>
    /// <param name="builder">The builder to configure.</param>
    /// <param name="appHost">The application host.</param>
    /// <param name="startupConfig">The application configuration.</param>
    /// <param name="appPaths">The application paths.</param>
    /// <param name="logger">The logger.</param>
    /// <returns>The configured web host builder.</returns>
    public static IWebHostBuilder ConfigureWebHostBuilder(
        this IWebHostBuilder builder,
        CoreAppHost appHost,
        IConfiguration startupConfig,
        IApplicationPaths appPaths,
        ILogger logger)
    {
        return builder
            .UseKestrel((builderContext, options) =>
            {
                var addresses = appHost.NetManager.GetAllBindInterfaces(false);

                bool flagged = false;
                foreach (var netAdd in addresses)
                {
                    var address = netAdd.Address;
                    logger.LogInformation("Kestrel is listening on {Address}", address.Equals(IPAddress.IPv6Any) ? "all interfaces" : address);
                    options.Listen(netAdd.Address, appHost.HttpPort);
                    if (appHost.ListenWithHttps)
                    {
                        options.Listen(
                            address,
                            appHost.HttpsPort,
                            listenOptions => listenOptions.UseHttps(appHost.Certificate));
                    }
                    else if (builderContext.HostingEnvironment.IsDevelopment())
                    {
                        try
                        {
                            options.Listen(
                                address,
                                appHost.HttpsPort,
                                listenOptions => listenOptions.UseHttps());
                        }
                        catch (InvalidOperationException)
                        {
                            if (!flagged)
                            {
                                logger.LogWarning("Failed to listen to HTTPS using the ASP.NET Core HTTPS development certificate. Please ensure it has been installed and set as trusted");
                                flagged = true;
                            }
                        }
                    }
                }

                // Bind to unix socket (only on unix systems)
                if (startupConfig.UseUnixSocket() && Environment.OSVersion.Platform == PlatformID.Unix)
                {
                    var socketPath = StartupHelpers.GetUnixSocketPath(startupConfig, appPaths);

                    // Workaround for https://github.com/aspnet/AspNetCore/issues/14134
                    if (File.Exists(socketPath))
                    {
                        File.Delete(socketPath);
                    }

                    options.ListenUnixSocket(socketPath);
                    logger.LogInformation("Kestrel listening to unix socket {SocketPath}", socketPath);
                }
            })
            .UseStartup(_ => new Startup(appHost));
    }
}
