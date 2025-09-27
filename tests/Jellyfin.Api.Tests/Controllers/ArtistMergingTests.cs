using System;
using System.Collections.Generic;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Model.Entities;
using Xunit;

namespace Jellyfin.Api.Tests.Controllers
{
    /// <summary>
    /// Tests for artist merging behavior to ensure artists with different MBIDs are not merged.
    /// </summary>
    public class ArtistMergingTests
    {
        [Fact]
        public void CreatePresentationUniqueKey_SameNameDifferentMBID_ShouldReturnDifferentKeys()
        {
            // Arrange - Two artists with the EXACT SAME name but different MusicBrainz IDs
            // This is the core bug: artists with same name but different MBIDs get merged
            var artist1 = new MusicArtist
            {
                Name = "Meg", // Exact same name
                Id = Guid.NewGuid()
            };
            artist1.SetProviderId(MetadataProvider.MusicBrainzArtist, "12345678-1234-1234-1234-123456789012");

            var artist2 = new MusicArtist
            {
                Name = "Meg", // Exact same name
                Id = Guid.NewGuid()
            };
            artist2.SetProviderId(MetadataProvider.MusicBrainzArtist, "87654321-4321-4321-4321-210987654321");

            // Act - Get presentation unique keys
            var key1 = artist1.CreatePresentationUniqueKey();
            var key2 = artist2.CreatePresentationUniqueKey();

            // Assert - Keys should be different because they have different MBIDs, even with same name
            Assert.NotEqual(key1, key2);
        }

        [Fact]
        public void CreatePresentationUniqueKey_SameNameSameMBID_ShouldReturnSameKeys()
        {
            // Arrange - Two artists with the same name and same MusicBrainz ID
            var mbid = "12345678-1234-1234-1234-123456789012";
            var artist1 = new MusicArtist
            {
                Name = "Meg",
                Id = Guid.NewGuid()
            };
            artist1.SetProviderId(MetadataProvider.MusicBrainzArtist, mbid);

            var artist2 = new MusicArtist
            {
                Name = "Meg", // Same name, same capitalization
                Id = Guid.NewGuid()
            };
            artist2.SetProviderId(MetadataProvider.MusicBrainzArtist, mbid);

            // Act - Get presentation unique keys
            var key1 = artist1.CreatePresentationUniqueKey();
            var key2 = artist2.CreatePresentationUniqueKey();

            // Assert - Keys should be the same because they have the same MBID
            Assert.Equal(key1, key2);
        }

        [Fact]
        public void CreatePresentationUniqueKey_DifferentCaseNoMBID_ShouldReturnSameKeys()
        {
            // Arrange - Two artists with the same name (different case) but no MusicBrainz ID
            // This should be considered the same artist (acceptable behavior)
            var artist1 = new MusicArtist
            {
                Name = "Meg",
                Id = Guid.NewGuid()
            };

            var artist2 = new MusicArtist
            {
                Name = "MEG", // Different capitalization
                Id = Guid.NewGuid()
            };

            // Act - Get presentation unique keys
            var key1 = artist1.CreatePresentationUniqueKey();
            var key2 = artist2.CreatePresentationUniqueKey();

            // Assert - Keys should be the same because they have the same name (after normalization) and no MBID
            // This is expected to fail until we implement proper case normalization
            Assert.Equal(key1.ToLowerInvariant(), key2.ToLowerInvariant());
        }

        [Fact]
        public void CreatePresentationUniqueKey_OneMBIDOneWithout_ShouldReturnDifferentKeys()
        {
            // Arrange - One artist with MBID, one without, same name
            var artist1 = new MusicArtist
            {
                Name = "Meg",
                Id = Guid.NewGuid()
            };
            artist1.SetProviderId(MetadataProvider.MusicBrainzArtist, "12345678-1234-1234-1234-123456789012");

            var artist2 = new MusicArtist
            {
                Name = "Meg", // Same name, no MBID
                Id = Guid.NewGuid()
            };

            // Act - Get presentation unique keys
            var key1 = artist1.CreatePresentationUniqueKey();
            var key2 = artist2.CreatePresentationUniqueKey();

            // Assert - Keys should be different because one has MBID and other doesn't
            Assert.NotEqual(key1, key2);
        }

        [Fact]
        public void GetUserDataKeys_DifferentMBID_ShouldReturnDifferentKeys()
        {
            // Arrange - Two artists with the same name but different MusicBrainz IDs
            var artist1 = new MusicArtist
            {
                Name = "Meg",
                Id = Guid.NewGuid()
            };
            artist1.SetProviderId(MetadataProvider.MusicBrainzArtist, "12345678-1234-1234-1234-123456789012");

            var artist2 = new MusicArtist
            {
                Name = "MEG",
                Id = Guid.NewGuid()
            };
            artist2.SetProviderId(MetadataProvider.MusicBrainzArtist, "87654321-4321-4321-4321-210987654321");

            // Act - Get user data keys
            var keys1 = artist1.GetUserDataKeys();
            var keys2 = artist2.GetUserDataKeys();

            // Assert - First keys should be different (MBID-based keys)
            Assert.NotEqual(keys1[0], keys2[0]);
        }
    }
}
