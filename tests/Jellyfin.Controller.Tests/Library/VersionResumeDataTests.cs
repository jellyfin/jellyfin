using System;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using Xunit;

namespace Jellyfin.Controller.Tests.Library;

public class VersionResumeDataTests
{
    [Fact]
    public void ApplyTo_PropagatesCompletionButNotPosition()
    {
        var lastPlayed = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc);
        var resume = new VersionResumeData(
            new UserItemData { Key = "version", PlaybackPositionTicks = 25, Played = true, LastPlayedDate = lastPlayed });

        var dto = new UserItemDataDto { Key = "primary", PlaybackPositionTicks = 1, Played = false, PlayedPercentage = 1 };

        resume.ApplyTo(dto);

        // Completion state propagates to the primary...
        Assert.True(dto.Played);
        Assert.Equal(lastPlayed, dto.LastPlayedDate);

        // ...but the in-progress resume position stays on the version that owns it.
        Assert.Equal(1, dto.PlaybackPositionTicks);
        Assert.Equal(1.0, dto.PlayedPercentage);
    }

    [Fact]
    public void ApplyTo_DoesNotUnsetExistingPlayedOrRegressLastPlayed()
    {
        var primaryLastPlayed = new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc);
        var versionLastPlayed = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc);
        var resume = new VersionResumeData(
            new UserItemData { Key = "version", Played = false, LastPlayedDate = versionLastPlayed });

        var dto = new UserItemDataDto { Key = "primary", Played = true, LastPlayedDate = primaryLastPlayed };

        resume.ApplyTo(dto);

        // A not-yet-completed version must not clear the primary's own completion, and the more recent
        // LastPlayedDate is kept.
        Assert.True(dto.Played);
        Assert.Equal(primaryLastPlayed, dto.LastPlayedDate);
    }
}
