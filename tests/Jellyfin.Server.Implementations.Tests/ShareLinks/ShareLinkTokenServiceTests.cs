using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Server.Implementations.ShareLinks;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.ShareLinks;
using MediaBrowser.Model.Configuration;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.ShareLinks;

/// <summary>
/// TC-200 token service tests: creation, key binding, scope, revocation and expiry.
/// </summary>
public sealed class ShareLinkTokenServiceTests
{
    private static readonly Guid _userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid _itemId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid _otherItemId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly DateTimeOffset _start = DateTimeOffset.FromUnixTimeSeconds(1_700_000_000);

    private readonly InMemoryShareLinkRepository _repository = new InMemoryShareLinkRepository();
    private readonly FakeTimeProvider _timeProvider = new FakeTimeProvider(_start);
    private readonly Mock<IUserManager> _userManager = new Mock<IUserManager>();
    private readonly Mock<IServerConfigurationManager> _configurationManager = new Mock<IServerConfigurationManager>();
    private readonly User _user;
    private readonly ShareLinkTokenService _service;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShareLinkTokenServiceTests"/> class.
    /// </summary>
    public ShareLinkTokenServiceTests()
    {
        _user = new User("alice", "auth-provider", "reset-provider")
        {
            Id = _userId
        };

        _userManager
            .Setup(m => m.GetUserById(It.IsAny<Guid>()))
            .Returns((Guid id) => id.Equals(_userId) ? _user : null);
        _userManager
            .Setup(m => m.UpdateUserAsync(It.IsAny<User>()))
            .Returns(Task.CompletedTask);

        _configurationManager
            .Setup(c => c.Configuration)
            .Returns(new ServerConfiguration { MediaSharingPolicy = MediaSharingPolicy.Enabled });

        _service = new ShareLinkTokenService(
            _userManager.Object,
            _repository,
            new ShareLinkPolicyService(_configurationManager.Object),
            _timeProvider);
    }

    [Fact]
    public async Task CreateThenValidate_RoundTrip()
    {
        var created = await _service.CreateAsync(_userId, _itemId, null, 3600, TestContext.Current.CancellationToken);

        Assert.True(created.Succeeded);
        Assert.NotNull(created.Token);
        Assert.NotNull(created.ShareLink);

        var validated = await _service.ValidateAsync(created.Token!, _itemId, TestContext.Current.CancellationToken);

        Assert.True(validated.IsValid);
        Assert.Equal(created.ShareLink!.Id, validated.Jti);
        Assert.Equal(_userId, validated.UserId);
        Assert.Equal(_itemId, validated.ItemId);
        Assert.Equal(_start.ToUnixTimeSeconds(), validated.IssuedAt);
        Assert.Null(validated.ExpiresAt);
        Assert.Equal(3600, validated.DurationSeconds);
    }

    [Fact]
    public async Task TamperedToken_ReturnsDecryptFailed()
    {
        var created = await _service.CreateAsync(_userId, _itemId, null, 3600, TestContext.Current.CancellationToken);
        string[] parts = created.Token!.Split('.');
        char[] body = parts[2].ToCharArray();
        body[10] = body[10] == 'A' ? 'B' : 'A';
        parts[2] = new string(body);
        string tampered = string.Join('.', parts);

        var validated = await _service.ValidateAsync(tampered, _itemId, TestContext.Current.CancellationToken);

        Assert.Equal(ShareLinkErrorCode.DecryptFailed, validated.ErrorCode);
    }

    [Fact]
    public async Task Create_WithBlankKey_GeneratesAndPersistsKey()
    {
        _user.ShareLinkKey = string.Empty;

        var created = await _service.CreateAsync(_userId, _itemId, null, 3600, TestContext.Current.CancellationToken);

        Assert.True(created.Succeeded);
        Assert.False(ShareLinkKeyHelper.IsNullOrBlank(_user.ShareLinkKey));
        _userManager.Verify(m => m.UpdateUserAsync(_user), Times.Once);

        var validated = await _service.ValidateAsync(created.Token!, _itemId, TestContext.Current.CancellationToken);
        Assert.True(validated.IsValid);
    }

    [Fact]
    public async Task RotatedKey_ReturnsDecryptFailed()
    {
        var created = await _service.CreateAsync(_userId, _itemId, null, 3600, TestContext.Current.CancellationToken);
        _user.ShareLinkKey = ShareLinkKeyHelper.Generate();

        var validated = await _service.ValidateAsync(created.Token!, _itemId, TestContext.Current.CancellationToken);

        Assert.Equal(ShareLinkErrorCode.DecryptFailed, validated.ErrorCode);
    }

