using System;
using Emby.Server.Implementations.Library;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Model.Configuration;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Library;

public class UserDataManagerTests
{
    private readonly UserDataManager _userDataManager;

    public UserDataManagerTests()
    {
        var config = new Mock<IServerConfigurationManager>();
        config.SetupGet(c => c.Configuration).Returns(new ServerConfiguration
        {
            MinResumePct = 5,
            MaxResumePct = 90,
            MinResumeDurationSeconds = 300,
            MinAudiobookResume = 5,
            MaxAudiobookResume = 5
        });

        var repository = Mock.Of<IDbContextFactory<JellyfinDbContext>>();

        _userDataManager = new UserDataManager(config.Object, repository);
    }

    private static AudioBook CreateAudioBook(TimeSpan runtime)
    {
        return new AudioBook
        {
            RunTimeTicks = runtime.Ticks
        };
    }

    private static Movie CreateMovie(TimeSpan runtime)
    {
        return new Movie
        {
            RunTimeTicks = runtime.Ticks
        };
    }

    private static UserItemData CreateUserData(long positionTicks)
    {
        return new UserItemData
        {
            Key = "test-key",
            PlaybackPositionTicks = positionTicks
        };
    }

    [Fact]
    public void UpdatePlayState_AudioBookNearStartReportAfterLongResume_PreservesResume()
    {
        var item = CreateAudioBook(TimeSpan.FromHours(16));
        var existingResume = TimeSpan.FromHours(5) + TimeSpan.FromMinutes(49);
        var data = CreateUserData(existingResume.Ticks);

        var playedToCompletion = _userDataManager.UpdatePlayState(item, data, TimeSpan.FromSeconds(20).Ticks);

        Assert.Equal(existingResume.Ticks, data.PlaybackPositionTicks);
        Assert.False(playedToCompletion);
        Assert.False(data.Played);
    }

    [Fact]
    public void UpdatePlayState_AudioBookNearStartReportWithNoExistingResume_StaysZero()
    {
        var item = CreateAudioBook(TimeSpan.FromHours(16));
        var data = CreateUserData(0);

        var playedToCompletion = _userDataManager.UpdatePlayState(item, data, TimeSpan.FromSeconds(20).Ticks);

        Assert.Equal(0, data.PlaybackPositionTicks);
        Assert.False(playedToCompletion);
    }

    [Fact]
    public void UpdatePlayState_AudioBookLiteralZeroReportAfterLongResume_HonorsExplicitRestart()
    {
        var item = CreateAudioBook(TimeSpan.FromHours(16));
        var existingResume = TimeSpan.FromHours(5) + TimeSpan.FromMinutes(49);
        var data = CreateUserData(existingResume.Ticks);

        var playedToCompletion = _userDataManager.UpdatePlayState(item, data, 0);

        Assert.Equal(0, data.PlaybackPositionTicks);
        Assert.False(playedToCompletion);
    }

    [Fact]
    public void UpdatePlayState_AudioBookNormalForwardProgress_UpdatesPosition()
    {
        var item = CreateAudioBook(TimeSpan.FromHours(16));
        var existingResume = TimeSpan.FromHours(5) + TimeSpan.FromMinutes(49);
        var data = CreateUserData(existingResume.Ticks);
        var reported = TimeSpan.FromHours(6);

        var playedToCompletion = _userDataManager.UpdatePlayState(item, data, reported.Ticks);

        Assert.Equal(reported.Ticks, data.PlaybackPositionTicks);
        Assert.False(playedToCompletion);
    }

    [Fact]
    public void UpdatePlayState_AudioBookNearEndReport_MarksCompletedAndClearsResume()
    {
        var runtime = TimeSpan.FromHours(16);
        var item = CreateAudioBook(runtime);
        var existingResume = TimeSpan.FromHours(5) + TimeSpan.FromMinutes(49);
        var data = CreateUserData(existingResume.Ticks);
        var reported = runtime - TimeSpan.FromMinutes(2);

        var playedToCompletion = _userDataManager.UpdatePlayState(item, data, reported.Ticks);

        Assert.Equal(0, data.PlaybackPositionTicks);
        Assert.True(playedToCompletion);
        Assert.True(data.Played);
    }

    [Fact]
    public void UpdatePlayState_MovieBelowMinResumePctAfterHalfwayResume_PreservesResume()
    {
        var runtime = TimeSpan.FromHours(2);
        var item = CreateMovie(runtime);
        var existingResume = TimeSpan.FromTicks(runtime.Ticks / 2);
        var data = CreateUserData(existingResume.Ticks);
        var reported = TimeSpan.FromTicks((long)(runtime.Ticks * 0.01));

        var playedToCompletion = _userDataManager.UpdatePlayState(item, data, reported.Ticks);

        Assert.Equal(existingResume.Ticks, data.PlaybackPositionTicks);
        Assert.False(playedToCompletion);
    }

    [Fact]
    public void UpdatePlayState_MovieGenuineBackwardSeek_PreservesReportedPosition()
    {
        var runtime = TimeSpan.FromHours(2);
        var item = CreateMovie(runtime);
        var existingResume = TimeSpan.FromTicks(runtime.Ticks / 2);
        var data = CreateUserData(existingResume.Ticks);
        var reported = TimeSpan.FromTicks((long)(runtime.Ticks * 0.40));

        var playedToCompletion = _userDataManager.UpdatePlayState(item, data, reported.Ticks);

        Assert.Equal(reported.Ticks, data.PlaybackPositionTicks);
        Assert.False(playedToCompletion);
    }

    [Fact]
    public void UpdatePlayState_MovieAboveMaxResumePct_MarksCompletedAndClearsResume()
    {
        var runtime = TimeSpan.FromHours(2);
        var item = CreateMovie(runtime);
        var existingResume = TimeSpan.FromTicks(runtime.Ticks / 2);
        var data = CreateUserData(existingResume.Ticks);
        var reported = TimeSpan.FromTicks((long)(runtime.Ticks * 0.96));

        var playedToCompletion = _userDataManager.UpdatePlayState(item, data, reported.Ticks);

        Assert.Equal(0, data.PlaybackPositionTicks);
        Assert.True(playedToCompletion);
        Assert.True(data.Played);
    }
}
