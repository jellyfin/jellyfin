using System;
using System.Text.Json;
using MediaBrowser.Common.Json.Converters;
using Xunit;

namespace Jellyfin.Common.Tests.Json
{
    public class JsonVersionConverterTests
    {
        private readonly JsonSerializerOptions _options;

        public JsonVersionConverterTests()
        {
            _options = new JsonSerializerOptions();
            _options.Converters.Add(new JsonVersionConverter());
        }

        [Fact]
        public void Deserialize_Version_Success()
        {
            var input = "\"1.025.222\"";
            var output = new Version(1, 25, 222);
            var deserializedInput = JsonSerializer.Deserialize<Version>(input, _options);
            Assert.Equal(output, deserializedInput);
        }

        [Fact]
        public void Serialize_Version_Success()
        {
            var input = new Version(1, 09, 59);
            var output = "\"1.9.59\"";
            var serializedInput = JsonSerializer.Serialize(input, _options);
            Assert.Equal(output, serializedInput);
        }
    }
}