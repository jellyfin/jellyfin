using System;
using Jellyfin.Server.Telemetry;
using MediaBrowser.Model.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using OpenTelemetry.Exporter;
using Xunit;

namespace Jellyfin.Server.Tests;

public static class OtlpEndpointBuilderTests
{
    [Theory]
    [InlineData(OtlpEndpointBuilder.TracesSignalPath, "http://collector:4318/v1/traces")]
    [InlineData(OtlpEndpointBuilder.MetricsSignalPath, "http://collector:4318/v1/metrics")]
    [InlineData(OtlpEndpointBuilder.LogsSignalPath, "http://collector:4318/v1/logs")]
    public static void Build_HttpProtobuf_AppendsSignalPath(string signalPath, string expected)
    {
        var options = new OpenTelemetryOptions
        {
            OtlpProtocol = OpenTelemetryOtlpProtocol.HttpProtobuf,
            OtlpEndpoint = "http://collector:4318"
        };

        Assert.Equal(expected, OtlpEndpointBuilder.Build(options, signalPath, NullLogger.Instance)?.ToString());
    }

    [Fact]
    public static void Build_HttpProtobufWithBasePath_KeepsBasePath()
    {
        var options = new OpenTelemetryOptions
        {
            OtlpProtocol = OpenTelemetryOtlpProtocol.HttpProtobuf,
            OtlpEndpoint = "https://vendor.example.com/otlp/"
        };

        Assert.Equal(
            "https://vendor.example.com/otlp/v1/traces",
            OtlpEndpointBuilder.Build(options, OtlpEndpointBuilder.TracesSignalPath, NullLogger.Instance)?.ToString());
    }

    [Fact]
    public static void Build_HttpProtobufWithSignalPath_ReplacesSignalPath()
    {
        var options = new OpenTelemetryOptions
        {
            OtlpProtocol = OpenTelemetryOtlpProtocol.HttpProtobuf,
            OtlpEndpoint = "http://collector:4318/v1/traces"
        };

        Assert.Equal(
            "http://collector:4318/v1/metrics",
            OtlpEndpointBuilder.Build(options, OtlpEndpointBuilder.MetricsSignalPath, NullLogger.Instance)?.ToString());
    }

    [Fact]
    public static void Build_Grpc_UsesEndpointVerbatim()
    {
        var options = new OpenTelemetryOptions
        {
            OtlpProtocol = OpenTelemetryOtlpProtocol.Grpc,
            OtlpEndpoint = "http://collector:4317"
        };

        Assert.Equal(
            "http://collector:4317/",
            OtlpEndpointBuilder.Build(options, OtlpEndpointBuilder.TracesSignalPath, NullLogger.Instance)?.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not a url")]
    public static void Build_WithoutUsableEndpoint_ReturnsNull(string? endpoint)
    {
        var options = new OpenTelemetryOptions { OtlpEndpoint = endpoint };

        Assert.Null(OtlpEndpointBuilder.Build(options, OtlpEndpointBuilder.TracesSignalPath, NullLogger.Instance));
    }

    [Fact]
    public static void TryGetProtocol_Default_IsNotConfigured()
    {
        Assert.False(OtlpEndpointBuilder.TryGetProtocol(OpenTelemetryOtlpProtocol.Default, out _));
        Assert.True(OtlpEndpointBuilder.TryGetProtocol(OpenTelemetryOtlpProtocol.Grpc, out var grpc));
        Assert.Equal(OtlpExportProtocol.Grpc, grpc);
        Assert.True(OtlpEndpointBuilder.TryGetProtocol(OpenTelemetryOtlpProtocol.HttpProtobuf, out var http));
        Assert.Equal(OtlpExportProtocol.HttpProtobuf, http);
    }

    [Fact]
    public static void GetEffectiveProtocol_Default_HonoursEnvironment()
    {
        const string Variable = "OTEL_EXPORTER_OTLP_PROTOCOL";
        var previous = Environment.GetEnvironmentVariable(Variable);
        try
        {
            Environment.SetEnvironmentVariable(Variable, "http/protobuf");
            Assert.Equal(
                OtlpExportProtocol.HttpProtobuf,
                OtlpEndpointBuilder.GetEffectiveProtocol(new OpenTelemetryOptions()));

            // An explicit configuration value still wins over the environment.
            Assert.Equal(
                OtlpExportProtocol.Grpc,
                OtlpEndpointBuilder.GetEffectiveProtocol(new OpenTelemetryOptions { OtlpProtocol = OpenTelemetryOtlpProtocol.Grpc }));
        }
        finally
        {
            Environment.SetEnvironmentVariable(Variable, previous);
        }
    }
}