    [Fact]
    public async Task DisabledUser_ReturnsUserDisabled()
    {
        var created = await _service.CreateAsync(_userId, _itemId, null, 3600, TestContext.Current.CancellationToken);
        _user.SetPermission(PermissionKind.IsDisabled, true);

        var validated = await _service.ValidateAsync(created.Token!, _itemId, TestContext.Current.CancellationToken);

        Assert.Equal(ShareLinkErrorCode.UserDisabled, validated.ErrorCode);
    }

    [Fact]
    public async Task WrongRouteItem_ReturnsItemMismatch()
    {
        var created = await _service.CreateAsync(_userId, _itemId, null, 3600, TestContext.Current.CancellationToken);

        var validated = await _service.ValidateAsync(created.Token!, _otherItemId, TestContext.Current.CancellationToken);

        Assert.Equal(ShareLinkErrorCode.ItemMismatch, validated.ErrorCode);
    }

    [Fact]
    public async Task RevokedRow_ReturnsLinkRevoked()
    {
        var created = await _service.CreateAsync(_userId, _itemId, null, 3600, TestContext.Current.CancellationToken);
        await _repository.RevokeAsync(created.ShareLink!.Id, TestContext.Current.CancellationToken);

        var validated = await _service.ValidateAsync(created.Token!, _itemId, TestContext.Current.CancellationToken);

        Assert.Equal(ShareLinkErrorCode.LinkRevoked, validated.ErrorCode);
    }

    [Fact]
    public async Task AbsoluteExpirationPassed_ReturnsLinkExpired()
    {
        long expiresAt = _start.ToUnixTimeSeconds() + 100;
        var created = await _service.CreateAsync(_userId, _itemId, expiresAt, null, TestContext.Current.CancellationToken);
        _timeProvider.Advance(TimeSpan.FromSeconds(200));

        var validated = await _service.ValidateAsync(created.Token!, _itemId, TestContext.Current.CancellationToken);

        Assert.Equal(ShareLinkErrorCode.LinkExpired, validated.ErrorCode);
    }

    [Fact]
    public async Task DurationElapsedAfterFirstAccess_ReturnsLinkExpired()
    {
        var created = await _service.CreateAsync(_userId, _itemId, null, 60, TestContext.Current.CancellationToken);

        var first = await _service.ValidateAsync(created.Token!, _itemId, TestContext.Current.CancellationToken);
        Assert.True(first.IsValid);

        _timeProvider.Advance(TimeSpan.FromSeconds(61));
        var second = await _service.ValidateAsync(created.Token!, _itemId, TestContext.Current.CancellationToken);

        Assert.Equal(ShareLinkErrorCode.LinkExpired, second.ErrorCode);
    }

    [Fact]
    public async Task MissingRow_ReturnsLinkStateMissing()
    {
        var created = await _service.CreateAsync(_userId, _itemId, null, 3600, TestContext.Current.CancellationToken);
        _repository.Remove(created.ShareLink!.Id);

        var validated = await _service.ValidateAsync(created.Token!, _itemId, TestContext.Current.CancellationToken);

        Assert.Equal(ShareLinkErrorCode.LinkStateMissing, validated.ErrorCode);
    }

