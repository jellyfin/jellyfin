using System;
using System.Text.Json;
using MediaBrowser.Common.Json.Converters;
using Xunit;

namespace Jellyfin.Common.Tests.Json
{
    public static class JsonIso8601ConverterTests
    {
        [Fact]
        public static void Serialize_Success()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(new JsonDateTimeIso8601Converter());
            const string ExpectedValue = "\"2000-01-01T00:00:00.0000000Z\"";
            var inputDateTime = new DateTime(2000, 01, 01);
            var serializedDateTime = JsonSerializer.Serialize(inputDateTime, options);
            Assert.Equal(ExpectedValue, serializedDateTime);
        }
    }
}