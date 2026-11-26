using System;
using System.Globalization;
using MediaBrowser.Model.Configuration;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Exporter;

namespace Jellyfin.Server.Telemetry;

/// <summary>
/// Resolves the OTLP protocol and per signal endpoint from the server configuration.
/// </summary>
internal static class OtlpEndpointBuilder
{
    /// <summary>
    /// The path traces are posted to when exporting over http/protobuf.
    /// </summary>
    public const string TracesSignalPath = "v1/traces";

    /// <summary>
    /// The path metrics are posted to when exporting over http/protobuf.
    /// </summary>
    public const string MetricsSignalPath = "v1/metrics";

    /// <summary>
    /// The path logs are posted to when exporting over http/protobuf.
    /// </summary>
    public const string LogsSignalPath = "v1/logs";

    private const string ProtocolEnvironmentVariable = "OTEL_EXPORTER_OTLP_PROTOCOL";
    private const string HttpProtobufValue = "http/protobuf";

    private static readonly string[] _signalPaths = [TracesSignalPath, MetricsSignalPath, LogsSignalPath];

    /// <summary>
    /// Translates the configured protocol, if one was explicitly picked.
    /// </summary>
    /// <param name="configured">The configured protocol.</param>
    /// <param name="protocol">The translated protocol.</param>
    /// <returns><c>true</c> if a protocol was explicitly configured.</returns>
    public static bool TryGetProtocol(OpenTelemetryOtlpProtocol configured, out OtlpExportProtocol protocol)
    {
        switch (configured)
        {
            case OpenTelemetryOtlpProtocol.Grpc:
                protocol = OtlpExportProtocol.Grpc;
                return true;
            case OpenTelemetryOtlpProtocol.HttpProtobuf:
                protocol = OtlpExportProtocol.HttpProtobuf;
                return true;
            default:
                protocol = OtlpExportProtocol.Grpc;
                return false;
        }
    }

    /// <summary>
    /// Gets the protocol that will be used, falling back to the environment and then the SDK default.
    /// </summary>
    /// <param name="options">The OpenTelemetry options.</param>
    /// <returns>The effective protocol.</returns>
    public static OtlpExportProtocol GetEffectiveProtocol(OpenTelemetryOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (TryGetProtocol(options.OtlpProtocol, out var protocol))
        {
            return protocol;
        }

        var fromEnvironment = Environment.GetEnvironmentVariable(ProtocolEnvironmentVariable);
        return string.Equals(fromEnvironment, HttpProtobufValue, StringComparison.OrdinalIgnoreCase)
            ? OtlpExportProtocol.HttpProtobuf
            : OtlpExportProtocol.Grpc;
    }

    /// <summary>
    /// Builds the endpoint for a single signal, or <c>null</c> to leave the SDK default in place.
    /// </summary>
    /// <param name="options">The OpenTelemetry options.</param>
    /// <param name="signalPath">The signal specific path.</param>
    /// <param name="logger">The logger.</param>
    /// <returns>The endpoint to export this signal to.</returns>
    public static Uri? Build(OpenTelemetryOptions options, string signalPath, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        if (string.IsNullOrWhiteSpace(options.OtlpEndpoint))
        {
            return null;
        }

        if (!Uri.TryCreate(options.OtlpEndpoint, UriKind.Absolute, out var baseUri))
        {
            logger.LogWarning("Ignoring invalid OpenTelemetry OTLP endpoint {Endpoint}", options.OtlpEndpoint);
            return null;
        }

        if (GetEffectiveProtocol(options) != OtlpExportProtocol.HttpProtobuf)
        {
            return baseUri;
        }

        // Unlike an endpoint picked up from the environment, an endpoint set in code is used verbatim,
        // so the signal specific path has to be appended here or every signal would post to the same URL.
        var path = baseUri.AbsolutePath.TrimEnd('/');
        foreach (var knownSignalPath in _signalPaths)
        {
            if (path.EndsWith(knownSignalPath, StringComparison.OrdinalIgnoreCase))
            {
                logger.LogWarning(
                    "The configured OpenTelemetry OTLP endpoint {Endpoint} contains a signal specific path, configure the base URL instead",
                    options.OtlpEndpoint);
                path = path[..^knownSignalPath.Length].TrimEnd('/');
                break;
            }
        }

        return new UriBuilder(baseUri)
        {
            Path = string.Create(CultureInfo.InvariantCulture, $"{path}/{signalPath}")
        }.Uri;
    }
}
