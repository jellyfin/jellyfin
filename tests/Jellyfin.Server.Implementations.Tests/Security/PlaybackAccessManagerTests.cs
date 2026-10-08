using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Server.Implementations.Security;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Streaming;
using MediaBrowser.Model.Dto;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Security;

public class PlaybackAccessManagerTests
{
    private readonly User _user = new("user", "auth", "reset");
    private readonly Video _item = new() { Id = Guid.NewGuid() };
    private readonly Mock<ILibraryManager> _libraryManager = new();
    private readonly PlaybackAccessManager _manager;

    public PlaybackAccessManagerTests()
    {
        _user.AddDefaultPermissions();
        var userManager = new Mock<IUserManager>();
        userManager.Setup(i => i.GetUserById(_user.Id)).Returns(_user);
        _libraryManager.Setup(i => i.GetItemById<BaseItem>(_item.Id, _user)).Returns(_item);
        var mediaSourceManager = new Mock<IMediaSourceManager>();
        mediaSourceManager.Setup(i => i.GetPlaybackMediaSources(_item, _user, false, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new MediaSourceInfo { Id = "source" }]);
        _manager = new PlaybackAccessManager(userManager.Object, _libraryManager.Object, mediaSourceManager.Object);
    }

    [Theory]
    [InlineData(PermissionKind.IsDisabled, true)]
    [InlineData(PermissionKind.EnableMediaPlayback, false)]
    public async Task CreateAsync_UserCannotPlay_Throws(PermissionKind permission, bool value)
    {
        _user.SetPermission(permission, value);

        await Assert.ThrowsAsync<SecurityException>(() => CreateAsync());
    }

    [Fact]
    public async Task CreateAsync_InaccessibleItem_Throws()
    {
        _libraryManager.Setup(i => i.GetItemById<BaseItem>(_item.Id, _user)).Returns((BaseItem?)null);

        await Assert.ThrowsAsync<SecurityException>(() => CreateAsync());
    }

    [Fact]
    public async Task CreateAsync_UnknownMediaSource_Throws()
    {
        await Assert.ThrowsAsync<SecurityException>(() => CreateAsync("other"));
    }

    [Fact]
    public async Task Get_DisabledUser_ReturnsNull()
    {
        var grant = await CreateAsync();
        _user.SetPermission(PermissionKind.IsDisabled, true);

        Assert.Null(_manager.Get(grant.Token));
    }

    [Fact]
    public async Task Revoke_OnlyOwnPlaySession_RemovesGrant()
    {
        var grant = await CreateAsync();

        _manager.Revoke(grant.PlaySessionId, Guid.NewGuid());
        Assert.NotNull(_manager.Get(grant.Token));

        _manager.Revoke(grant.PlaySessionId, grant.UserId);
        Assert.Null(_manager.Get(grant.Token));
    }

    private Task<PlaybackAccessGrant> CreateAsync(string mediaSourceId = "source")
        => _manager.CreateAsync(_user.Id, _item.Id, mediaSourceId, "device", "session", TestContext.Current.CancellationToken);
}
