using System.Text.Json;
using MediaBrowser.Common.Json.Converters;
using Xunit;

namespace Jellyfin.Common.Tests.Json
{
    public static class JsonBoolNumberTests
    {
        [Theory]
        [InlineData("1", true)]
        [InlineData("0", false)]
        [InlineData("2", true)]
        [InlineData("true", true)]
        [InlineData("false", false)]
        public static void Deserialize_Number_Valid_Success(string input, bool? output)
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(new JsonBoolNumberConverter());
            var value = JsonSerializer.Deserialize<bool>(input, options);
            Assert.Equal(value, output);
        }

        [Theory]
        [InlineData(true, "true")]
        [InlineData(false, "false")]
        public static void Serialize_Bool_Success(bool input, string output)
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(new JsonBoolNumberConverter());
            var value = JsonSerializer.Serialize(input, options);
            Assert.Equal(value, output);
        }
    }
}