using System;

namespace MediaBrowser.Model.Configuration;

/// <summary>
/// The OTLP transport protocol to use when exporting telemetry.
/// </summary>
public enum OpenTelemetryOtlpProtocol
{
    /// <summary>
    /// Let the SDK decide, honouring the standard OTEL_EXPORTER_OTLP_PROTOCOL environment variable.
    /// </summary>
    Default = 0,

    /// <summary>
    /// gRPC over HTTP/2. Default OTLP endpoint is http://localhost:4317.
    /// </summary>
    Grpc = 1,

    /// <summary>
    /// Protobuf payloads over HTTP/1.1. Default OTLP endpoint is http://localhost:4318.
    /// </summary>
    HttpProtobuf = 2
}

/// <summary>
/// The aggregation temporality requested from the metrics exporter.
/// </summary>
public enum OpenTelemetryMetricTemporality
{
    /// <summary>
    /// Let the SDK decide, honouring the standard OTEL_EXPORTER_OTLP_METRICS_TEMPORALITY_PREFERENCE environment variable.
    /// </summary>
    Default = 0,

    /// <summary>
    /// Report cumulative sums, the OTLP default. Expected by Prometheus style backends.
    /// </summary>
    Cumulative = 1,

    /// <summary>
    /// Report deltas between exports. Expected by several commercial backends.
    /// </summary>
    Delta = 2
}

/// <summary>
/// Settings controlling the OpenTelemetry pipeline. Disabled by default.
/// </summary>
public class OpenTelemetryOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether OpenTelemetry instrumentation and export are enabled.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether traces are collected and exported.
    /// </summary>
    public bool EnableTraces { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether metrics are collected and exported.
    /// </summary>
    public bool EnableMetrics { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether logs are exported via OpenTelemetry.
    /// </summary>
    public bool EnableLogs { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether ASP.NET Core requests are instrumented.
    /// </summary>
    public bool InstrumentAspNetCore { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether outgoing HTTP client calls are instrumented.
    /// </summary>
    public bool InstrumentHttpClient { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether Entity Framework Core database commands are instrumented.
    /// </summary>
    public bool InstrumentEntityFrameworkCore { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether .NET runtime metrics (GC, thread pool, exceptions, etc.) are collected.
    /// </summary>
    public bool InstrumentRuntime { get; set; } = true;

    /// <summary>
    /// Gets or sets the service name reported in telemetry. Defaults to "jellyfin" when null or empty.
    /// </summary>
    public string? ServiceName { get; set; }

    /// <summary>
    /// Gets or sets the OTLP endpoint base URL. When null or empty the SDK default for the configured protocol is used.
    /// </summary>
    public string? OtlpEndpoint { get; set; }

    /// <summary>
    /// Gets or sets the OTLP protocol.
    /// </summary>
    public OpenTelemetryOtlpProtocol OtlpProtocol { get; set; } = OpenTelemetryOtlpProtocol.Default;

    /// <summary>
    /// Gets or sets optional OTLP headers in the form "key1=value1,key2=value2". Useful for vendor authentication tokens.
    /// </summary>
    public string? OtlpHeaders { get; set; }

    /// <summary>
    /// Gets or sets the OTLP export timeout in milliseconds. When null the SDK default is used.
    /// </summary>
    public int? OtlpTimeoutMilliseconds { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the OTLP payload is gzip compressed. When null the SDK default is used.
    /// </summary>
    public bool? OtlpCompression { get; set; }

    /// <summary>
    /// Gets or sets the metric export interval in milliseconds. When null the SDK default (60s) is used.
    /// </summary>
    public int? MetricExportIntervalMilliseconds { get; set; }

    /// <summary>
    /// Gets or sets the aggregation temporality requested from the metrics exporter.
    /// </summary>
    public OpenTelemetryMetricTemporality MetricTemporality { get; set; } = OpenTelemetryMetricTemporality.Default;

    /// <summary>
    /// Gets or sets the ratio of traces that are sampled, between 0.0 and 1.0.
    /// </summary>
    public double TracingSampleRatio { get; set; } = 1.0;

    /// <summary>
    /// Gets or sets the request path fragments that are never traced, matched case insensitively.
    /// </summary>
    public string[] ExcludedPathPatterns { get; set; } =
    [
        "/health",
        "/metrics",
        "/web/",
        "/videos/",
        "/audio/",
        "/images",
        "/socket",
        "/livestreamfiles/",
        "/sessions/playing/progress"
    ];

    /// <summary>
    /// Gets or sets additional ActivitySource names to listen to, for example those emitted by plugins.
    /// </summary>
    public string[] AdditionalSources { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets additional Meter names to listen to, for example those emitted by plugins.
    /// </summary>
    public string[] AdditionalMeters { get; set; } = Array.Empty<string>();
}
