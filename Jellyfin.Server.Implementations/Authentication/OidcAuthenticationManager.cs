using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Entities.Security;
using Jellyfin.Database.Implementations.Enums;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Authentication;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Authentication;
using MediaBrowser.Model.Users;
using Microsoft.EntityFrameworkCore;
using SecurityException = MediaBrowser.Controller.Net.SecurityException;

namespace Jellyfin.Server.Implementations.Authentication;

/// <summary>
/// Handles native OpenID Connect sign-in.
/// </summary>
public class OidcAuthenticationManager : IOidcAuthenticationManager
{
    private const int MaxExchangeStates = 1024;
    private const int MaxLinkStates = 1024;
    private const string ProviderNotFoundMessage = "OpenID Connect provider was not found.";
    private static readonly TimeSpan ExchangeCodeLifetime = TimeSpan.FromMinutes(2);
    private readonly IOidcConfigurationManager _configurationManager;
    private readonly IDbContextFactory<JellyfinDbContext> _dbProvider;
    private readonly IUserManager _userManager;
    private readonly INetworkManager _networkManager;
    private readonly IExternalSessionCreator _externalSessionCreator;
    private readonly ConcurrentDictionary<string, OidcExchangeState> _exchangeStates = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, OidcLinkState> _linkStates = new(StringComparer.Ordinal);

    /// <summary>
    /// Initializes a new instance of the <see cref="OidcAuthenticationManager"/> class.
    /// </summary>
    /// <param name="configurationManager">The OpenID Connect configuration manager.</param>
    /// <param name="dbProvider">The Jellyfin database context factory.</param>
    /// <param name="userManager">The user manager.</param>
    /// <param name="networkManager">The network manager.</param>
    /// <param name="externalSessionCreator">The external session creator.</param>
    public OidcAuthenticationManager(
        IOidcConfigurationManager configurationManager,
        IDbContextFactory<JellyfinDbContext> dbProvider,
        IUserManager userManager,
        INetworkManager networkManager,
        IExternalSessionCreator externalSessionCreator)
    {
        _configurationManager = configurationManager;
        _dbProvider = dbProvider;
        _userManager = userManager;
        _networkManager = networkManager;
        _externalSessionCreator = externalSessionCreator;
    }

    /// <inheritdoc />
    public Task<string> CompleteSignInAsync(OidcExternalIdentityRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var provider = _configurationManager.GetEnabledProvider(request.ProviderId)
            ?? throw new SecurityException("OpenID Connect provider is not enabled.");

        ValidateExternalIdentity(request, provider);

        var now = DateTime.UtcNow;
        RemoveExpiredExchangeStates(now);
        EnsureExchangeStateCapacity();
        var code = GenerateCode();
        _exchangeStates[code] = new OidcExchangeState(CloneRequest(request), now.Add(ExchangeCodeLifetime));
        return Task.FromResult(code);
    }

    /// <inheritdoc />
    public async Task<AuthenticationResult> ExchangeCodeAsync(string providerId, string code, string remoteEndPoint, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        RemoveExpiredExchangeStates(now);

        if (string.IsNullOrWhiteSpace(code) || !_exchangeStates.TryGetValue(code, out var state))
        {
            throw new ResourceNotFoundException("OpenID Connect exchange code was not found or has expired.");
        }

        if (state.ExpiresAt <= now)
        {
            _exchangeStates.TryRemove(code, out _);
            throw new ResourceNotFoundException("OpenID Connect exchange code was not found or has expired.");
        }

        if (!string.Equals(state.Request.ProviderId, providerId, StringComparison.OrdinalIgnoreCase)
            || !_exchangeStates.TryRemove(code, out var removedState)
            || !ReferenceEquals(removedState, state))
        {
            throw new ResourceNotFoundException("OpenID Connect exchange code was not found or has expired.");
        }

        var provider = _configurationManager.GetEnabledProvider(providerId)
            ?? throw new SecurityException("OpenID Connect provider is not enabled.");

        ValidateExternalIdentity(state.Request, provider);
        var user = await ResolveUserAsync(state.Request, provider, cancellationToken).ConfigureAwait(false);
        EnsureUserPolicyAllowsLogin(user, remoteEndPoint);
        user = await SyncAdministratorRoleAsync(user, state.Request, provider, cancellationToken).ConfigureAwait(false);

        var authenticationResult = await _externalSessionCreator.CreateExternalSession(new ExternalAuthenticationRequest
        {
            UserId = user.Id,
            App = state.Request.App,
            AppVersion = state.Request.AppVersion,
            DeviceId = state.Request.DeviceId,
            DeviceName = state.Request.DeviceName,
            RemoteEndPoint = remoteEndPoint
        }).ConfigureAwait(false);

        return authenticationResult;
    }

