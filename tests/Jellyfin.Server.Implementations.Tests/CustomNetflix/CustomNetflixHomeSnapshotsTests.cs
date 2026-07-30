using System;
using Jellyfin.Server.Implementations.CustomNetflix;
using MediaBrowser.Controller.CustomNetflix;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.CustomNetflix;

public class CustomNetflixHomeSnapshotsTests
{
    [Fact]
    public void Deserialize_ReturnsResponseForFreshMatchingPayload()
    {
        var profileId = Guid.NewGuid();
        var snapshotKey = CustomNetflixHomeSnapshots.SnapshotKey(24);
        var now = DateTime.UtcNow;
        var response = new CustomNetflixHomeResponseDto
        {
            ProfileId = profileId,
            GeneratedAt = now,
            Rows = new[]
            {
                new CustomNetflixHomeRowDto
                {
                    Id = "popular-movies",
                    Title = "Films populaires",
                    TitleKey = "customnetflix.home.popular_movies"
                }
            }
        };

        var payload = CustomNetflixHomeSnapshots.Serialize(profileId, snapshotKey, response, now, now.AddMinutes(5));
        var result = CustomNetflixHomeSnapshots.Deserialize(payload, profileId, snapshotKey, now);

        Assert.NotNull(result);
        Assert.Equal(profileId, result!.ProfileId);
        Assert.Single(result.Rows);
        Assert.Equal("popular-movies", result.Rows[0].Id);
        Assert.Equal("customnetflix.home.popular_movies", result.Rows[0].TitleKey);
    }

    [Fact]
    public void Deserialize_ReturnsNullForExpiredPayload()
    {
        var profileId = Guid.NewGuid();
        var snapshotKey = CustomNetflixHomeSnapshots.SnapshotKey(10);
        var now = DateTime.UtcNow;
        var response = new CustomNetflixHomeResponseDto
        {
            ProfileId = profileId,
            GeneratedAt = now.AddMinutes(-10)
        };

        var payload = CustomNetflixHomeSnapshots.Serialize(profileId, snapshotKey, response, now.AddMinutes(-10), now.AddMinutes(-1));
        var result = CustomNetflixHomeSnapshots.Deserialize(payload, profileId, snapshotKey, now);

        Assert.Null(result);
    }

    [Fact]
    public void Deserialize_ReturnsNullForDifferentProfileOrSnapshotKey()
    {
        var profileId = Guid.NewGuid();
        var snapshotKey = CustomNetflixHomeSnapshots.SnapshotKey(10);
        var now = DateTime.UtcNow;
        var response = new CustomNetflixHomeResponseDto
        {
            ProfileId = profileId,
            GeneratedAt = now
        };

        var payload = CustomNetflixHomeSnapshots.Serialize(profileId, snapshotKey, response, now, now.AddMinutes(5));

        Assert.Null(CustomNetflixHomeSnapshots.Deserialize(payload, Guid.NewGuid(), snapshotKey, now));
        Assert.Null(CustomNetflixHomeSnapshots.Deserialize(payload, profileId, CustomNetflixHomeSnapshots.SnapshotKey(11), now));
    }

    [Theory]
    [InlineData(-1, 1)]
    [InlineData(0, 1)]
    [InlineData(24, 24)]
    [InlineData(500, 50)]
    public void NormalizeLimit_ClampsToSupportedVariants(int limit, int expected)
    {
        var result = CustomNetflixHomeSnapshots.NormalizeLimit(limit);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void CacheKeys_ReturnsEverySupportedHomeVariant()
    {
        var profileId = Guid.NewGuid();

        var keys = CustomNetflixHomeSnapshots.CacheKeys(profileId);

        Assert.Equal(50, keys.Length);
        Assert.Equal($"cnx:home:{profileId:N}:v4:l1", keys[0]);
        Assert.Equal($"cnx:home:{profileId:N}:v4:l50", keys[^1]);
    }
}
