using System;
using System.Collections.Generic;
using MediaBrowser.Model.Entities;
using Xunit;

namespace Jellyfin.Model.Tests.Entities
{
    public class ProviderIdsExtensionsTests
    {
        private const string ExampleImdbId = "tt0113375";

        [Fact]
        public void HasProviderId_NullInstance_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => ProviderIdsExtensions.HasProviderId(null!, MetadataProvider.Imdb));
        }

        [Fact]
        public void HasProviderId_NullProvider_False()
        {
            var nullProvider = new ProviderIdsExtensionsTestsObject
            {
                ProviderIds = null!
            };

            Assert.False(nullProvider.HasProviderId(MetadataProvider.Imdb));
        }

        [Fact]
        public void HasProviderId_NullName_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => ProviderIdsExtensionsTestsObject.Empty.HasProviderId(null!));
        }

        [Fact]
        public void HasProviderId_NotFoundName_False()
        {
            Assert.False(ProviderIdsExtensionsTestsObject.Empty.HasProviderId(MetadataProvider.Imdb));
        }

        [Fact]
        public void HasProviderId_FoundName_True()
        {
            var provider = new ProviderIdsExtensionsTestsObject();
            provider.ProviderIds[MetadataProvider.Imdb.ToString()] = ExampleImdbId;

            Assert.True(provider.HasProviderId(MetadataProvider.Imdb));
        }

        [Fact]
        public void HasProviderId_FoundNameEmptyValue_False()
        {
            var provider = new ProviderIdsExtensionsTestsObject();
            provider.ProviderIds[MetadataProvider.Imdb.ToString()] = string.Empty;

            Assert.False(provider.HasProviderId(MetadataProvider.Imdb));
        }

        [Fact]
        public void GetProviderId_NullInstance_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => ProviderIdsExtensions.GetProviderId(null!, MetadataProvider.Imdb));
        }

        [Fact]
        public void GetProviderId_NullName_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => ProviderIdsExtensionsTestsObject.Empty.GetProviderId(null!));
        }

        [Fact]
        public void GetProviderId_NotFoundName_Null()
        {
            Assert.Null(ProviderIdsExtensionsTestsObject.Empty.GetProviderId(MetadataProvider.Imdb));
        }

        [Fact]
        public void GetProviderId_NullProvider_Null()
        {
            var nullProvider = new ProviderIdsExtensionsTestsObject
            {
                ProviderIds = null!
            };

            Assert.Null(nullProvider.GetProviderId(MetadataProvider.Imdb));
        }

        [Fact]
        public void TryGetProviderId_NotFoundName_False()
        {
            Assert.False(ProviderIdsExtensionsTestsObject.Empty.TryGetProviderId(MetadataProvider.Imdb, out _));
        }

        [Fact]
        public void TryGetProviderId_NullProvider_False()
        {
            var nullProvider = new ProviderIdsExtensionsTestsObject
            {
                ProviderIds = null!
            };

            Assert.False(nullProvider.TryGetProviderId(MetadataProvider.Imdb, out _));
        }

        [Fact]
        public void GetProviderId_FoundName_Id()
        {
            var provider = new ProviderIdsExtensionsTestsObject();
            provider.ProviderIds[MetadataProvider.Imdb.ToString()] = ExampleImdbId;

            Assert.Equal(ExampleImdbId, provider.GetProviderId(MetadataProvider.Imdb));
        }

        [Fact]
        public void TryGetProviderId_FoundName_True()
        {
            var provider = new ProviderIdsExtensionsTestsObject();
            provider.ProviderIds[MetadataProvider.Imdb.ToString()] = ExampleImdbId;

            Assert.True(provider.TryGetProviderId(MetadataProvider.Imdb, out var id));
            Assert.Equal(ExampleImdbId, id);
        }

        [Fact]
        public void TryGetProviderId_FoundNameEmptyValue_False()
        {
            var provider = new ProviderIdsExtensionsTestsObject();
            provider.ProviderIds[MetadataProvider.Imdb.ToString()] = string.Empty;

            Assert.False(provider.TryGetProviderId(MetadataProvider.Imdb, out var id));
            Assert.Null(id);
        }

        [Fact]
        public void SetProviderId_NullInstance_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => ProviderIdsExtensions.SetProviderId(null!, MetadataProvider.Imdb, ExampleImdbId));
        }

        [Fact]
        public void SetProviderId_Null_Remove()
        {
            var provider = new ProviderIdsExtensionsTestsObject();
            Assert.Throws<ArgumentNullException>(() => provider.SetProviderId(MetadataProvider.Imdb, null!));
            Assert.Empty(provider.ProviderIds);
        }

        [Fact]
        public void SetProviderId_EmptyName_Remove()
        {
            var provider = new ProviderIdsExtensionsTestsObject();
            provider.ProviderIds[MetadataProvider.Imdb.ToString()] = ExampleImdbId;
            Assert.Throws<ArgumentException>(() => provider.SetProviderId(MetadataProvider.Imdb, string.Empty));
            Assert.Single(provider.ProviderIds);
        }

        [Fact]
        public void SetProviderId_NonEmptyId_Success()
        {
            var provider = new ProviderIdsExtensionsTestsObject();
            provider.SetProviderId(MetadataProvider.Imdb, ExampleImdbId);
            Assert.Single(provider.ProviderIds);
        }

        [Fact]
        public void SetProviderId_NullProvider_Success()
        {
            var nullProvider = new ProviderIdsExtensionsTestsObject
            {
                ProviderIds = null!
            };

            nullProvider.SetProviderId(MetadataProvider.Imdb, ExampleImdbId);
            Assert.Single(nullProvider.ProviderIds);
        }

        [Fact]
        public void SetProviderId_NullProviderAndEmptyName_Success()
        {
            var nullProvider = new ProviderIdsExtensionsTestsObject
            {
                ProviderIds = null!
            };

            Assert.Throws<ArgumentException>(() => nullProvider.SetProviderId(MetadataProvider.Imdb, string.Empty));
            Assert.Null(nullProvider.ProviderIds);
        }

        [Theory]
        [InlineData(nameof(MetadataProvider.Imdb), "tt0113375", true)]
        [InlineData(nameof(MetadataProvider.Imdb), "nm0000123", true)]
        [InlineData(nameof(MetadataProvider.Imdb), "0113375", true)]
        [InlineData(nameof(MetadataProvider.Imdb), "https://www.imdb.com/title/tt0113375", false)]
        [InlineData(nameof(MetadataProvider.Tmdb), "11", true)]
        [InlineData(nameof(MetadataProvider.Tmdb), "nm0000123", false)]
        [InlineData(nameof(MetadataProvider.Tmdb), "0", false)]
        [InlineData(nameof(MetadataProvider.Tmdb), "-11", false)]
        [InlineData(nameof(MetadataProvider.TmdbCollection), "nm0000123", false)]
        [InlineData(nameof(MetadataProvider.AudioDbArtist), "111239", true)]
        [InlineData(nameof(MetadataProvider.AudioDbArtist), "a3cb23fc-acd3-4ce0-8f36-1e5aa6a18432", false)]
        [InlineData(nameof(MetadataProvider.MusicBrainzArtist), "a3cb23fc-acd3-4ce0-8f36-1e5aa6a18432", true)]
        [InlineData(nameof(MetadataProvider.MusicBrainzArtist), "111239", false)]
        [InlineData(nameof(MetadataProvider.MusicBrainzAlbum), "not-an-mbid", false)]
        [InlineData(nameof(MetadataProvider.Tvdb), "anything-goes", true)]
        [InlineData("SomePlugin", "anything-goes", true)]
        [InlineData(nameof(MetadataProvider.Tmdb), null, false)]
        [InlineData(null, "11", false)]
        public void IsValidProviderId_ChecksKnownFormats(string? name, string? value, bool expected)
        {
            Assert.Equal(expected, ProviderIdsExtensions.IsValidProviderId(name, value));
        }

        [Fact]
        public void TrySetProviderId_ForeignId_False()
        {
            var provider = new ProviderIdsExtensionsTestsObject();

            Assert.False(provider.TrySetProviderId(MetadataProvider.Tmdb, "nm0000123"));
            Assert.Empty(provider.ProviderIds);
        }

        [Fact]
        public void TrySetProviderId_ForeignId_KeepsExisting()
        {
            var provider = new ProviderIdsExtensionsTestsObject();
            provider.ProviderIds[MetadataProvider.Tmdb.ToString()] = "11";

            Assert.False(provider.TrySetProviderId(MetadataProvider.Tmdb, "nm0000123"));
            Assert.Equal("11", provider.GetProviderId(MetadataProvider.Tmdb));
        }

        [Theory]
        [InlineData(nameof(MetadataProvider.Imdb), " tt0113375 ")]
        [InlineData(" Imdb", ExampleImdbId)]
        public void TrySetProviderId_SurroundingWhitespace_Trimmed(string name, string value)
        {
            var provider = new ProviderIdsExtensionsTestsObject();

            Assert.True(provider.TrySetProviderId(name, value));
            Assert.Equal(ExampleImdbId, provider.GetProviderId(MetadataProvider.Imdb));
        }

        [Fact]
        public void SetProviderIds_ReplacesAll()
        {
            var provider = new ProviderIdsExtensionsTestsObject();
            provider.ProviderIds[MetadataProvider.Tvdb.ToString()] = "12345";

            provider.SetProviderIds(new Dictionary<string, string>
            {
                [MetadataProvider.Imdb.ToString()] = ExampleImdbId
            });

            Assert.Equal(ExampleImdbId, provider.GetProviderId(MetadataProvider.Imdb));
            Assert.False(provider.HasProviderId(MetadataProvider.Tvdb));
        }

        [Fact]
        public void SetProviderIds_ForeignId_Dropped()
        {
            var provider = new ProviderIdsExtensionsTestsObject();

            provider.SetProviderIds(new Dictionary<string, string>
            {
                [MetadataProvider.Tmdb.ToString()] = "nm0000123",
                [MetadataProvider.Imdb.ToString()] = ExampleImdbId,
                [MetadataProvider.Tvdb.ToString()] = string.Empty
            });

            Assert.False(provider.HasProviderId(MetadataProvider.Tmdb));
            Assert.False(provider.HasProviderId(MetadataProvider.Tvdb));
            Assert.Equal(ExampleImdbId, provider.GetProviderId(MetadataProvider.Imdb));
        }

        [Fact]
        public void SetProviderIds_Null_Clears()
        {
            var provider = new ProviderIdsExtensionsTestsObject();
            provider.ProviderIds[MetadataProvider.Imdb.ToString()] = ExampleImdbId;

            provider.SetProviderIds(null);

            Assert.Empty(provider.ProviderIds);
        }

        [Fact]
        public void SetProviderIds_NullInstance_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => ProviderIdsExtensions.SetProviderIds(null!, new Dictionary<string, string>()));
        }

        [Fact]
        public void RemoveProviderId_Null_Remove()
        {
            var provider = new ProviderIdsExtensionsTestsObject();

            provider.ProviderIds[MetadataProvider.Imdb.ToString()] = ExampleImdbId;
            provider.RemoveProviderId(MetadataProvider.Imdb);
            Assert.Empty(provider.ProviderIds);
        }

        private sealed class ProviderIdsExtensionsTestsObject : IHasProviderIds
        {
            public static readonly ProviderIdsExtensionsTestsObject Empty = new ProviderIdsExtensionsTestsObject();

            public Dictionary<string, string> ProviderIds { get; set; } = new Dictionary<string, string>();
        }
    }
}
