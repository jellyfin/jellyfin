using System;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using Xunit;

namespace Jellyfin.Controller.Tests.Library;

public class VersionResumeDataTests
{
    [Fact]
    public void ApplyTo_CompletedOtherVersion_PropagatesCompletionAndClearsStaleResume()
    {
        var lastPlayed = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc);
        var resume = new VersionResumeData(
            Guid.NewGuid(),
            new UserItemData { Key = "version", PlaybackPositionTicks = 0, Played = true, LastPlayedDate = lastPlayed });

        var dto = new UserItemDataDto { ItemId = Guid.NewGuid(), Key = "primary", PlaybackPositionTicks = 1, Played = false, PlayedPercentage = 50 };

        resume.ApplyTo(dto);

        // Completion state propagates to the primary...
        Assert.True(dto.Played);
        Assert.Equal(lastPlayed, dto.LastPlayedDate);

        // ...and because the movie was finished on a different version, the primary's own stale resume bar is cleared.
        Assert.Equal(0, dto.PlaybackPositionTicks);
        Assert.Null(dto.PlayedPercentage);
    }

    [Fact]
    public void ApplyTo_PrimaryOwnProgress_KeepsResumePosition()
    {
        var primaryId = Guid.NewGuid();
        var lastPlayed = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc);

        // The winning version is the primary itself (e.g. rewatching): its resume bar must survive.
        var resume = new VersionResumeData(
            primaryId,
            new UserItemData { Key = "primary", PlaybackPositionTicks = 5, Played = true, LastPlayedDate = lastPlayed });

        var dto = new UserItemDataDto { ItemId = primaryId, Key = "primary", PlaybackPositionTicks = 5, Played = true, PlayedPercentage = 20 };

        resume.ApplyTo(dto);

        Assert.True(dto.Played);
        Assert.Equal(5, dto.PlaybackPositionTicks);
        Assert.Equal(20, dto.PlayedPercentage);
    }

    [Fact]
    public void ApplyTo_InProgressOtherVersion_KeepsPrimaryResumePosition()
    {
        var lastPlayed = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc);

        // A different version that is in-progress (not finished) must not clear the primary's position.
        var resume = new VersionResumeData(
            Guid.NewGuid(),
            new UserItemData { Key = "version", PlaybackPositionTicks = 25, Played = false, LastPlayedDate = lastPlayed });

        var dto = new UserItemDataDto { ItemId = Guid.NewGuid(), Key = "primary", PlaybackPositionTicks = 1, Played = false, PlayedPercentage = 50 };

        resume.ApplyTo(dto);

        Assert.False(dto.Played);
        Assert.Equal(1, dto.PlaybackPositionTicks);
        Assert.Equal(50, dto.PlayedPercentage);
    }

    [Fact]
    public void ApplyTo_DoesNotUnsetExistingPlayedOrRegressLastPlayed()
    {
        var primaryLastPlayed = new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc);
        var versionLastPlayed = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc);
        var resume = new VersionResumeData(
            Guid.NewGuid(),
            new UserItemData { Key = "version", Played = false, LastPlayedDate = versionLastPlayed });

        var dto = new UserItemDataDto { ItemId = Guid.NewGuid(), Key = "primary", Played = true, LastPlayedDate = primaryLastPlayed };

        resume.ApplyTo(dto);

        // A not-yet-completed version must not clear the primary's own completion, and the more recent
        // LastPlayedDate is kept.
        Assert.True(dto.Played);
        Assert.Equal(primaryLastPlayed, dto.LastPlayedDate);
    }
}
