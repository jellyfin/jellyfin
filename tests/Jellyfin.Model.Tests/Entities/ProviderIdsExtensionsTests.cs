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
