using System;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using Xunit;

namespace Jellyfin.Controller.Tests.Library;

public class VersionResumeDataTests
{
    [Fact]
    public void ApplyTo_OverridesResumeFieldsAndPercentage()
    {
        var lastPlayed = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc);
        var resume = new VersionResumeData(
            new UserItemData { Key = "version", PlaybackPositionTicks = 25, Played = true, LastPlayedDate = lastPlayed },
            RunTimeTicks: 100);

        var dto = new UserItemDataDto { Key = "primary", PlaybackPositionTicks = 1, Played = false, PlayedPercentage = 1 };

        resume.ApplyTo(dto);

        Assert.Equal(25, dto.PlaybackPositionTicks);
        Assert.True(dto.Played);
        Assert.Equal(lastPlayed, dto.LastPlayedDate);

        // The percentage is based on the resume version's own runtime, not the primary's.
        Assert.NotNull(dto.PlayedPercentage);
        Assert.Equal(25.0, dto.PlayedPercentage.Value, 5);
    }

    [Fact]
    public void ApplyTo_WithoutRuntime_LeavesPercentageUntouched()
    {
        var resume = new VersionResumeData(new UserItemData { Key = "version", PlaybackPositionTicks = 25 }, null);
        var dto = new UserItemDataDto { Key = "primary", PlayedPercentage = 42 };

        resume.ApplyTo(dto);

        Assert.Equal(25, dto.PlaybackPositionTicks);
        Assert.NotNull(dto.PlayedPercentage);
        Assert.Equal(42.0, dto.PlayedPercentage.Value, 5);
    }
}
