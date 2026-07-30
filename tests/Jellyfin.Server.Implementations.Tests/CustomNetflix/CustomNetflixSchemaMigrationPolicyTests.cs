using System.Collections.Generic;
using Jellyfin.Server.Implementations.CustomNetflix;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.CustomNetflix;

public class CustomNetflixSchemaMigrationPolicyTests
{
    [Fact]
    public void GetPendingVersions_ReturnsKnownVersionsInOrder()
    {
        var result = CustomNetflixSchemaMigrationPolicy.GetPendingVersions(
            new HashSet<int> { 2 },
            new[] { 3, 1, 2 });

        Assert.Equal(new[] { 1, 3 }, result);
    }

    [Fact]
    public void GetPendingVersions_ReturnsEmptyWhenAllKnownVersionsAreApplied()
    {
        var result = CustomNetflixSchemaMigrationPolicy.GetPendingVersions(
            new HashSet<int>(CustomNetflixSchemaMigrationPolicy.KnownVersions),
            CustomNetflixSchemaMigrationPolicy.KnownVersions);

        Assert.Empty(result);
    }

    [Fact]
    public void KnownVersions_IncludeAllSchemaMigrations()
    {
        Assert.Equal(
            new[]
            {
                CustomNetflixSchemaMigrationPolicy.BaseSchemaVersion,
                CustomNetflixSchemaMigrationPolicy.MyListSchemaVersion,
                CustomNetflixSchemaMigrationPolicy.ActiveProfilesByTokenSchemaVersion,
                CustomNetflixSchemaMigrationPolicy.DisableUnsafeChildProfilesSchemaVersion,
                CustomNetflixSchemaMigrationPolicy.PlaybackProfilePreferencesSchemaVersion,
                CustomNetflixSchemaMigrationPolicy.ProfileItemFeedbackSchemaVersion
            },
            CustomNetflixSchemaMigrationPolicy.KnownVersions);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(CustomNetflixSchemaMigrationPolicy.BaseSchemaVersion, false)]
    [InlineData(CustomNetflixSchemaMigrationPolicy.CurrentSchemaVersion, true)]
    [InlineData(CustomNetflixSchemaMigrationPolicy.CurrentSchemaVersion + 1, true)]
    public void IsCurrent_RequiresLatestSchemaVersion(int appliedVersion, bool expected)
    {
        var result = CustomNetflixSchemaMigrationPolicy.IsCurrent(appliedVersion);

        Assert.Equal(expected, result);
    }
}
