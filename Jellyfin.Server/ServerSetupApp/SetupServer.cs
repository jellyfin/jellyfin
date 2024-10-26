using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Networking.Manager;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Model.System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using SQLitePCL;

namespace Jellyfin.Server.ServerSetupApp;

/// <summary>
/// Creates a fake application pipeline that will only exist for as long as the main app is not started.
/// </summary>
public sealed class SetupServer : IDisposable
{
    private IHost? _startupServer;
    private bool _disposed;

    /// <summary>
    /// Starts the Bind-All Setup aspcore server to provide a reflection on the current core setup.
    /// </summary>
    /// <param name="networkManagerFactory">The networkmanager.</param>
    /// <param name="applicationPaths">The application paths.</param>
    /// <param name="serverApplicationHost">The servers application host.</param>
    /// <returns>A Task.</returns>
    public async Task RunAsync(
        Func<INetworkManager?> networkManagerFactory,
        IApplicationPaths applicationPaths,
        Func<IServerApplicationHost?> serverApplicationHost)
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
                                .UseKestrel()
                                .Configure(app =>
                                {
                                    app.UseHealthChecks("/health");

                                    app.Map("/startup/logger", loggerRoute =>
                                    {
                                        loggerRoute.Run(async context =>
                                        {
                                            var networkManager = networkManagerFactory();
                                            if (context.Connection.RemoteIpAddress is null || networkManager is null || !networkManager.IsInLocalNetwork(context.Connection.RemoteIpAddress))
                                            {
                                                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                                                return;
                                            }

                                            var logfilePath = Directory.EnumerateFiles(applicationPaths.LogDirectoryPath).Select(e => new FileInfo(e)).OrderBy(f => f.CreationTimeUtc).FirstOrDefault()?.FullName;
                                            if (logfilePath is not null)
                                            {
                                                await context.Response.SendFileAsync(logfilePath, CancellationToken.None).ConfigureAwait(false);
                                            }
                                        });
                                    });

                                    app.Map("/System/Info/Public", systemRoute =>
                                    {
                                        systemRoute.Run(async context =>
                                        {
                                            var jfApplicationHost = serverApplicationHost();

                                            var retryCounter = 0;
                                            while (jfApplicationHost is null && retryCounter < 5)
                                            {
                                                await Task.Delay(500).ConfigureAwait(false);
                                                jfApplicationHost = serverApplicationHost();
                                                retryCounter++;
                                            }

                                            if (jfApplicationHost is null)
                                            {
                                                context.Response.StatusCode = (int)HttpStatusCode.ServiceUnavailable;
                                                context.Response.Headers.RetryAfter = new Microsoft.Extensions.Primitives.StringValues("60");
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
                                        context.Response.Headers.RetryAfter = new Microsoft.Extensions.Primitives.StringValues("60");
                                        context.Response.WriteAsync("<p>Jellyfin Server still starting. Please wait.</p>");
                                        var networkManager = networkManagerFactory();
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
