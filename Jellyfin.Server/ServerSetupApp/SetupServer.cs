using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.Configuration;
using Emby.Server.Implementations.Serialization;
using Jellyfin.Networking.Manager;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Model.System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Jellyfin.Server.ServerSetupApp;

/// <summary>
/// Creates a fake application pipeline that will only exist for as long as the main app is not started.
/// </summary>
public sealed class SetupServer : IDisposable
{
    private readonly Func<INetworkManager?> _networkManagerFactory;
    private readonly IApplicationPaths _applicationPaths;
    private readonly Func<IServerApplicationHost?> _serverFactory;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IConfiguration _startupConfiguration;
    private readonly ServerConfigurationManager _configurationManager;
    private IHost? _startupServer;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="SetupServer"/> class.
    /// </summary>
    /// <param name="networkManagerFactory">The networkmanager.</param>
    /// <param name="applicationPaths">The application paths.</param>
    /// <param name="serverApplicationHostFactory">The servers application host.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    /// <param name="startupConfiguration">The startup configuration.</param>
    public SetupServer(
        Func<INetworkManager?> networkManagerFactory,
        IApplicationPaths applicationPaths,
        Func<IServerApplicationHost?> serverApplicationHostFactory,
        ILoggerFactory loggerFactory,
        IConfiguration startupConfiguration)
    {
        _networkManagerFactory = networkManagerFactory;
        _applicationPaths = applicationPaths;
        _serverFactory = serverApplicationHostFactory;
        _loggerFactory = loggerFactory;
        _startupConfiguration = startupConfiguration;
        var xmlSerializer = new MyXmlSerializer();
        _configurationManager = new ServerConfigurationManager(_applicationPaths, loggerFactory, xmlSerializer);
        _configurationManager.RegisterConfiguration<NetworkConfigurationFactory>();
    }

    /// <summary>
    /// Starts the Bind-All Setup aspcore server to provide a reflection on the current core setup.
    /// </summary>
    /// <returns>A Task.</returns>
    public async Task RunAsync()
    {
        ThrowIfDisposed();
        _startupServer = Host.CreateDefaultBuilder()
            .UseConsoleLifetime()
            .ConfigureServices(serv =>
            {
                serv.AddHealthChecks()
                    .AddCheck<SetupHealthcheck>("StartupCheck");
            })
            .ConfigureWebHostDefaults(webHostBuilder =>
                    {
                        webHostBuilder
                                .UseKestrel((builderContext, options) =>
                                {
                                    var config = _configurationManager.GetNetworkConfiguration()!;
                                    var knownBindInterfaces = NetworkManager.GetInterfacesCore(_loggerFactory.CreateLogger<SetupServer>(), config.EnableIPv4, config.EnableIPv6);
                                    knownBindInterfaces = NetworkManager.FilterBindSettings(config, knownBindInterfaces.ToList(), config.EnableIPv4, config.EnableIPv6);
                                    var bindInterfaces = NetworkManager.GetAllBindInterfaces(false, _configurationManager, knownBindInterfaces, config.EnableIPv4, config.EnableIPv6);
                                    Extensions.WebHostBuilderExtensions.SetupJellyfinWebServer(
                                        bindInterfaces,
                                        config.InternalHttpPort,
                                        null,
                                        null,
                                        _startupConfiguration,
                                        _applicationPaths,
                                        _loggerFactory.CreateLogger<SetupServer>(),
                                        builderContext,
                                        options);
                                })
                                .Configure(app =>
                                {
                                    app.UseHealthChecks("/health");

                                    app.Map("/startup/logger", loggerRoute =>
                                    {
                                        loggerRoute.Run(async context =>
                                        {
                                            var networkManager = _networkManagerFactory();
                                            if (context.Connection.RemoteIpAddress is null || networkManager is null || !networkManager.IsInLocalNetwork(context.Connection.RemoteIpAddress))
                                            {
                                                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                                                return;
                                            }

                                            var logFilePath = new DirectoryInfo(_applicationPaths.LogDirectoryPath)
                                                .EnumerateFiles()
                                                .OrderByDescending(f => f.CreationTimeUtc)
                                                .FirstOrDefault()
                                                ?.FullName;
                                            if (logFilePath is not null)
                                            {
                                                await context.Response.SendFileAsync(logFilePath, CancellationToken.None).ConfigureAwait(false);
                                            }
                                        });
                                    });

                                    app.Map("/System/Info/Public", systemRoute =>
                                    {
                                        systemRoute.Run(async context =>
                                        {
                                            var jfApplicationHost = _serverFactory();

                                            var retryCounter = 0;
                                            while (jfApplicationHost is null && retryCounter < 5)
                                            {
                                                await Task.Delay(500).ConfigureAwait(false);
                                                jfApplicationHost = _serverFactory();
                                                retryCounter++;
                                            }

                                            if (jfApplicationHost is null)
                                            {
                                                context.Response.StatusCode = (int)HttpStatusCode.ServiceUnavailable;
                                                context.Response.Headers.RetryAfter = new StringValues("5");
                                                return;
                                            }

                                            var sysInfo = new PublicSystemInfo
                                            {
                                                Version = jfApplicationHost.ApplicationVersionString,
                                                ProductName = jfApplicationHost.Name,
                                                Id = jfApplicationHost.SystemId,
                                                ServerName = jfApplicationHost.FriendlyName,
                                                LocalAddress = jfApplicationHost.GetSmartApiUrl(context.Request),
                                                StartupWizardCompleted = false
                                            };

                                            await context.Response.WriteAsJsonAsync(sysInfo).ConfigureAwait(false);
                                        });
                                    });

                                    app.Run((context) =>
                                    {
                                        context.Response.StatusCode = (int)HttpStatusCode.ServiceUnavailable;
                                        context.Response.Headers.RetryAfter = new StringValues("5");
                                        context.Response.Headers.ContentType = new StringValues("text/html");
                                        context.Response.WriteAsync("<p>Jellyfin Server still starting. Please wait.</p>");
                                        var networkManager = _networkManagerFactory();
                                        if (networkManager is not null && context.Connection.RemoteIpAddress is not null && networkManager.IsInLocalNetwork(context.Connection.RemoteIpAddress))
                                        {
                                            context.Response.WriteAsync("<p>You can download the current logfiles <a href='/startup/logger'>here</a>.</p>");
                                        }

                                        return Task.CompletedTask;
                                    });
                                });
                    })
                    .Build();
        await _startupServer.StartAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Stops the Setup server.
    /// </summary>
    /// <returns>A task. Duh.</returns>
    public async Task StopAsync()
    {
        ThrowIfDisposed();
        if (_startupServer is null)
        {
            throw new InvalidOperationException("Tried to stop a non existing startup server");
        }

        await _startupServer.StopAsync().ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _startupServer?.Dispose();
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    private class SetupHealthcheck : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(HealthCheckResult.Degraded("Server is still starting up."));
        }
    }
}
