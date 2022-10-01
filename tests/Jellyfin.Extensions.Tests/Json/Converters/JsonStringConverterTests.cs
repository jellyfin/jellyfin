using System.Text.Json;
using Jellyfin.Extensions.Json.Converters;
using Xunit;

namespace Jellyfin.Extensions.Tests.Json.Converters
{
    public class JsonStringConverterTests
    {
        private readonly JsonSerializerOptions _jsonSerializerOptions = new()
        {
            Converters =
            {
                new JsonStringConverter()
            }
        };

        [Theory]
        [InlineData("\"test\"", "test")]
        [InlineData("123", "123")]
        [InlineData("123.45", "123.45")]
        [InlineData("true", "true")]
        [InlineData("false", "false")]
        public void Deserialize_String_Valid_Success(string input, string output)
        {
            var deserialized = JsonSerializer.Deserialize<string>(input, _jsonSerializerOptions);
            Assert.Equal(deserialized, output);
        }

        [Fact]
        public void Deserialize_Int32asInt32_Valid_Success()
        {
            const string? input = "123";
            const int output = 123;
            var deserialized = JsonSerializer.Deserialize<int>(input, _jsonSerializerOptions);
            Assert.Equal(output, deserialized);
        }
    }
}
