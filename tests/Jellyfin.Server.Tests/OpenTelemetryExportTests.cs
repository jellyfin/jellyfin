using System;
using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Jellyfin.Server.Extensions;
using MediaBrowser.Common;
using MediaBrowser.Common.Telemetry;
using MediaBrowser.Model.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Xunit;

namespace Jellyfin.Server.Tests;

public static class OpenTelemetryExportTests
{
    /// <summary>
    /// Over http/protobuf an endpoint set in code is used verbatim by the SDK, so all three signals would
    /// otherwise be posted to the same URL.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public static async Task HttpProtobuf_ExportsEverySignalToItsOwnPath()
    {
        var paths = new ConcurrentBag<string>();
        var port = GetFreePort();

        using var listener = new HttpListener();
        listener.Prefixes.Add(string.Create(CultureInfo.InvariantCulture, $"http://127.0.0.1:{port}/"));
        listener.Start();
        var listening = Task.Run(
            async () =>
            {
                while (listener.IsListening)
                {
                    HttpListenerContext context;
                    try
                    {
                        context = await listener.GetContextAsync().ConfigureAwait(false);
                    }
                    catch (Exception)
                    {
                        return;
                    }

                    paths.Add(context.Request.Url!.AbsolutePath);
                    context.Response.StatusCode = (int)HttpStatusCode.OK;
                    context.Response.Close();
                }
            },
            TestContext.Current.CancellationToken);

        var options = new OpenTelemetryOptions
        {
            Enabled = true,
            EnableLogs = true,
            OtlpProtocol = OpenTelemetryOtlpProtocol.HttpProtobuf,
            OtlpEndpoint = string.Create(CultureInfo.InvariantCulture, $"http://127.0.0.1:{port}"),
            InstrumentAspNetCore = false,
            InstrumentHttpClient = false,
            InstrumentRuntime = false
        };

        var services = new ServiceCollection();
        services.AddJellyfinOpenTelemetry(options, CreateApplicationHost(), NullLogger.Instance);

        await using var provider = services.BuildServiceProvider();

        // Resolving the providers builds the pipelines and starts listening to the Jellyfin sources.
        var tracerProvider = provider.GetRequiredService<TracerProvider>();
        var meterProvider = provider.GetRequiredService<MeterProvider>();
        var loggerProvider = provider.GetRequiredService<LoggerProvider>();

        using (var activity = JellyfinTelemetry.ActivitySource.StartActivity("test"))
        {
            Assert.NotNull(activity);
        }

        JellyfinTelemetry.Meter.CreateCounter<long>("test.counter").Add(1);

        using (var loggerFactory = new LoggerFactory(provider.GetServices<ILoggerProvider>()))
        {
            loggerFactory.CreateLogger("test").LogInformation("test message");
        }

        tracerProvider.ForceFlush(5000);
        meterProvider.ForceFlush(5000);
        loggerProvider.ForceFlush(5000);

        listener.Stop();
        await listening;

        Assert.Contains("/v1/traces", paths);
        Assert.Contains("/v1/metrics", paths);
        Assert.Contains("/v1/logs", paths);
    }

    /// <summary>
    /// The instruments live in static fields, so they do not exist until something touches their
    /// class. A gauge that should read zero on an idle server then reports nothing at all.
    /// </summary>
    [Fact]
    public static void AddJellyfinOpenTelemetry_PublishesInstrumentsWithoutAnyActivity()
    {
        var options = new OpenTelemetryOptions
        {
            Enabled = true,
            EnableMetrics = true,
            InstrumentAspNetCore = false,
            InstrumentHttpClient = false,
            InstrumentRuntime = false
        };

        var services = new ServiceCollection();
        services.AddJellyfinOpenTelemetry(options, CreateApplicationHost(), NullLogger.Instance);

        var published = new ConcurrentBag<string>();
        using var listener = new MeterListener
        {
            InstrumentPublished = (instrument, _) =>
            {
                if (string.Equals(instrument.Meter.Name, JellyfinTelemetry.SourceName, StringComparison.Ordinal))
                {
                    published.Add(instrument.Name);
                }
            }
        };

        // Start replays every instrument that already exists.
        listener.Start();

        Assert.Contains("jellyfin.metadata.refresh.active", published);
        Assert.Contains("jellyfin.sessions.active", published);
        Assert.Contains("jellyfin.playback.sessions.active", published);
        Assert.Contains("jellyfin.transcode.active", published);
        Assert.Contains("jellyfin.authentication.attempts", published);
        Assert.Contains("jellyfin.library.changes", published);
        Assert.Contains("jellyfin.task.executions", published);
        Assert.Contains("jellyfin.subtitle.downloads", published);
    }

    private static int GetFreePort()
    {
        using var socket = new TcpListener(IPAddress.Loopback, 0);
        socket.Start();
        var port = ((IPEndPoint)socket.LocalEndpoint).Port;
        socket.Stop();
        return port;
    }

    private static IApplicationHost CreateApplicationHost()
    {
        var applicationHost = new Mock<IApplicationHost>();
        applicationHost.SetupGet(host => host.ApplicationVersionString).Returns("10.0.0");
        applicationHost.SetupGet(host => host.SystemId).Returns("system-id");
        return applicationHost.Object;
    }
}
