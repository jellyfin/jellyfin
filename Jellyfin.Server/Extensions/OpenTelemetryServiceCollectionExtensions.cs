using System;
using System.Collections.Generic;
using Jellyfin.Server.Telemetry;
using MediaBrowser.Common;
using MediaBrowser.Common.Telemetry;
using MediaBrowser.Controller.Telemetry;
using MediaBrowser.Model.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Jellyfin.Server.Extensions;

/// <summary>
/// Extension methods for wiring up OpenTelemetry tracing, metrics and logging.
/// </summary>
public static class OpenTelemetryServiceCollectionExtensions
{
    private const string DefaultServiceName = "jellyfin";

    /// <summary>
    /// Registers OpenTelemetry pipelines based on the provided <see cref="OpenTelemetryOptions"/>.
    /// When <see cref="OpenTelemetryOptions.Enabled"/> is <c>false</c> this method is a no-op.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="options">Server-side OpenTelemetry options.</param>
    /// <param name="applicationHost">The application host, used for the reported resource attributes.</param>
    /// <param name="logger">The logger.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection AddJellyfinOpenTelemetry(
        this IServiceCollection services,
        OpenTelemetryOptions options,
        IApplicationHost applicationHost,
        ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(applicationHost);
        ArgumentNullException.ThrowIfNull(logger);

        if (!options.Enabled)
        {
            return services;
        }

        if (!options.EnableTraces && !options.EnableMetrics && !options.EnableLogs)
        {
            logger.LogWarning("OpenTelemetry is enabled but traces, metrics and logs are all disabled, skipping setup");
            return services;
        }

        var serviceName = string.IsNullOrWhiteSpace(options.ServiceName) ? DefaultServiceName : options.ServiceName;

        var otel = services.AddOpenTelemetry()
            .ConfigureResource(r => r
                .AddService(
                    serviceName: serviceName,
                    serviceVersion: applicationHost.ApplicationVersionString,
                    serviceInstanceId: applicationHost.SystemId));

        if (options.EnableTraces)
        {
            otel.WithTracing(tracing =>
            {
                tracing.AddSource(JellyfinTelemetry.SourceNameWildcard);
                foreach (var source in options.AdditionalSources)
                {
                    tracing.AddSource(source);
                }

                if (options.TracingSampleRatio < 1.0)
                {
                    // Parent based so that a sampled incoming request keeps its whole trace.
                    tracing.SetSampler(new ParentBasedSampler(
                        new TraceIdRatioBasedSampler(Math.Clamp(options.TracingSampleRatio, 0.0, 1.0))));
                }

                if (options.InstrumentAspNetCore)
                {
                    tracing.AddAspNetCoreInstrumentation(o =>
                    {
                        var excluded = options.ExcludedPathPatterns;
                        if (excluded.Length > 0)
                        {
                            o.Filter = context => !IsExcluded(context.Request.Path, excluded);
                        }
                    });
                }

                if (options.InstrumentHttpClient)
                {
                    tracing.AddHttpClientInstrumentation();
                }

                if (options.InstrumentEntityFrameworkCore)
                {
                    tracing.AddEntityFrameworkCoreInstrumentation();
                }

                tracing.AddOtlpExporter(o => ConfigureOtlp(o, options, OtlpEndpointBuilder.TracesSignalPath, logger));
            });
        }

        if (options.EnableMetrics)
        {
            otel.WithMetrics(metrics =>
            {
                metrics.AddMeter(JellyfinTelemetry.SourceNameWildcard);
                foreach (var meter in options.AdditionalMeters)
                {
                    metrics.AddMeter(meter);
                }

                if (options.InstrumentAspNetCore)
                {
                    metrics.AddAspNetCoreInstrumentation();
                }

                if (options.InstrumentHttpClient)
                {
                    metrics.AddHttpClientInstrumentation();
                }

                if (options.InstrumentRuntime)
                {
                    metrics.AddRuntimeInstrumentation();
                }

                metrics.AddView(
                    PlaybackMetrics.PlaybackDurationName,
                    new ExplicitBucketHistogramConfiguration
                    {
                        Boundaries = [10, 30, 60, 120, 300, 600, 1200, 1800, 2700, 3600, 5400, 7200, 10800]
                    });

                metrics.AddOtlpExporter((exporter, reader) =>
                {
                    ConfigureOtlp(exporter, options, OtlpEndpointBuilder.MetricsSignalPath, logger);

                    if (options.MetricExportIntervalMilliseconds is int interval and > 0)
                    {
                        reader.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = interval;
                    }

                    switch (options.MetricTemporality)
                    {
                        case OpenTelemetryMetricTemporality.Cumulative:
                            reader.TemporalityPreference = MetricReaderTemporalityPreference.Cumulative;
                            break;
                        case OpenTelemetryMetricTemporality.Delta:
                            reader.TemporalityPreference = MetricReaderTemporalityPreference.Delta;
                            break;
                        default:
                            break;
                    }
                });
            });
        }

        if (options.EnableLogs)
        {
            // Shares the resource configured above instead of building a second, drifting one.
            otel.WithLogging(
                logging => logging.AddOtlpExporter(o => ConfigureOtlp(o, options, OtlpEndpointBuilder.LogsSignalPath, logger)),
                loggerOptions =>
                {
                    loggerOptions.IncludeFormattedMessage = true;
                    loggerOptions.IncludeScopes = true;
                });
        }

        // Export failures are only reported on the OpenTelemetry event sources, bridge them into the log.
        services.AddHostedService<OpenTelemetryEventListener>();

        logger.LogInformation(
            "OpenTelemetry enabled for {Signals} as service {ServiceName}, exporting to {Endpoint} over {Protocol}",
            DescribeSignals(options),
            serviceName,
            string.IsNullOrWhiteSpace(options.OtlpEndpoint) ? "the SDK default endpoint" : options.OtlpEndpoint,
            OtlpEndpointBuilder.GetEffectiveProtocol(options));

        return services;
    }

    private static string DescribeSignals(OpenTelemetryOptions options)
    {
        var signals = new List<string>(3);
        if (options.EnableTraces)
        {
            signals.Add("traces");
        }

        if (options.EnableMetrics)
        {
            signals.Add("metrics");
        }

        if (options.EnableLogs)
        {
            signals.Add("logs");
        }

        return string.Join(", ", signals);
    }

    private static bool IsExcluded(PathString path, string[] excludedPatterns)
    {
        var value = path.Value;
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        foreach (var pattern in excludedPatterns)
        {
            if (!string.IsNullOrEmpty(pattern) && value.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static void ConfigureOtlp(OtlpExporterOptions exporter, OpenTelemetryOptions options, string signalPath, ILogger logger)
    {
        // Only assign what was actually configured: every setter here takes precedence over the
        // standard OTEL_EXPORTER_OTLP_* environment variables.
        if (OtlpEndpointBuilder.TryGetProtocol(options.OtlpProtocol, out var protocol))
        {
            exporter.Protocol = protocol;
        }

        var endpoint = OtlpEndpointBuilder.Build(options, signalPath, logger);
        if (endpoint is not null)
        {
            exporter.Endpoint = endpoint;
        }

        if (!string.IsNullOrWhiteSpace(options.OtlpHeaders))
        {
            exporter.Headers = options.OtlpHeaders;
        }

        if (options.OtlpTimeoutMilliseconds is int timeout and > 0)
        {
            exporter.TimeoutMilliseconds = timeout;
        }

        if (options.OtlpCompression is bool compression)
        {
            exporter.Compression = compression ? OtlpExportCompression.GZip : OtlpExportCompression.None;
        }
    }
}
