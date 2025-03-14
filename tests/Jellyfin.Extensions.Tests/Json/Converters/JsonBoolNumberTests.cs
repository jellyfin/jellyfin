using System.Text.Json;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Jellyfin.Extensions.Json.Converters;
using Xunit;

namespace Jellyfin.Extensions.Tests.Json.Converters
{
    public class JsonBoolNumberTests
    {
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions()
        {
            Converters =
            {
                new JsonBoolNumberConverter()
            }
        };

        [Theory]
        [InlineData("1", true)]
        [InlineData("0", false)]
        [InlineData("2", true)]
        [InlineData("true", true)]
        [InlineData("false", false)]
        public void Deserialize_Number_Valid_Success(string input, bool? output)
        {
            var value = JsonSerializer.Deserialize<bool>(input, _jsonOptions);
            Assert.Equal(value, output);
        }

        [Theory]
        [InlineData(true, "true")]
        [InlineData(false, "false")]
        public void Serialize_Bool_Success(bool input, string output)
        {
            var value = JsonSerializer.Serialize(input, _jsonOptions);
            Assert.Equal(value, output);
        }

        [Property]
        public Property Deserialize_NonZeroInt_True(NonZeroInt input)
            => JsonSerializer.Deserialize<bool>(input.ToString(), _jsonOptions).ToProperty();
    }
}
