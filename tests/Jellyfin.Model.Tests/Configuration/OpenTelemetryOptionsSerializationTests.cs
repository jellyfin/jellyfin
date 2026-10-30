using System.IO;
using System.Xml;
using System.Xml.Serialization;
using MediaBrowser.Model.Configuration;
using Xunit;

namespace Jellyfin.Model.Tests.Configuration;

public static class OpenTelemetryOptionsSerializationTests
{
    [Fact]
    public static void OpenTelemetryOptionsRoundTrip()
    {
        var options = new OpenTelemetryOptions
        {
            Enabled = true,
            OtlpProtocol = OpenTelemetryOtlpProtocol.HttpProtobuf,
            OtlpTimeoutMilliseconds = 1234,
            OtlpCompression = true,
            TracingSampleRatio = 0.25,
            AdditionalSources = ["Plugin.Foo"]
        };

        var serializer = new XmlSerializer(typeof(OpenTelemetryOptions));
        using var stream = new MemoryStream();
        serializer.Serialize(stream, options);
        stream.Position = 0;
        var round = (OpenTelemetryOptions)Deserialize(serializer, stream)!;

        Assert.True(round.Enabled);
        Assert.Equal(OpenTelemetryOtlpProtocol.HttpProtobuf, round.OtlpProtocol);
        Assert.Equal(1234, round.OtlpTimeoutMilliseconds);
        Assert.True(round.OtlpCompression);
        Assert.Equal(0.25, round.TracingSampleRatio);
        Assert.Equal(["Plugin.Foo"], round.AdditionalSources);
        Assert.Equal(9, round.ExcludedPathPatterns.Length);
    }

    [Fact]
    public static void SparseConfigurationKeepsDefaults()
    {
        const string SparseXml = """
            <?xml version="1.0" encoding="utf-8"?>
            <OpenTelemetryOptions xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
              <Enabled>true</Enabled>
            </OpenTelemetryOptions>
            """;

        var serializer = new XmlSerializer(typeof(OpenTelemetryOptions));
        using var reader = new StringReader(SparseXml);
        var options = (OpenTelemetryOptions)Deserialize(serializer, reader)!;

        Assert.True(options.Enabled);
        Assert.True(options.EnableTraces);
        Assert.True(options.EnableMetrics);
        Assert.False(options.EnableLogs);
        Assert.Null(options.OtlpEndpoint);
        Assert.Equal(OpenTelemetryOtlpProtocol.Default, options.OtlpProtocol);
        Assert.Equal(1.0, options.TracingSampleRatio);
        Assert.Equal(9, options.ExcludedPathPatterns.Length);
    }

    private static object? Deserialize(XmlSerializer serializer, Stream stream)
    {
        using var reader = XmlReader.Create(stream, CreateReaderSettings());
        return serializer.Deserialize(reader);
    }

    private static object? Deserialize(XmlSerializer serializer, TextReader textReader)
    {
        using var reader = XmlReader.Create(textReader, CreateReaderSettings());
        return serializer.Deserialize(reader);
    }

    private static XmlReaderSettings CreateReaderSettings()
        => new() { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null };
}
