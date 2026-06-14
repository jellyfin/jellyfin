using System;
using System.Collections.Generic;
using Emby.Server.Implementations.Library;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Configuration;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Library;

public class UserDataManagerKeySelectionTests
{
    private readonly UserDataManager _userDataManager;
    private readonly User _user;

    public UserDataManagerKeySelectionTests()
    {
        var config = new Mock<IServerConfigurationManager>();
        config.Setup(c => c.Configuration).Returns(new ServerConfiguration());
        var dbProvider = new Mock<IDbContextFactory<JellyfinDbContext>>();

        _userDataManager = new UserDataManager(config.Object, dbProvider.Object);
        _user = new User("Test User", "Auth provider", "Reset provider");

        // Video.SourceType -> IsActiveRecording() dereferences this static.
        Video.RecordingsManager ??= Mock.Of<IRecordingsManager>(r => r.GetActiveRecordingInfo(It.IsAny<string>()) == null);
    }

    private UserData Row(Guid itemId, string key, bool played) => new UserData
    {
        CustomDataKey = key,
        ItemId = itemId,
        Item = null,
        UserId = _user.Id,
        User = null,
        Played = played,
    };

    [Fact]
    public void GetUserData_StaleRowFirst_PrefersCurrentKeyRow()
    {
        // A re-identified item keeps user data rows under old (stale) keys as well as its current
        // keys. The current key here is the item id (no provider ids set). The stale row is first,
        // so the previous FirstOrDefault logic would have returned it. See #15795.
        var itemId = Guid.NewGuid();
        var movie = new Movie { Id = itemId };
        movie.UserData = new List<UserData>
        {
            Row(itemId, "tt0197013", played: false), // stale key from the wrong match
            Row(itemId, itemId.ToString(), played: true) // current key
        };

        var result = _userDataManager.GetUserData(_user, movie);

        Assert.NotNull(result);
        Assert.True(result!.Played);
    }

    [Fact]
    public void GetUserData_OnlyStaleRows_FallsBackToFirstRow()
    {
        var itemId = Guid.NewGuid();
        var movie = new Movie { Id = itemId };
        movie.UserData = new List<UserData>
        {
            Row(itemId, "tt0197013", played: true)
        };

        var result = _userDataManager.GetUserData(_user, movie);

        Assert.NotNull(result);
        Assert.True(result!.Played);
    }

    [Fact]
    public void GetUserData_NoRows_ReturnsDefaultWithCurrentKey()
    {
        var itemId = Guid.NewGuid();
        var movie = new Movie { Id = itemId };
        movie.UserData = new List<UserData>();

        var result = _userDataManager.GetUserData(_user, movie);

        Assert.NotNull(result);
        Assert.False(result!.Played);
        Assert.Equal(itemId.ToString(), result.Key);
    }
}
