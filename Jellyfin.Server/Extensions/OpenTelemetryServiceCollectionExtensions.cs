using System;
using System.Reflection;
using MediaBrowser.Model.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Jellyfin.Server.Extensions
{
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
        /// <returns>The service collection, for chaining.</returns>
        public static IServiceCollection AddJellyfinOpenTelemetry(this IServiceCollection services, OpenTelemetryOptions options)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(options);

            if (!options.Enabled)
            {
                return services;
            }

            var serviceName = string.IsNullOrWhiteSpace(options.ServiceName) ? DefaultServiceName : options.ServiceName;
            var serviceVersion = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "unknown";

            var otel = services.AddOpenTelemetry()
                .ConfigureResource(r => r.AddService(serviceName: serviceName, serviceVersion: serviceVersion));

            if (options.EnableTraces)
            {
                otel.WithTracing(tracing =>
                {
                    if (options.InstrumentAspNetCore)
                    {
                        tracing.AddAspNetCoreInstrumentation();
                    }

                    if (options.InstrumentHttpClient)
                    {
                        tracing.AddHttpClientInstrumentation();
                    }

                    if (options.InstrumentEntityFrameworkCore)
                    {
                        tracing.AddEntityFrameworkCoreInstrumentation();
                    }

                    tracing.AddOtlpExporter(o => ConfigureOtlp(o, options));
                });
            }

            if (options.EnableMetrics)
            {
                otel.WithMetrics(metrics =>
                {
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

                    metrics.AddOtlpExporter(o => ConfigureOtlp(o, options));
                });
            }

            if (options.EnableLogs)
            {
                services.AddLogging(b => b.AddOpenTelemetry(o =>
                {
                    o.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName: serviceName, serviceVersion: serviceVersion));
                    o.IncludeFormattedMessage = true;
                    o.IncludeScopes = true;
                    o.AddOtlpExporter(opt => ConfigureOtlp(opt, options));
                }));
            }

            return services;
        }

        private static void ConfigureOtlp(OtlpExporterOptions exporter, OpenTelemetryOptions options)
        {
            exporter.Protocol = options.OtlpProtocol == OpenTelemetryOtlpProtocol.HttpProtobuf
                ? OtlpExportProtocol.HttpProtobuf
                : OtlpExportProtocol.Grpc;

            if (!string.IsNullOrWhiteSpace(options.OtlpEndpoint)
                && Uri.TryCreate(options.OtlpEndpoint, UriKind.Absolute, out var endpoint))
            {
                exporter.Endpoint = endpoint;
            }

            if (!string.IsNullOrWhiteSpace(options.OtlpHeaders))
            {
                exporter.Headers = options.OtlpHeaders;
            }
        }
    }
}