    /// <inheritdoc />
    public Task<string> CreateLinkCodeAsync(string providerId, Guid userId, string? returnUrl, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (userId.Equals(Guid.Empty))
        {
            throw new SecurityException("Jellyfin user was not found.");
        }

        var provider = _configurationManager.GetEnabledProvider(providerId)
            ?? throw new ResourceNotFoundException(ProviderNotFoundMessage);

        var now = DateTime.UtcNow;
        RemoveExpiredLinkStates(now);
        EnsureLinkStateCapacity();
        var code = GenerateCode();
        _linkStates[code] = new OidcLinkState(provider.ProviderId, userId, returnUrl, now.Add(ExchangeCodeLifetime));
        return Task.FromResult(code);
    }

    /// <inheritdoc />
    public Task<OidcLinkRequest> ConsumeLinkCodeAsync(string providerId, string code, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var now = DateTime.UtcNow;
        RemoveExpiredLinkStates(now);

        if (string.IsNullOrWhiteSpace(code) || !_linkStates.TryGetValue(code, out var state))
        {
            throw new ResourceNotFoundException("OpenID Connect link code was not found or has expired.");
        }

        if (state.ExpiresAt <= now)
        {
            _linkStates.TryRemove(code, out _);
            throw new ResourceNotFoundException("OpenID Connect link code was not found or has expired.");
        }

        if (!string.Equals(state.ProviderId, providerId, StringComparison.OrdinalIgnoreCase)
            || !_linkStates.TryRemove(code, out var removedState)
            || !ReferenceEquals(removedState, state))
        {
            throw new ResourceNotFoundException("OpenID Connect link code was not found or has expired.");
        }

        var provider = _configurationManager.GetEnabledProvider(providerId)
            ?? throw new ResourceNotFoundException(ProviderNotFoundMessage);

        return Task.FromResult(new OidcLinkRequest
        {
            ProviderId = provider.ProviderId,
            UserId = state.UserId,
            ReturnUrl = state.ReturnUrl
        });
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ExternalIdentityDto>> GetExternalIdentitiesAsync(Guid userId, CancellationToken cancellationToken)
    {
        var dbContext = await _dbProvider.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using (dbContext.ConfigureAwait(false))
        {
            var identities = await dbContext.OidcExternalIdentities
                .AsNoTracking()
                .Where(identity => identity.UserId.Equals(userId))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return identities.Select(ToExternalIdentityDto).ToList();
        }
    }

    /// <inheritdoc />
    public async Task<ExternalIdentityDto> CreateExternalIdentityAsync(Guid userId, OidcExternalIdentityCreateRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        return await CreateExternalIdentityAsync(
            userId,
            request.ProviderId,
            request.Issuer,
            request.Subject,
            request.PreferredUsername,
            request.Email,
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ExternalIdentityDto> LinkExternalIdentityAsync(Guid userId, OidcExternalIdentityRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var provider = _configurationManager.GetEnabledProvider(request.ProviderId)
            ?? throw new ResourceNotFoundException(ProviderNotFoundMessage);

        ValidateExternalIdentity(request, provider);

        return await CreateExternalIdentityAsync(
            userId,
            provider.ProviderId,
            request.Issuer,
            request.Subject,
            request.PreferredUsername,
            request.Email,
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<ExternalIdentityDto> CreateExternalIdentityAsync(
        Guid userId,
        string requestProviderId,
        string requestIssuer,
        string requestSubject,
        string? preferredUsername,
        string? email,
        CancellationToken cancellationToken)
    {
        var provider = _configurationManager.GetEnabledProvider(requestProviderId)
            ?? throw new ResourceNotFoundException(ProviderNotFoundMessage);

        var issuer = requestIssuer.Trim();
        var subject = requestSubject.Trim();
        if (string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(subject))
        {
            throw new ArgumentException("OpenID Connect identity links require issuer and subject.");
        }

        var dbContext = await _dbProvider.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using (dbContext.ConfigureAwait(false))
        {
            if (!await dbContext.Users.AnyAsync(user => user.Id.Equals(userId), cancellationToken).ConfigureAwait(false))
            {
                throw new ResourceNotFoundException("Jellyfin user was not found.");
            }

            var providerId = provider.ProviderId;
            if (await dbContext.OidcExternalIdentities
                    .AnyAsync(identity => identity.UserId.Equals(userId) && identity.ProviderId == providerId, cancellationToken)
                    .ConfigureAwait(false))
            {
                throw new ArgumentException("Jellyfin user is already linked to this OpenID Connect provider.");
            }

            if (await dbContext.OidcExternalIdentities
                    .AnyAsync(
                        identity => identity.ProviderId == providerId
                                    && identity.Issuer == issuer
                                    && identity.Subject == subject,
                        cancellationToken)
                    .ConfigureAwait(false))
            {
                throw new ArgumentException("OpenID Connect identity is already linked to a Jellyfin user.");
            }

            var identity = CreateExternalIdentity(
                new OidcExternalIdentityLink(userId, providerId, issuer, subject, preferredUsername, email),
                DateTime.UtcNow,
                lastLoginAt: null);

            dbContext.OidcExternalIdentities.Add(identity);
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ToExternalIdentityDto(identity);
        }
    }

    /// <inheritdoc />
    public async Task DeleteExternalIdentityAsync(Guid userId, Guid identityId, CancellationToken cancellationToken)
    {
        var dbContext = await _dbProvider.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using (dbContext.ConfigureAwait(false))
        {
            var identity = await dbContext.OidcExternalIdentities
                .FirstOrDefaultAsync(item => item.UserId.Equals(userId) && item.Id.Equals(identityId), cancellationToken)
                .ConfigureAwait(false)
                ?? throw new ResourceNotFoundException("External identity was not found.");

            dbContext.OidcExternalIdentities.Remove(identity);
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<User> ResolveUserAsync(
        OidcExternalIdentityRequest request,
        OidcProviderOptions provider,
        CancellationToken cancellationToken)
    {
        var dbContext = await _dbProvider.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using (dbContext.ConfigureAwait(false))
        {
            var identity = await dbContext.OidcExternalIdentities
                .FirstOrDefaultAsync(
                    item => item.ProviderId == request.ProviderId
                            && item.Issuer == request.Issuer
                            && item.Subject == request.Subject,
                    cancellationToken)
                .ConfigureAwait(false);

            if (identity is not null)
            {
                identity.PreferredUsername = request.PreferredUsername;
                identity.Email = request.Email;
                identity.LastLoginAt = DateTime.UtcNow;
                await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                return _userManager.GetUserById(identity.UserId)
                    ?? throw new ResourceNotFoundException("Linked Jellyfin user was not found.");
            }
        }

        var user = await ProvisionUserAsync(request, provider).ConfigureAwait(false);
        await CreateProvisionedExternalIdentityAsync(user.Id, request, cancellationToken).ConfigureAwait(false);

        if (!provider.SyncAdminRole && provider.AdminGroups.Count > 0 && HasAnyGroup(request.Groups, provider.AdminGroups))
        {
            await SetAdministratorAsync(user.Id, true, cancellationToken).ConfigureAwait(false);
            user = _userManager.GetUserById(user.Id) ?? user;
        }

        return user;
    }

    private async Task<User> ProvisionUserAsync(
        OidcExternalIdentityRequest request,
        OidcProviderOptions provider)
    {
        if (string.IsNullOrWhiteSpace(request.PreferredUsername))
        {
            throw new SecurityException("OpenID Connect identity did not include a username claim.");
        }

        if (provider.ProvisioningMode == OidcUserProvisioningMode.CreateUser)
        {
            var existingUser = _userManager.GetUserByName(request.PreferredUsername);
            if (existingUser is not null)
            {
                throw new SecurityException("OpenID Connect identity matched an existing Jellyfin username.");
            }

            return await _userManager.CreateUserAsync(request.PreferredUsername).ConfigureAwait(false);
        }

        throw new SecurityException("OpenID Connect identity is not linked to a Jellyfin user.");
    }

    private void EnsureUserPolicyAllowsLogin(User user, string remoteEndPoint)
    {
        if (user.HasPermission(PermissionKind.IsDisabled))
        {
            throw new SecurityException(
                $"The {user.Username} account is currently disabled. Please consult with your administrator.");
        }

        if (!user.HasPermission(PermissionKind.EnableRemoteAccess)
            && !_networkManager.IsInLocalNetwork(remoteEndPoint))
        {
            throw new SecurityException("Forbidden.");
        }

        if (!user.IsParentalScheduleAllowed())
        {
            throw new SecurityException("User is not allowed access at this time.");
        }
    }

    private async Task<User> SyncAdministratorRoleAsync(
        User user,
        OidcExternalIdentityRequest request,
        OidcProviderOptions provider,
        CancellationToken cancellationToken)
    {
        if (!provider.SyncAdminRole || provider.AdminGroups.Count == 0)
        {
            return user;
        }

        await SetAdministratorAsync(user.Id, HasAnyGroup(request.Groups, provider.AdminGroups), cancellationToken).ConfigureAwait(false);
        return _userManager.GetUserById(user.Id) ?? user;
    }

    private async Task CreateProvisionedExternalIdentityAsync(Guid userId, OidcExternalIdentityRequest request, CancellationToken cancellationToken)
    {
        var dbContext = await _dbProvider.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using (dbContext.ConfigureAwait(false))
        {
            var now = DateTime.UtcNow;
            dbContext.OidcExternalIdentities.Add(CreateExternalIdentity(
                new OidcExternalIdentityLink(
                    userId,
                    request.ProviderId,
                    request.Issuer,
                    request.Subject,
                    request.PreferredUsername,
                    request.Email),
                now,
                now));

            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task SetAdministratorAsync(Guid userId, bool isAdministrator, CancellationToken cancellationToken)
    {
        var dbContext = await _dbProvider.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using (dbContext.ConfigureAwait(false))
        {
            var user = await dbContext.Users
                .Include(item => item.Permissions)
                .FirstOrDefaultAsync(item => item.Id.Equals(userId), cancellationToken)
                .ConfigureAwait(false)
                ?? throw new ResourceNotFoundException("Jellyfin user was not found.");

            if (!isAdministrator
                && user.HasPermission(PermissionKind.IsAdministrator)
                && await dbContext.Users
                    .CountAsync(item => item.Permissions.Any(permission => permission.Kind == PermissionKind.IsAdministrator && permission.Value), cancellationToken)
                    .ConfigureAwait(false) == 1)
            {
                throw new SecurityException("There must be at least one user in the system with administrative access.");
            }

            user.SetPermission(PermissionKind.IsAdministrator, isAdministrator);
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private static void ValidateExternalIdentity(OidcExternalIdentityRequest request, OidcProviderOptions provider)
    {
        if (string.IsNullOrWhiteSpace(request.Issuer) || string.IsNullOrWhiteSpace(request.Subject))
        {
            throw new SecurityException("OpenID Connect identity did not include issuer and subject claims.");
        }

        if (provider.RequiredGroups.Count > 0 && !HasAnyGroup(request.Groups, provider.RequiredGroups))
        {
            throw new SecurityException("OpenID Connect identity is not a member of a required group.");
        }
    }

    private void RemoveExpiredExchangeStates(DateTime now)
    {
        RemoveStates(_exchangeStates, state => state.ExpiresAt <= now);
    }

    private void EnsureExchangeStateCapacity()
    {
        if (_exchangeStates.Count < MaxExchangeStates)
        {
            return;
        }

        RemoveOldestStates(_exchangeStates, _exchangeStates.Count - MaxExchangeStates + 1, state => state.ExpiresAt);
    }

    private void RemoveExpiredLinkStates(DateTime now)
    {
        RemoveStates(_linkStates, state => state.ExpiresAt <= now);
    }

    private void EnsureLinkStateCapacity()
    {
        if (_linkStates.Count < MaxLinkStates)
        {
            return;
        }

        RemoveOldestStates(_linkStates, _linkStates.Count - MaxLinkStates + 1, state => state.ExpiresAt);
    }

    private static OidcExternalIdentityRequest CloneRequest(OidcExternalIdentityRequest request)
    {
        return new OidcExternalIdentityRequest
        {
            ProviderId = request.ProviderId,
            Issuer = request.Issuer,
            Subject = request.Subject,
            PreferredUsername = request.PreferredUsername,
            Email = request.Email,
            Groups = request.Groups.ToList(),
            App = request.App,
            AppVersion = request.AppVersion,
            DeviceId = request.DeviceId,
            DeviceName = request.DeviceName,
            RemoteEndPoint = request.RemoteEndPoint
        };
    }

    private static bool HasAnyGroup(IEnumerable<string> actualGroups, IEnumerable<string> requiredGroups)
    {
        var actual = new HashSet<string>(actualGroups, StringComparer.OrdinalIgnoreCase);
        return requiredGroups.Any(actual.Contains);
    }

    private static string GenerateCode()
    {
        return Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLower(CultureInfo.InvariantCulture);
    }

    private static void RemoveStates<TState>(ConcurrentDictionary<string, TState> states, Func<TState, bool> predicate)
    {
        foreach (var key in states
            .Where(state => predicate(state.Value))
            .Select(state => state.Key)
            .ToList())
        {
            states.TryRemove(key, out _);
        }
    }

    private static void RemoveOldestStates<TState>(
        ConcurrentDictionary<string, TState> states,
        int count,
        Func<TState, DateTime> getExpiresAt)
    {
        foreach (var key in states
            .OrderBy(state => getExpiresAt(state.Value))
            .Take(count)
            .Select(state => state.Key)
            .ToList())
        {
            states.TryRemove(key, out _);
        }
    }

    private static ExternalIdentityDto ToExternalIdentityDto(OidcExternalIdentity identity)
    {
        return new ExternalIdentityDto
        {
            Id = identity.Id,
            UserId = identity.UserId,
            ProviderId = identity.ProviderId,
            Issuer = identity.Issuer,
            Subject = identity.Subject,
            PreferredUsername = identity.PreferredUsername,
            Email = identity.Email,
            CreatedAt = identity.CreatedAt,
            LastLoginAt = identity.LastLoginAt
        };
    }

    private static OidcExternalIdentity CreateExternalIdentity(OidcExternalIdentityLink link, DateTime createdAt, DateTime? lastLoginAt)
    {
        return new OidcExternalIdentity
        {
            UserId = link.UserId,
            ProviderId = link.ProviderId,
            Issuer = link.Issuer,
            Subject = link.Subject,
            PreferredUsername = link.PreferredUsername?.Trim(),
            Email = link.Email?.Trim(),
            CreatedAt = createdAt,
            LastLoginAt = lastLoginAt
        };
    }

    private sealed record OidcExchangeState(OidcExternalIdentityRequest Request, DateTime ExpiresAt);

    private sealed record OidcExternalIdentityLink(
        Guid UserId,
        string ProviderId,
        string Issuer,
        string Subject,
        string? PreferredUsername,
        string? Email);

    private sealed record OidcLinkState(string ProviderId, Guid UserId, string? ReturnUrl, DateTime ExpiresAt);
}
