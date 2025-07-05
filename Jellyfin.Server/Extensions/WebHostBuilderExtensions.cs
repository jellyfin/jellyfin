using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Jellyfin.Server.Helpers;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Extensions;
using MediaBrowser.Model.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
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
                SetupJellyfinWebServer(
                    appHost.NetManager.GetAllBindInterfaces(false),
                    appHost.HttpPort,
                    appHost.ListenWithHttps ? appHost.HttpsPort : null,
                    appHost.Certificate,
                    startupConfig,
                    appPaths,
                    logger,
                    builderContext,
                    options);
            })
            .UseStartup(context => new Startup(appHost, context.Configuration));
    }

    /// <summary>
    /// Configures a Kestrel type webServer to bind to the specific arguments.
    /// </summary>
    /// <param name="addresses">The IP addresses that should be listend to.</param>
    /// <param name="httpPort">The http port.</param>
    /// <param name="httpsPort">If set the https port. If set you must also set the certificate.</param>
    /// <param name="certificate">The certificate used for https port.</param>
    /// <param name="startupConfig">The startup config.</param>
    /// <param name="appPaths">The app paths.</param>
    /// <param name="logger">A logger.</param>
    /// <param name="builderContext">The kestrel build pipeline context.</param>
    /// <param name="options">The kestrel server options.</param>
    /// <exception cref="InvalidOperationException">Will be thrown when a https port is set but no or an invalid certificate is provided.</exception>
    public static void SetupJellyfinWebServer(
        IReadOnlyList<IPData> addresses,
        int httpPort,
        int? httpsPort,
        X509Certificate2? certificate,
        IConfiguration startupConfig,
        IApplicationPaths appPaths,
        ILogger logger,
        WebHostBuilderContext builderContext,
        KestrelServerOptions options)
    {
        bool flagged = false;
        foreach (var netAdd in addresses)
        {
            var address = netAdd.Address;
            logger.LogInformation("Kestrel is listening on {Address}", address.Equals(IPAddress.IPv6Any) ? "all interfaces" : address);
            options.Listen(netAdd.Address, httpPort);
            if (httpsPort.HasValue)
            {
                if (builderContext.HostingEnvironment.IsDevelopment())
                {
                    try
                    {
                        options.Listen(
                            address,
                            httpsPort.Value,
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
                else
                {
                    if (certificate is null)
                    {
                        throw new InvalidOperationException("Cannot run jellyfin with https without setting a valid certificate.");
                    }

                    options.Listen(
                        address,
                        httpsPort.Value,
                        listenOptions => listenOptions.UseHttps(certificate));
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
    }
}
