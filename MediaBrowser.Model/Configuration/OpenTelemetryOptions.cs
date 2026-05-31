namespace MediaBrowser.Model.Configuration;

/// <summary>
/// The OTLP transport protocol to use when exporting telemetry.
/// </summary>
public enum OpenTelemetryOtlpProtocol
{
    /// <summary>
    /// gRPC over HTTP/2. Default OTLP endpoint is http://localhost:4317.
    /// </summary>
    Grpc = 0,

    /// <summary>
    /// Protobuf payloads over HTTP/1.1. Default OTLP endpoint is http://localhost:4318.
    /// </summary>
    HttpProtobuf = 1
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
    /// Backed by a prerelease OpenTelemetry instrumentation package.
    /// </summary>
    public bool InstrumentEntityFrameworkCore { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether .NET runtime metrics (GC, thread pool, exceptions, etc.) are collected.
    /// </summary>
    public bool InstrumentRuntime { get; set; } = true;

    /// <summary>
    /// Gets or sets the service name reported in telemetry. Defaults to "jellyfin" when null or empty.
    /// </summary>
    public string? ServiceName { get; set; }

    /// <summary>
    /// Gets or sets the OTLP endpoint URL. When null or empty the SDK default for the configured protocol is used.
    /// </summary>
    public string? OtlpEndpoint { get; set; }

    /// <summary>
    /// Gets or sets the OTLP protocol. The default is gRPC.
    /// </summary>
    public OpenTelemetryOtlpProtocol OtlpProtocol { get; set; } = OpenTelemetryOtlpProtocol.Grpc;

    /// <summary>
    /// Gets or sets optional OTLP headers in the form "key1=value1,key2=value2". Useful for vendor authentication tokens.
    /// </summary>
    public string? OtlpHeaders { get; set; }
}
