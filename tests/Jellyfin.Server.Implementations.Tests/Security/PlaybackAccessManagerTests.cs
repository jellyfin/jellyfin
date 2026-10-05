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
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Dto;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Security;

public sealed class PlaybackAccessManagerTests : IDisposable
{
    private readonly User _user = new("caster", "auth", "reset");
    private readonly Video _item = new() { Id = Guid.NewGuid() };
    private readonly Mock<IUserManager> _users = new();
    private readonly Mock<ILibraryManager> _library = new();
    private readonly Mock<ISessionManager> _sessions = new();
    private readonly Mock<IMediaSourceManager> _sources = new();
    private readonly TestTimeProvider _clock = new();
    private readonly PlaybackAccessManager _manager;

    public PlaybackAccessManagerTests()
    {
        _user.AddDefaultPermissions();
        _users.Setup(users => users.GetUserById(_user.Id)).Returns(_user);
        _library.Setup(library => library.GetItemById<BaseItem>(_item.Id, _user)).Returns(_item);
        _sources.Setup(manager => manager.GetPlaybackMediaSources(_item, _user, false, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new MediaSourceInfo { Id = "source", RunTimeTicks = TimeSpan.FromHours(1).Ticks } });
        _manager = new PlaybackAccessManager(_users.Object, _library.Object, _sources.Object, _sessions.Object, _clock);
    }

    [Fact]
    public async Task AdministratorReceivesAnIndependentPlaybackCredential()
    {
        _user.SetPermission(PermissionKind.IsAdministrator, true);
        var first = await CreateAsync();
        var second = await CreateAsync();

        Assert.Equal(64, first.Token.Length);
        Assert.NotEqual(first.Token, second.Token);
        Assert.Equal(_user.Id, first.UserId);
        Assert.Equal(_item.Id, first.ItemId);
        Assert.Equal("source", first.MediaSourceId);
        Assert.Equal(first, _manager.Get(first.Token));
    }

    [Theory]
    [InlineData(PermissionKind.IsDisabled, true)]
    [InlineData(PermissionKind.EnableMediaPlayback, false)]
    public async Task IssuanceRequiresCurrentPlaybackPermission(PermissionKind permission, bool value)
    {
        _user.SetPermission(permission, value);
        await Assert.ThrowsAsync<SecurityException>(() => CreateAsync());
    }

    [Fact]
    public async Task HiddenItemsCannotReceiveGrants()
    {
        _library.Setup(library => library.GetItemById<BaseItem>(_item.Id, _user)).Returns((BaseItem?)null);
        await Assert.ThrowsAsync<SecurityException>(() => CreateAsync());
    }

    [Fact]
    public async Task UnrelatedMediaSourcesCannotReceiveGrants()
    {
        await Assert.ThrowsAsync<SecurityException>(() => _manager.CreateAsync(_user.Id, _item.Id, "another-source", "renderer", "session", TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(true, false, 60)]
    [InlineData(false, true, 60)]
    [InlineData(false, false, 0)]
    public async Task LiveAndUnknownDurationSourcesCannotReceiveGrants(bool requiresOpening, bool infinite, int durationSeconds)
    {
        _sources.Setup(manager => manager.GetPlaybackMediaSources(_item, _user, false, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new MediaSourceInfo
                {
                    Id = "source",
                    RequiresOpening = requiresOpening,
                    IsInfiniteStream = infinite,
                    RunTimeTicks = TimeSpan.FromSeconds(durationSeconds).Ticks
                }
            });
        await Assert.ThrowsAsync<SecurityException>(() => CreateAsync());
    }

    [Fact]
    public async Task DisablingTheOwnerInvalidatesExistingGrants()
    {
        var grant = await CreateAsync();
        _user.SetPermission(PermissionKind.IsDisabled, true);
        Assert.Null(_manager.Get(grant.Token));
    }

    [Fact]
    public async Task RemovingLibraryAccessInvalidatesExistingGrants()
    {
        var grant = await CreateAsync();
        _library.Setup(library => library.GetItemById<BaseItem>(_item.Id, _user)).Returns((BaseItem?)null);
        Assert.Null(_manager.Get(grant.Token));
    }

    [Fact]
    public async Task IdleGrantExpiresAndCannotBeRevived()
    {
        var grant = await CreateAsync();
        _clock.Advance(TimeSpan.FromHours(4));
        _manager.Touch(grant.Token);
        Assert.Null(_manager.Get(grant.Token));
    }

    [Fact]
    public async Task ActivePlaybackCannotExtendAbsoluteExpiry()
    {
        var grant = await CreateAsync();
        for (var hour = 0; hour < 23; hour++)
        {
            _clock.Advance(TimeSpan.FromHours(1));
            _manager.Touch(grant.Token);
            Assert.NotNull(_manager.Get(grant.Token));
        }

        _clock.Advance(TimeSpan.FromHours(1));
        Assert.Null(_manager.Get(grant.Token));
    }

    [Fact]
    public async Task AnotherUserCannotRevokePlayback()
    {
        var grant = await CreateAsync();
        _manager.Revoke(grant.PlaySessionId, Guid.NewGuid());
        _clock.Advance(TimeSpan.FromMinutes(1));
        Assert.NotNull(_manager.Get(grant.Token));
    }

    [Fact]
    public async Task RevocationAllowsOnlyTheOriginalGracePeriod()
    {
        var grant = await CreateAsync();
        _manager.Revoke(grant.PlaySessionId, grant.UserId);
        Assert.NotNull(_manager.Get(grant.Token));
        _clock.Advance(TimeSpan.FromSeconds(20));
        _manager.Touch(grant.Token);
        _manager.Revoke(grant.PlaySessionId, grant.UserId);
        _clock.Advance(TimeSpan.FromSeconds(11));
        Assert.Null(_manager.Get(grant.Token));
    }

    [Fact]
    public async Task PlaybackStopRevokesTheMatchingSession()
    {
        var grant = await CreateAsync();
        _sessions.Raise(manager => manager.PlaybackStopped += null, new PlaybackStopEventArgs
        {
            PlaySessionId = grant.PlaySessionId,
            Users = new() { _user }
        });
        _clock.Advance(TimeSpan.FromSeconds(31));
        Assert.Null(_manager.Get(grant.Token));
    }

    public void Dispose() => _manager.Dispose();

    private Task<MediaBrowser.Controller.Streaming.PlaybackAccessGrant> CreateAsync()
        => _manager.CreateAsync(_user.Id, _item.Id, "source", "renderer", "session", TestContext.Current.CancellationToken);

    private sealed class TestTimeProvider : TimeProvider
    {
        private DateTimeOffset _now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan elapsed) => _now += elapsed;
    }
}
