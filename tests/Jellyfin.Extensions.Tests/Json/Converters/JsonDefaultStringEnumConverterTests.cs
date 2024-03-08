using System.Text.Json;
using Jellyfin.Data.Enums;
using Jellyfin.Extensions.Json.Converters;
using Xunit;

namespace Jellyfin.Extensions.Tests.Json.Converters;

public class JsonDefaultStringEnumConverterTests
{
    private readonly JsonSerializerOptions _jsonOptions = new() { Converters = { new JsonDefaultStringEnumConverterFactory() } };

    /// <summary>
    /// Test to ensure that `null` and empty string are deserialized to the default value.
    /// </summary>
    /// <param name="input">The input string.</param>
    /// <param name="output">The expected enum value.</param>
    [Theory]
    [InlineData("\"\"", MediaStreamProtocol.http)]
    [InlineData("\"Http\"", MediaStreamProtocol.http)]
    [InlineData("\"Hls\"", MediaStreamProtocol.hls)]
    public void Deserialize_Enum_Direct(string input, MediaStreamProtocol output)
    {
        var value = JsonSerializer.Deserialize<MediaStreamProtocol>(input, _jsonOptions);
        Assert.Equal(output, value);
    }

    /// <summary>
    /// Test to ensure that `null` and empty string are deserialized to the default value.
    /// </summary>
    /// <param name="input">The input string.</param>
    /// <param name="output">The expected enum value.</param>
    [Theory]
    [InlineData(null, MediaStreamProtocol.http)]
    [InlineData("\"\"", MediaStreamProtocol.http)]
    [InlineData("\"Http\"", MediaStreamProtocol.http)]
    [InlineData("\"Hls\"", MediaStreamProtocol.hls)]
    public void Deserialize_Enum(string? input, MediaStreamProtocol output)
    {
        input ??= "null";
        var json = $"{{ \"EnumValue\": {input} }}";
        var value = JsonSerializer.Deserialize<TestClass>(json, _jsonOptions);
        Assert.NotNull(value);
        Assert.Equal(output, value.EnumValue);
    }

    /// <summary>
    /// Test to ensure that empty string is deserialized to the default value,
    /// and `null` is deserialized to `null`.
    /// </summary>
    /// <param name="input">The input string.</param>
    /// <param name="output">The expected enum value.</param>
    [Theory]
    [InlineData(null, null)]
    [InlineData("\"\"", MediaStreamProtocol.http)]
    [InlineData("\"Http\"", MediaStreamProtocol.http)]
    [InlineData("\"Hls\"", MediaStreamProtocol.hls)]
    public void Deserialize_Enum_Nullable(string? input, MediaStreamProtocol? output)
    {
        input ??= "null";
        var json = $"{{ \"EnumValue\": {input} }}";
        var value = JsonSerializer.Deserialize<NullTestClass>(json, _jsonOptions);
        Assert.NotNull(value);
        Assert.Equal(output, value.EnumValue);
    }

    /// <summary>
    /// Ensures that the roundtrip serialization & deserialization is successful.
    /// </summary>
    /// <param name="input">Input enum.</param>
    /// <param name="output">Output enum.</param>
    [Theory]
    [InlineData(MediaStreamProtocol.http, MediaStreamProtocol.http)]
    [InlineData(MediaStreamProtocol.hls, MediaStreamProtocol.hls)]
    public void Enum_RoundTrip(MediaStreamProtocol input, MediaStreamProtocol output)
    {
        var inputObj = new TestClass { EnumValue = input };

        var outputObj = JsonSerializer.Deserialize<TestClass>(JsonSerializer.Serialize(inputObj, _jsonOptions), _jsonOptions);

        Assert.NotNull(outputObj);
        Assert.Equal(output, outputObj.EnumValue);
    }

    /// <summary>
    /// Ensures that the roundtrip serialization & deserialization is successful, including null.
    /// </summary>
    /// <param name="input">Input enum.</param>
    /// <param name="output">Output enum.</param>
    [Theory]
    [InlineData(MediaStreamProtocol.http, MediaStreamProtocol.http)]
    [InlineData(MediaStreamProtocol.hls, MediaStreamProtocol.hls)]
    [InlineData(null, null)]
    public void Enum_RoundTrip_Nullable(MediaStreamProtocol? input, MediaStreamProtocol? output)
    {
        var inputObj = new NullTestClass { EnumValue = input };

        var outputObj = JsonSerializer.Deserialize<NullTestClass>(JsonSerializer.Serialize(inputObj, _jsonOptions), _jsonOptions);

        Assert.NotNull(outputObj);
        Assert.Equal(output, outputObj.EnumValue);
    }

    private sealed class TestClass
    {
        public MediaStreamProtocol EnumValue { get; set; }
    }

    private sealed class NullTestClass
    {
        public MediaStreamProtocol? EnumValue { get; set; }
    }
}
