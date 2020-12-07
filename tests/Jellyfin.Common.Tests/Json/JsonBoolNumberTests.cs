using System.Text.Json;
using Jellyfin.Common.Tests.Models;
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
            var inputJson = $"{{ \"Value\": {input} }}";
            var options = new JsonSerializerOptions();
            var value = JsonSerializer.Deserialize<BoolTypeModel>(inputJson, options);
            Assert.Equal(value?.Value, output);
        }
    }
}