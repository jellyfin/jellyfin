using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Entities.Security;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Database.Providers.Sqlite;
using Jellyfin.Server.Implementations.Authentication;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Authentication;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Authentication;
using MediaBrowser.Model.Dto;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using SecurityException = MediaBrowser.Controller.Net.SecurityException;

namespace Jellyfin.Server.Implementations.Tests.Authentication;

public sealed class OidcAuthenticationManagerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<JellyfinDbContext> _dbOptions;
    private readonly Mock<IOidcConfigurationManager> _configurationManager = new();
    private readonly Mock<IUserManager> _userManager = new();
    private readonly Mock<INetworkManager> _networkManager = new();
    private readonly Mock<IExternalSessionCreator> _externalSessionCreator = new();
    private readonly Dictionary<Guid, User> _usersById = new();
    private readonly List<OidcProviderOptions> _providers = new();
    private readonly OidcAuthenticationManager _manager;
    private int _createdSessionCount;

    public OidcAuthenticationManagerTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _dbOptions = new DbContextOptionsBuilder<JellyfinDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var ctx = CreateDbContext();
        ctx.Database.EnsureCreated();

        var factory = new Mock<IDbContextFactory<JellyfinDbContext>>();
        factory.Setup(f => f.CreateDbContext()).Returns(CreateDbContext);
        factory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateDbContext);

        _configurationManager
            .Setup(manager => manager.GetEnabledProvider(It.IsAny<string>()))
            .Returns((string providerId) => _providers
                .FirstOrDefault(provider => provider.Enabled && string.Equals(provider.ProviderId, providerId, StringComparison.OrdinalIgnoreCase)));

        _userManager.Setup(manager => manager.GetUserById(It.IsAny<Guid>()))
            .Returns((Guid userId) => _usersById.GetValueOrDefault(userId));
        _userManager.Setup(manager => manager.GetUserByName(It.IsAny<string>()))
            .Returns((string username) => _usersById.Values.FirstOrDefault(user => string.Equals(user.Username, username, StringComparison.OrdinalIgnoreCase)));
        _userManager.Setup(manager => manager.CreateUserAsync(It.IsAny<string>()))
            .Returns((string username) => CreateUserAsync(username));
        _networkManager.Setup(manager => manager.IsInLocalNetwork(It.IsAny<string>())).Returns(true);

        _externalSessionCreator
            .Setup(creator => creator.CreateExternalSession(It.IsAny<ExternalAuthenticationRequest>()))
            .Returns((ExternalAuthenticationRequest request) =>
            {
                _createdSessionCount++;
                return Task.FromResult(new AuthenticationResult
                {
                    AccessToken = "access-token-" + _createdSessionCount,
                    ServerId = "server",
                    User = new UserDto { Id = request.UserId },
                    SessionInfo = new SessionInfoDto { UserId = request.UserId }
                });
            });

        _manager = new OidcAuthenticationManager(
            _configurationManager.Object,
            factory.Object,
            _userManager.Object,
            _networkManager.Object,
            _externalSessionCreator.Object);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    [Fact]
    public async Task ExchangeCodeAsync_WhenWrongProviderThenCorrectProvider_OnlyCorrectProviderCreatesSessionAndCodeIsSingleUse()
    {
        _providers.Add(CreateProvider("authelia", OidcUserProvisioningMode.CreateUser));
        _providers.Add(CreateProvider("other", OidcUserProvisioningMode.CreateUser));

        var code = await _manager.CompleteSignInAsync(CreateRequest("authelia", "alice"), CancellationToken.None);

        _externalSessionCreator.Verify(creator => creator.CreateExternalSession(It.IsAny<ExternalAuthenticationRequest>()), Times.Never);
        await Assert.ThrowsAsync<ResourceNotFoundException>(() => _manager.ExchangeCodeAsync("other", code, "127.0.0.1", CancellationToken.None));
        _externalSessionCreator.Verify(creator => creator.CreateExternalSession(It.IsAny<ExternalAuthenticationRequest>()), Times.Never);

        var result = await _manager.ExchangeCodeAsync("authelia", code, "127.0.0.1", CancellationToken.None);

        Assert.Equal("access-token-1", result.AccessToken);
        _externalSessionCreator.Verify(creator => creator.CreateExternalSession(It.IsAny<ExternalAuthenticationRequest>()), Times.Once);

        await Assert.ThrowsAsync<ResourceNotFoundException>(() => _manager.ExchangeCodeAsync("authelia", code, "127.0.0.1", CancellationToken.None));
        _externalSessionCreator.Verify(creator => creator.CreateExternalSession(It.IsAny<ExternalAuthenticationRequest>()), Times.Once);
    }

    [Fact]
    public async Task ExchangeCodeAsync_WhenCreateUserUsernameCollides_ThrowsAndDoesNotLinkIdentityOrCreateSession()
    {
        _providers.Add(CreateProvider("authelia", OidcUserProvisioningMode.CreateUser));
        await SeedUserAsync("alice");

        var code = await _manager.CompleteSignInAsync(CreateRequest("authelia", "ALICE"), CancellationToken.None);

        await Assert.ThrowsAsync<SecurityException>(() => _manager.ExchangeCodeAsync("authelia", code, "127.0.0.1", CancellationToken.None));

        Assert.Equal(0, CountOidcExternalIdentities());
        _externalSessionCreator.Verify(creator => creator.CreateExternalSession(It.IsAny<ExternalAuthenticationRequest>()), Times.Never);
    }

    [Fact]
    public async Task ExchangeCodeAsync_WhenSyncAdminRoleWouldRemoveOnlyAdmin_ThrowsAndKeepsAdmin()
    {
        _providers.Add(CreateProvider("authelia", OidcUserProvisioningMode.Disabled, syncAdminRole: true, adminGroups: ["admins"]));
        var admin = await SeedUserAsync("admin", isAdministrator: true);
        await SeedExternalIdentityAsync(admin.Id, "authelia", "https://issuer.example.com", "subject");

        var code = await _manager.CompleteSignInAsync(CreateRequest("authelia", "admin", groups: []), CancellationToken.None);

        await Assert.ThrowsAsync<SecurityException>(() => _manager.ExchangeCodeAsync("authelia", code, "127.0.0.1", CancellationToken.None));

        using var ctx = CreateDbContext();
        var storedAdmin = ctx.Users.Include(user => user.Permissions).Single(user => user.Id.Equals(admin.Id));
        Assert.True(storedAdmin.HasPermission(PermissionKind.IsAdministrator));
        _externalSessionCreator.Verify(creator => creator.CreateExternalSession(It.IsAny<ExternalAuthenticationRequest>()), Times.Never);
    }

    [Fact]
    public async Task ExchangeCodeAsync_WhenSyncAdminRoleDemotesNonLastAdmin_DemotesAndCreatesSession()
    {
        _providers.Add(CreateProvider("authelia", OidcUserProvisioningMode.Disabled, syncAdminRole: true, adminGroups: ["admins"]));
        var linkedAdmin = await SeedUserAsync("linked-admin", isAdministrator: true);
        var otherAdmin = await SeedUserAsync("other-admin", isAdministrator: true);
        await SeedExternalIdentityAsync(linkedAdmin.Id, "authelia", "https://issuer.example.com", "subject");

        var code = await _manager.CompleteSignInAsync(CreateRequest("authelia", "linked-admin", groups: []), CancellationToken.None);
        await _manager.ExchangeCodeAsync("authelia", code, "127.0.0.1", CancellationToken.None);

        using var ctx = CreateDbContext();
        var storedLinkedAdmin = ctx.Users.Include(user => user.Permissions).Single(user => user.Id.Equals(linkedAdmin.Id));
        var storedOtherAdmin = ctx.Users.Include(user => user.Permissions).Single(user => user.Id.Equals(otherAdmin.Id));
        Assert.False(storedLinkedAdmin.HasPermission(PermissionKind.IsAdministrator));
        Assert.True(storedOtherAdmin.HasPermission(PermissionKind.IsAdministrator));
    }

    [Fact]
    public async Task ExchangeCodeAsync_UsesExchangeRemoteEndPointForPolicyAndSession()
    {
        _providers.Add(CreateProvider("authelia", OidcUserProvisioningMode.CreateUser));
        _networkManager
            .Setup(manager => manager.IsInLocalNetwork("198.51.100.10"))
            .Returns(true);

        var code = await _manager.CompleteSignInAsync(CreateRequest("authelia", "alice", remoteEndPoint: "127.0.0.1"), CancellationToken.None);

        var result = await _manager.ExchangeCodeAsync("authelia", code, "198.51.100.10", CancellationToken.None);

        Assert.Equal("access-token-1", result.AccessToken);
        _networkManager.Verify(manager => manager.IsInLocalNetwork("198.51.100.10"), Times.Once);
        _networkManager.Verify(manager => manager.IsInLocalNetwork("127.0.0.1"), Times.Never);
        _externalSessionCreator.Verify(
            creator => creator.CreateExternalSession(It.Is<ExternalAuthenticationRequest>(request => request.RemoteEndPoint == "198.51.100.10")),
            Times.Once);
    }

    [Fact]
    public async Task ConsumeLinkCodeAsync_WhenWrongProviderThenCorrectProvider_OnlyCorrectProviderConsumesAndCodeIsSingleUse()
    {
        _providers.Add(CreateProvider("authelia", OidcUserProvisioningMode.Disabled));
        _providers.Add(CreateProvider("other", OidcUserProvisioningMode.Disabled));
        var userId = Guid.NewGuid();

        var code = await _manager.CreateLinkCodeAsync("authelia", userId, "/web", CancellationToken.None);

        await Assert.ThrowsAsync<ResourceNotFoundException>(() => _manager.ConsumeLinkCodeAsync("other", code, CancellationToken.None));

        var linkRequest = await _manager.ConsumeLinkCodeAsync("authelia", code, CancellationToken.None);

        Assert.Equal("authelia", linkRequest.ProviderId);
        Assert.Equal(userId, linkRequest.UserId);
        Assert.Equal("/web", linkRequest.ReturnUrl);
        await Assert.ThrowsAsync<ResourceNotFoundException>(() => _manager.ConsumeLinkCodeAsync("authelia", code, CancellationToken.None));
    }

    private JellyfinDbContext CreateDbContext()
    {
        return new JellyfinDbContext(
            _dbOptions,
            NullLogger<JellyfinDbContext>.Instance,
            new SqliteDatabaseProvider(null!, NullLogger<SqliteDatabaseProvider>.Instance),
            new NoLockBehavior(NullLogger<NoLockBehavior>.Instance));
    }

    private async Task<User> SeedUserAsync(string username, bool isAdministrator = false)
    {
        var user = new User(username, "Default", "Default");
        user.SetPermission(PermissionKind.IsAdministrator, isAdministrator);
        using var ctx = CreateDbContext();
        ctx.Users.Add(user);
        await ctx.SaveChangesAsync();
        _usersById[user.Id] = user;
        return user;
    }

    private async Task<User> CreateUserAsync(string username)
    {
        if (_usersById.Values.Any(user => string.Equals(user.Username, username, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ArgumentException("User already exists.", nameof(username));
        }

        return await SeedUserAsync(username);
    }

    private async Task SeedExternalIdentityAsync(Guid userId, string providerId, string issuer, string subject)
    {
        using var ctx = CreateDbContext();
        ctx.OidcExternalIdentities.Add(new OidcExternalIdentity
        {
            UserId = userId,
            ProviderId = providerId,
            Issuer = issuer,
            Subject = subject,
            PreferredUsername = "linked",
            CreatedAt = DateTime.UtcNow
        });

        await ctx.SaveChangesAsync();
    }

    private int CountOidcExternalIdentities()
    {
        using var ctx = CreateDbContext();
        return ctx.OidcExternalIdentities.Count();
    }

    private static OidcProviderOptions CreateProvider(
        string providerId,
        OidcUserProvisioningMode provisioningMode,
        bool syncAdminRole = false,
        IReadOnlyList<string>? adminGroups = null)
    {
        return new OidcProviderOptions
        {
            Enabled = true,
            ProviderId = providerId,
            Name = providerId,
            Authority = "https://issuer.example.com",
            ClientId = "jellyfin",
            ClientSecret = "secret",
            Scopes = ["openid", "profile", "email", "groups"],
            UsernameClaim = "preferred_username",
            RoleClaim = "groups",
            EmailClaim = "email",
            ProvisioningMode = provisioningMode,
            SyncAdminRole = syncAdminRole,
            AdminGroups = adminGroups ?? []
        };
    }

    private static OidcExternalIdentityRequest CreateRequest(
        string providerId,
        string username,
        IReadOnlyList<string>? groups = null,
        string remoteEndPoint = "127.0.0.1")
    {
        return new OidcExternalIdentityRequest
        {
            ProviderId = providerId,
            Issuer = "https://issuer.example.com",
            Subject = "subject",
            PreferredUsername = username,
            Email = username + "@example.com",
            Groups = groups ?? [],
            App = "app",
            AppVersion = "1",
            DeviceId = "device",
            DeviceName = "device",
            RemoteEndPoint = remoteEndPoint
        };
    }
}