    [Fact]
    public async Task Create_ServerSharingDisabled_ReturnsSharingDisabled()
    {
        SetServerMediaSharingPolicy(MediaSharingPolicy.Disabled);

        var result = await _service.CreateAsync(
            _userId,
            _itemId,
            null,
            3600,
            TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Equal(ShareLinkCreateErrorCode.SharingDisabled, result.ErrorCode);
    }

    [Fact]
    public async Task Validate_ServerSharingSwitchedOff_ReturnsSharingDisabled()
    {
        var created = await _service.CreateAsync(
            _userId,
            _itemId,
            null,
            3600,
            TestContext.Current.CancellationToken);
        Assert.True(created.Succeeded);
        Assert.NotNull(created.Token);

        SetServerMediaSharingPolicy(MediaSharingPolicy.Disabled);

        var validation = await _service.ValidateAsync(
            created.Token,
            _itemId,
            TestContext.Current.CancellationToken);

        Assert.False(validation.IsValid);
        Assert.Equal(ShareLinkErrorCode.SharingDisabled, validation.ErrorCode);
    }

    [Fact]
    public async Task Create_PerUserDefaultNo_RequiresExplicitUserEnable()
    {
        SetServerMediaSharingPolicy(MediaSharingPolicy.PerUserDefaultNo);

        _user.MediaSharingPolicy = UserMediaSharingPolicy.Default;
        var defaultsToDisabled = await _service.CreateAsync(
            _userId,
            _itemId,
            null,
            3600,
            TestContext.Current.CancellationToken);
        Assert.Equal(ShareLinkCreateErrorCode.SharingDisabled, defaultsToDisabled.ErrorCode);

        _user.MediaSharingPolicy = UserMediaSharingPolicy.Enabled;
        var explicitlyEnabled = await _service.CreateAsync(
            _userId,
            _itemId,
            null,
            3600,
            TestContext.Current.CancellationToken);
        Assert.True(explicitlyEnabled.Succeeded);
    }

    [Fact]
    public async Task Create_HardServerEnable_IgnoresPerUserDisable()
    {
        SetServerMediaSharingPolicy(MediaSharingPolicy.Enabled);
        _user.MediaSharingPolicy = UserMediaSharingPolicy.Disabled;

        var result = await _service.CreateAsync(
            _userId,
            _itemId,
            null,
            3600,
            TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
    }

    private void SetServerMediaSharingPolicy(MediaSharingPolicy policy)
    {
        _configurationManager
            .Setup(c => c.Configuration)
            .Returns(new ServerConfiguration { MediaSharingPolicy = policy });
    }

    private sealed class FakeTimeProvider : TimeProvider
    {
        private DateTimeOffset _utcNow;

        public FakeTimeProvider(DateTimeOffset utcNow)
        {
            _utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public void Advance(TimeSpan delta) => _utcNow += delta;
    }

    private sealed class InMemoryShareLinkRepository : IShareLinkRepository
    {
        private readonly object _sync = new object();
        private readonly Dictionary<string, ShareLink> _links = new Dictionary<string, ShareLink>();

        public Task<ShareLink?> GetByIdAsync(string jti, CancellationToken cancellationToken = default)
        {
            lock (_sync)
            {
                return Task.FromResult(_links.TryGetValue(jti, out var link) ? link : null);
            }
        }

        public Task CreateAsync(ShareLink shareLink, CancellationToken cancellationToken = default)
        {
            lock (_sync)
            {
                _links[shareLink.Id] = shareLink;
            }

            return Task.CompletedTask;
        }

        public Task<bool> TrySetFirstAccessAsync(string jti, long unixSecondsFirstAccessed, CancellationToken cancellationToken = default)
        {
            lock (_sync)
            {
                if (_links.TryGetValue(jti, out var link) && link.FirstAccessedAt is null)
                {
                    link.FirstAccessedAt = unixSecondsFirstAccessed;
                    return Task.FromResult(true);
                }

                return Task.FromResult(false);
            }
        }

        public Task<bool> RevokeAsync(string jti, CancellationToken cancellationToken = default)
        {
            lock (_sync)
            {
                if (_links.TryGetValue(jti, out var link))
                {
                    link.IsRevoked = true;
                    return Task.FromResult(true);
                }

                return Task.FromResult(false);
            }
        }

        public Task<IReadOnlyList<ShareLink>> ListByUserAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            lock (_sync)
            {
                IReadOnlyList<ShareLink> result = _links.Values
                    .Where(link => link.UserId.Equals(userId))
                    .OrderByDescending(link => link.CreatedAt)
                    .ToList();
                return Task.FromResult(result);
            }
        }

        public Task<IReadOnlyList<ShareLink>> ListByItemAsync(Guid userId, Guid itemId, CancellationToken cancellationToken = default)
        {
            lock (_sync)
            {
                IReadOnlyList<ShareLink> result = _links.Values
                    .Where(link => link.UserId.Equals(userId) && link.ItemId.Equals(itemId))
                    .OrderByDescending(link => link.CreatedAt)
                    .ToList();
                return Task.FromResult(result);
            }
        }

        public Task DeleteUserLinksAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            lock (_sync)
            {
                foreach (string jti in _links.Values.Where(link => link.UserId.Equals(userId)).Select(link => link.Id).ToList())
                {
                    _links.Remove(jti);
                }
            }

            return Task.CompletedTask;
        }

        public Task<int> PurgeExpiredAsync(long unixNow, CancellationToken cancellationToken = default)
        {
            lock (_sync)
            {
                var expired = _links.Values
                    .Where(link => link.IsRevoked
                        || (link.ExpiresAt is { } expiresAt && unixNow >= expiresAt)
                        || (link.DurationSeconds is { } duration
                            && link.FirstAccessedAt is { } firstAccessedAt
                            && unixNow > firstAccessedAt + duration))
                    .Select(link => link.Id)
                    .ToList();

                foreach (string jti in expired)
                {
                    _links.Remove(jti);
                }

                return Task.FromResult(expired.Count);
            }
        }

        public void Remove(string jti)
        {
            lock (_sync)
            {
                _links.Remove(jti);
            }
        }
    }
}
