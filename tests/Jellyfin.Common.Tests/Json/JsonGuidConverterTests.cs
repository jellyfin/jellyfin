using System;
using System.Text.Json;
using MediaBrowser.Common.Json.Converters;
using Xunit;

namespace Jellyfin.Common.Tests.Extensions
{
    public static class JsonGuidConverterTests
    {
        [Fact]
        public static void Deserialize_Valid_Success()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(new JsonGuidConverter());
            Guid value = JsonSerializer.Deserialize<Guid>(@"""a852a27afe324084ae66db579ee3ee18""", options);
            Assert.Equal(new Guid("a852a27afe324084ae66db579ee3ee18"), value);

            value = JsonSerializer.Deserialize<Guid>(@"""e9b2dcaa-529c-426e-9433-5e9981f27f2e""", options);
            Assert.Equal(new Guid("e9b2dcaa-529c-426e-9433-5e9981f27f2e"), value);
        }
    }
}
