using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.Configuration;
using Emby.Server.Implementations.Serialization;
using Jellyfin.Networking.Manager;
using Jellyfin.Server.Extensions;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Model.IO;
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
using Morestachio;
using Morestachio.Framework.IO.SingleStream;
using Morestachio.Rendering;
using Serilog;
using ILogger = Microsoft.Extensions.Logging.ILogger;

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
    private IRenderer? _startupUiRenderer;
    private IHost? _startupServer;
    private bool _disposed;
    private bool _isUnhealthy;

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

    internal static ConcurrentQueue<StartupLogTopic>? LogQueue { get; set; } = new();

    /// <summary>
    /// Gets a value indicating whether Startup server is currently running.
    /// </summary>
    public bool IsAlive { get; internal set; }

    /// <summary>
    /// Starts the Bind-All Setup aspcore server to provide a reflection on the current core setup.
    /// </summary>
    /// <returns>A Task.</returns>
    public async Task RunAsync()
    {
        var fileTemplate = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "ServerSetupApp", "index.mstemplate.html")).ConfigureAwait(false);
        _startupUiRenderer = (await ParserOptionsBuilder.New()
            .WithTemplate(fileTemplate)
            .WithFormatter(
                (StartupLogTopic logEntry, IEnumerable<StartupLogTopic> children) =>
                {
                    if (children.Any())
                    {
                        var maxLevel = logEntry.LogLevel;
                        var stack = new Stack<StartupLogTopic>(children);

                        while (maxLevel != LogLevel.Error && stack.Count > 0 && (logEntry = stack.Pop()) is not null) // error is the highest inherted error level.
                        {
                            maxLevel = maxLevel < logEntry.LogLevel ? logEntry.LogLevel : maxLevel;
                            foreach (var child in logEntry.Children)
                            {
                                stack.Push(child);
                            }
                        }

                        return maxLevel;
                    }

                    return logEntry.LogLevel;
                },
                "FormatLogLevel")
            .WithFormatter(
                (LogLevel logLevel) =>
                {
                    switch (logLevel)
                    {
                        case LogLevel.Trace:
                        case LogLevel.Debug:
                        case LogLevel.None:
                            return "success";
                        case LogLevel.Information:
                            return "info";
                        case LogLevel.Warning:
                            return "warn";
                        case LogLevel.Error:
                            return "danger";
                        case LogLevel.Critical:
                            return "danger-strong";
                    }

                    return string.Empty;
                },
                "ToString")
            .BuildAndParseAsync()
            .ConfigureAwait(false))
            .CreateCompiledRenderer();

        ThrowIfDisposed();
        var retryAfterValue = TimeSpan.FromSeconds(5);
        var config = _configurationManager.GetNetworkConfiguration()!;
        _startupServer = Host.CreateDefaultBuilder(["hostBuilder:reloadConfigOnChange=false"])
            .UseConsoleLifetime()
            .UseSerilog()
            .ConfigureServices(serv =>
            {
                serv.AddSingleton(this);
                serv.AddHealthChecks()
                    .AddCheck<SetupHealthcheck>("StartupCheck");
                serv.Configure<ForwardedHeadersOptions>(options =>
                {
                    ApiServiceCollectionExtensions.ConfigureForwardHeaders(config, options);
                });
            })
            .ConfigureWebHostDefaults(webHostBuilder =>
                    {
                        webHostBuilder
                                .UseKestrel((builderContext, options) =>
                                {
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
                                    app.UseForwardedHeaders();
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
                                                context.Response.Headers.RetryAfter = new StringValues(retryAfterValue.TotalSeconds.ToString("000", CultureInfo.InvariantCulture));
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

                                    app.Run(async (context) =>
                                    {
                                        context.Response.StatusCode = (int)HttpStatusCode.ServiceUnavailable;
                                        context.Response.Headers.RetryAfter = new StringValues(retryAfterValue.TotalSeconds.ToString("000", CultureInfo.InvariantCulture));
                                        context.Response.Headers.ContentType = new StringValues("text/html");
                                        var networkManager = _networkManagerFactory();

                                        var startupLogEntries = LogQueue?.ToArray() ?? [];
                                        await _startupUiRenderer.RenderAsync(
                                            new Dictionary<string, object>()
                                            {
                                                { "isInReportingMode", _isUnhealthy },
                                                { "retryValue", retryAfterValue },
                                                { "version", typeof(Emby.Server.Implementations.ApplicationHost).Assembly.GetName().Version! },
                                                { "logs", startupLogEntries },
                                                { "networkManagerReady", networkManager is not null },
                                                { "localNetworkRequest", networkManager is not null && context.Connection.RemoteIpAddress is not null && networkManager.IsInLocalNetwork(context.Connection.RemoteIpAddress) }
                                            },
                                            new ByteCounterStream(context.Response.BodyWriter.AsStream(), IODefaults.FileStreamBufferSize, true, _startupUiRenderer.ParserOptions))
                                            .ConfigureAwait(false);
                                    });
                                });
                    })
                    .Build();
        await _startupServer.StartAsync().ConfigureAwait(false);
        IsAlive = true;
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
        IsAlive = false;
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
        IsAlive = false;
        LogQueue?.Clear();
        LogQueue = null;
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    internal void SoftStop()
    {
        _isUnhealthy = true;
    }

    private class SetupHealthcheck : IHealthCheck
    {
        private readonly SetupServer _startupServer;

        public SetupHealthcheck(SetupServer startupServer)
        {
            _startupServer = startupServer;
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            if (_startupServer._isUnhealthy)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy("Server is could not complete startup. Check logs."));
            }

            return Task.FromResult(HealthCheckResult.Degraded("Server is still starting up."));
        }
    }

    internal sealed class SetupLoggerFactory : ILoggerProvider, IDisposable
    {
        private bool _disposed;

        public ILogger CreateLogger(string categoryName)
        {
            return new CatchingSetupServerLogger();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
        }
    }

    internal sealed class CatchingSetupServerLogger : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel is LogLevel.Error or LogLevel.Critical;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            LogQueue?.Enqueue(new()
            {
                LogLevel = logLevel,
                Content = formatter(state, exception),
                DateOfCreation = DateTimeOffset.Now
            });
        }
    }
}
