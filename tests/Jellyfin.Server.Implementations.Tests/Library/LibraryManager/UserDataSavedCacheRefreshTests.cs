using System;
using System.Collections.Generic;
using AutoFixture;
using AutoFixture.AutoMoq;
using Emby.Naming.Common;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Library.LibraryManager;

public sealed class UserDataSavedCacheRefreshTests
{
    private readonly Mock<IUserDataManager> _userDataManager;
    private readonly Emby.Server.Implementations.Library.LibraryManager _libraryManager;
    private readonly Guid _userId = Guid.NewGuid();

    public UserDataSavedCacheRefreshTests()
    {
        var fixture = new Fixture().Customize(new AutoMoqCustomization());
        fixture.Register(() => new NamingOptions());
        _userDataManager = fixture.Freeze<Mock<IUserDataManager>>();
        _libraryManager = fixture.Create<Emby.Server.Implementations.Library.LibraryManager>();
    }

    [Fact]
    public void UserDataSaved_ThroughAnotherInstance_RefreshesTheCachedInstance()
    {
        var id = Guid.NewGuid();
        var cached = new Episode { Id = id, UserData = new List<UserData> { CreateRow(id, played: false) } };
        _libraryManager.RegisterItem(cached);

        var saved = new Episode { Id = id, UserData = new List<UserData> { CreateRow(id, played: true) } };
        RaiseUserDataSaved(saved);

        Assert.Same(saved.UserData, cached.UserData);
    }

    [Fact]
    public void UserDataSaved_ThroughTheCachedInstance_KeepsItsRows()
    {
        var id = Guid.NewGuid();
        var rows = new List<UserData> { CreateRow(id, played: true) };
        var cached = new Episode { Id = id, UserData = rows };
        _libraryManager.RegisterItem(cached);

        RaiseUserDataSaved(cached);

        Assert.Same(rows, cached.UserData);
    }

    [Fact]
    public void UserDataSaved_ItemNotCached_IsIgnored()
    {
        var id = Guid.NewGuid();
        var saved = new Episode { Id = id, UserData = new List<UserData> { CreateRow(id, played: true) } };

        var exception = Record.Exception(() => RaiseUserDataSaved(saved));

        Assert.Null(exception);
    }

    private void RaiseUserDataSaved(BaseItem item)
    {
        _userDataManager.Raise(
            m => m.UserDataSaved += null,
            new UserDataSaveEventArgs
            {
                Item = item,
                UserId = _userId,
                Keys = [],
                SaveReason = UserDataSaveReason.TogglePlayed,
                UserData = new UserItemData { Key = "key" }
            });
    }

    private UserData CreateRow(Guid itemId, bool played)
    {
        return new UserData
        {
            ItemId = itemId,
            Item = null,
            UserId = _userId,
            User = null,
            CustomDataKey = "key",
            Played = played
        };
    }
}
