using System.Text.Json;
using System.Text.Json.Serialization;
using Jellyfin.Extensions.Json.Converters;
using MediaBrowser.Model.Entities;
using Xunit;

namespace Jellyfin.Extensions.Tests.Json.Converters
{
    public class JsonLowerCaseConverterTests
    {
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions()
        {
            Converters =
            {
                new JsonStringEnumConverter()
            }
        };

        [Theory]
        [InlineData(null, "{\"CollectionType\":null}")]
        [InlineData(CollectionTypeOptions.Movies, "{\"CollectionType\":\"movies\"}")]
        [InlineData(CollectionTypeOptions.MusicVideos, "{\"CollectionType\":\"musicvideos\"}")]
        public void Serialize_CollectionTypeOptions_Correct(CollectionTypeOptions? collectionType, string expected)
        {
            Assert.Equal(expected, JsonSerializer.Serialize(new TestContainer(collectionType), _jsonOptions));
        }

        [Theory]
        [InlineData("{\"CollectionType\":null}", null)]
        [InlineData("{\"CollectionType\":\"movies\"}", CollectionTypeOptions.Movies)]
        [InlineData("{\"CollectionType\":\"musicvideos\"}", CollectionTypeOptions.MusicVideos)]
        public void Deserialize_CollectionTypeOptions_Correct(string json, CollectionTypeOptions? result)
        {
            var res = JsonSerializer.Deserialize<TestContainer>(json, _jsonOptions);
            Assert.NotNull(res);
            Assert.Equal(result, res!.CollectionType);
        }

        [Theory]
        [InlineData(null)]
        [InlineData(CollectionTypeOptions.Movies)]
        [InlineData(CollectionTypeOptions.MusicVideos)]
        public void RoundTrip_CollectionTypeOptions_Correct(CollectionTypeOptions? value)
        {
            var res = JsonSerializer.Deserialize<TestContainer>(JsonSerializer.Serialize(new TestContainer(value), _jsonOptions), _jsonOptions);
            Assert.NotNull(res);
            Assert.Equal(value, res!.CollectionType);
        }

        [Theory]
        [InlineData("{\"CollectionType\":null}")]
        [InlineData("{\"CollectionType\":\"movies\"}")]
        [InlineData("{\"CollectionType\":\"musicvideos\"}")]
        public void RoundTrip_String_Correct(string json)
        {
            var res = JsonSerializer.Serialize(JsonSerializer.Deserialize<TestContainer>(json, _jsonOptions), _jsonOptions);
            Assert.Equal(json, res);
        }

        private class TestContainer
        {
            public TestContainer(CollectionTypeOptions? collectionType)
            {
                CollectionType = collectionType;
            }

            [JsonConverter(typeof(JsonLowerCaseConverter<CollectionTypeOptions?>))]
            public CollectionTypeOptions? CollectionType { get; set; }
        }
    }
}
