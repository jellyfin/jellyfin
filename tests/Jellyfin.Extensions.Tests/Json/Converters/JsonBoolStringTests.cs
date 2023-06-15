using System.Text.Json;
using Jellyfin.Extensions.Json.Converters;
using Xunit;

namespace Jellyfin.Extensions.Tests.Json.Converters
{
    public class JsonBoolStringTests
    {
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions()
        {
            Converters =
            {
                new JsonBoolStringConverter()
            }
        };

        [Theory]
        [InlineData(@"{ ""Value"": ""true"" }", true)]
        [InlineData(@"{ ""Value"": ""false"" }", false)]
        public void Deserialize_String_Valid_Success(string input, bool output)
        {
            var s = JsonSerializer.Deserialize<TestStruct>(input, _jsonOptions);
            Assert.Equal(s.Value, output);
        }

        [Theory]
        [InlineData(true, "true")]
        [InlineData(false, "false")]
        public void Serialize_Bool_Success(bool input, string output)
        {
            var value = JsonSerializer.Serialize(input, _jsonOptions);
            Assert.Equal(value, output);
        }

        private readonly record struct TestStruct(bool Value);
    }
}
