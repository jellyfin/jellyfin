using System;
using System.Collections.Concurrent;
using Jellyfin.Server.Extensions;
using MediaBrowser.Common;
using MediaBrowser.Model.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Serilog;
using Serilog.Extensions.Logging;
using Xunit;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Jellyfin.Server.Tests;

public static class OpenTelemetryServiceCollectionExtensionsTests
{
    [Fact]
    public static void AddJellyfinOpenTelemetry_Disabled_RegistersNothing()
    {
        var services = new ServiceCollection();

        services.AddJellyfinOpenTelemetry(new OpenTelemetryOptions { Enabled = false }, CreateApplicationHost(), NullLogger.Instance);

        Assert.Empty(services);
    }

    [Fact]
    public static void AddJellyfinOpenTelemetry_AllSignalsDisabled_RegistersNothing()
    {
        var services = new ServiceCollection();
        var options = new OpenTelemetryOptions
        {
            Enabled = true,
            EnableTraces = false,
            EnableMetrics = false,
            EnableLogs = false
        };

        services.AddJellyfinOpenTelemetry(options, CreateApplicationHost(), NullLogger.Instance);

        Assert.Empty(services);
    }

    /// <summary>
    /// The exported logs only ever reach the SDK through an <see cref="ILoggerProvider"/> in the service
    /// collection, see <see cref="SerilogForwardsLogEventsToRegisteredProviders"/> for the other half.
    /// </summary>
    [Fact]
    public static void AddJellyfinOpenTelemetry_LogsEnabled_RegistersLoggerProvider()
    {
        var services = new ServiceCollection();
        var options = new OpenTelemetryOptions
        {
            Enabled = true,
            EnableTraces = false,
            EnableMetrics = false,
            EnableLogs = true
        };

        services.AddJellyfinOpenTelemetry(options, CreateApplicationHost(), NullLogger.Instance);

        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(ILoggerProvider));
    }

    /// <summary>
    /// Serilog owns the logger factory, so it has to be told to write to the providers other components register.
    /// </summary>
    [Fact]
    public static void SerilogForwardsLogEventsToRegisteredProviders()
    {
        var providers = new LoggerProviderCollection();
        using var recordingProvider = new RecordingLoggerProvider();
        using var serilogLogger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Providers(providers)
            .CreateLogger();

        using var host = Host.CreateDefaultBuilder()
            .ConfigureLogging(builder => builder.ClearProviders())
            .ConfigureServices(services => services.AddSingleton<ILoggerProvider>(recordingProvider))
            .UseSerilog(serilogLogger, dispose: false, providers: providers)
            .Build();

        host.Services.GetRequiredService<ILoggerFactory>()
            .CreateLogger("Test")
            .LogInformation("forwarded message");

        Assert.Contains("forwarded message", recordingProvider.Messages);
    }

    private static IApplicationHost CreateApplicationHost()
    {
        var applicationHost = new Mock<IApplicationHost>();
        applicationHost.SetupGet(host => host.ApplicationVersionString).Returns("10.0.0");
        applicationHost.SetupGet(host => host.SystemId).Returns("system-id");
        return applicationHost.Object;
    }

    private sealed class RecordingLoggerProvider : ILoggerProvider
    {
        public ConcurrentBag<string> Messages { get; } = new();

        public ILogger CreateLogger(string categoryName) => new RecordingLogger(Messages);

        public void Dispose()
        {
        }

        private sealed class RecordingLogger : ILogger
        {
            private readonly ConcurrentBag<string> _messages;

            public RecordingLogger(ConcurrentBag<string> messages)
            {
                _messages = messages;
            }

            public IDisposable? BeginScope<TState>(TState state)
                where TState : notnull
                => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                ArgumentNullException.ThrowIfNull(formatter);
                _messages.Add(formatter(state, exception));
            }
        }
    }
}
