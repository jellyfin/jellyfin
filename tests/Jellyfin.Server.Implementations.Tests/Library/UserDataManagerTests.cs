using System;
using Emby.Server.Implementations.Library;
using Jellyfin.Database.Implementations;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Model.Configuration;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Library;

public class UserDataManagerTests
{
    private const long ThreeMinuteSongTicks = 3L * 60 * TimeSpan.TicksPerSecond;

    private static UserDataManager CreateManager(int minAudioResumePct = 10, int maxAudioResumePct = 90)
    {
        var config = new ServerConfiguration
        {
            MinAudioResumePct = minAudioResumePct,
            MaxAudioResumePct = maxAudioResumePct
        };
        var configManager = new Mock<IServerConfigurationManager>();
        configManager.Setup(m => m.Configuration).Returns(config);

        return new UserDataManager(
            configManager.Object,
            Mock.Of<IDbContextFactory<JellyfinDbContext>>());
    }

    private static long PercentToTicks(long runtimeTicks, int pct)
        => (long)(runtimeTicks * (pct / 100.0));

    [Theory]
    [InlineData(0, false)] // not started
    [InlineData(5, false)] // not played
    [InlineData(9, false)] // not played
    [InlineData(10, false)] // partially played, not completed
    [InlineData(50, false)] // partially played, not completed
    [InlineData(90, false)] // partially played, not completed
    [InlineData(91, true)] // fully played
    [InlineData(100, true)] // fully played
    public void UpdatePlayState_Audio_SetsPartiallyPlayedCorrectly(int positionPct, bool expectedPartiallyPlayed)
    {
        var manager = CreateManager(minAudioResumePct: 10, maxAudioResumePct: 90);
        var item = new Audio { RunTimeTicks = ThreeMinuteSongTicks };
        var data = new UserItemData { Key = string.Empty };

        manager.UpdatePlayState(item, data, PercentToTicks(ThreeMinuteSongTicks, positionPct));

        Assert.Equal(expectedPartiallyPlayed, data.PartiallyPlayed);
    }

    [Fact]
    public void UpdatePlayState_Audio_FullyPlayed_ClearsPartiallyPlayed()
    {
        // Una canción que antes estaba parcialmente escuchada queda limpia al completarse
        var manager = CreateManager();
        var item = new Audio { RunTimeTicks = ThreeMinuteSongTicks };
        var data = new UserItemData { Key = string.Empty, PartiallyPlayed = true };

        manager.UpdatePlayState(item, data, PercentToTicks(ThreeMinuteSongTicks, 95));

        Assert.False(data.PartiallyPlayed);
        Assert.True(data.Played);
    }

    [Fact]
    public void UpdatePlayState_AudioBook_PartiallyPlayedIsNeverSet()
    {
        // AudioBook : Audio — no debe verse afectado por la lógica de PartiallyPlayed
        var manager = CreateManager(minAudioResumePct: 80, maxAudioResumePct: 95);

        const long sixtyMinuteTicks = 60L * 60 * TimeSpan.TicksPerSecond;
        var thirtyMinuteTicks = 30L * 60 * TimeSpan.TicksPerSecond;

        var item = new AudioBook { RunTimeTicks = sixtyMinuteTicks };
        var data = new UserItemData { Key = string.Empty };

        manager.UpdatePlayState(item, data, thirtyMinuteTicks);

        Assert.False(data.PartiallyPlayed);
    }
    public void UpdatePlayState_Audio_ReturnsExpectedCompletion(int positionPct, bool expectedCompletion)
    {
        var manager = CreateManager(minAudioResumePct: 10, maxAudioResumePct: 90);
        var item = new Audio { RunTimeTicks = ThreeMinuteSongTicks };
        var data = new UserItemData { Key = string.Empty };

        var result = manager.UpdatePlayState(item, data, PercentToTicks(ThreeMinuteSongTicks, positionPct));

        Assert.Equal(expectedCompletion, result);
        Assert.Equal(expectedCompletion, data.Played);
    }

    [Fact]
    public void UpdatePlayState_Audio_FullyPlayed_ResetsPositionToZero()
    {
        var manager = CreateManager();
        var item = new Audio { RunTimeTicks = ThreeMinuteSongTicks };
        var data = new UserItemData { Key = string.Empty };

        manager.UpdatePlayState(item, data, PercentToTicks(ThreeMinuteSongTicks, 95));

        Assert.Equal(0, data.PlaybackPositionTicks);
        Assert.True(data.Played);
    }

    [Fact]
    public void UpdatePlayState_Audio_NeverSetsSkipCount()
    {
        // Verify UpdatePlayState never touches SkipCount regardless of position, should be managed by SessionManager at stop.
        var manager = CreateManager();
        var item = new Audio { RunTimeTicks = ThreeMinuteSongTicks };
        var data = new UserItemData { Key = string.Empty, SkipCount = 3 };

        manager.UpdatePlayState(item, data, PercentToTicks(ThreeMinuteSongTicks, 2));

        Assert.Equal(3, data.SkipCount); // unchanged
    }

    [Fact]
    public void UpdatePlayState_AudioBook_IsNotAffectedByAudioThresholds()
    {
        // AudioBook : Audio - must use the AudioBook branch, not the Audio branch.
        // MinAudioResumePct is set to 80% so that if an AudioBook were mistakenly routed
        // through the Audio branch, 30 min into a 60 min audiobook (= 50%) would fall
        // below the min and position would be reset to 0.
        // Under correct AudioBook handling (MinAudiobookResume default = 5 min),
        // 30 min is well past the minimum and position must be saved.
        var manager = CreateManager(minAudioResumePct: 80, maxAudioResumePct: 95);

        const long sixtyMinuteTicks = 60L * 60 * TimeSpan.TicksPerSecond;
        var thirtyMinuteTicks = 30L * 60 * TimeSpan.TicksPerSecond;

        var item = new AudioBook { RunTimeTicks = sixtyMinuteTicks };
        var data = new UserItemData { Key = string.Empty };

        var result = manager.UpdatePlayState(item, data, thirtyMinuteTicks);

        // AudioBook branch: 30 min in, 30 min remaining > MaxAudiobookResume (5 min default)
        // - not completed, position saved (AudioBook.SupportsPositionTicksResume = true)
        Assert.False(result);
        Assert.False(data.Played);
        Assert.Equal(thirtyMinuteTicks, data.PlaybackPositionTicks);
        Assert.Equal(0, data.SkipCount); // AudioBook skips are not tracked
    }
}
