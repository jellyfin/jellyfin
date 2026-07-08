using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Security;
using System.Security.Cryptography;
using System.Text.Json;
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
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Implementations.Authentication;

/// <summary>
/// Handles native OpenID Connect sign-in and logout.
/// </summary>
public class OidcAuthenticationManager : IOidcAuthenticationManager
{
    private const string IdTokenHintProtectorPurpose = "Jellyfin.Server.Implementations.Authentication.OidcAuthenticationManager.IdTokenHint";
    private static readonly TimeSpan ExchangeCodeLifetime = TimeSpan.FromMinutes(2);
    private readonly IOidcConfigurationManager _configurationManager;
    private readonly IDbContextFactory<JellyfinDbContext> _dbProvider;
    private readonly IUserManager _userManager;
    private readonly ISessionManager _sessionManager;
    private readonly IExternalSessionCreator _externalSessionCreator;
    private readonly IMemoryCache _memoryCache;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IDataProtector _idTokenHintProtector;
    private readonly ILogger<OidcAuthenticationManager> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="OidcAuthenticationManager"/> class.
    /// </summary>
    /// <param name="configurationManager">The OpenID Connect configuration manager.</param>
    /// <param name="dbProvider">The Jellyfin database context factory.</param>
    /// <param name="userManager">The user manager.</param>
    /// <param name="sessionManager">The session manager.</param>
    /// <param name="externalSessionCreator">The external session creator.</param>
    /// <param name="memoryCache">The memory cache.</param>
    /// <param name="httpClientFactory">The HTTP client factory.</param>
    /// <param name="dataProtectionProvider">The data protection provider.</param>
    /// <param name="logger">The logger.</param>
    public OidcAuthenticationManager(
        IOidcConfigurationManager configurationManager,
        IDbContextFactory<JellyfinDbContext> dbProvider,
        IUserManager userManager,
        ISessionManager sessionManager,
        IExternalSessionCreator externalSessionCreator,
        IMemoryCache memoryCache,
        IHttpClientFactory httpClientFactory,
        IDataProtectionProvider dataProtectionProvider,
        ILogger<OidcAuthenticationManager> logger)
    {
        _configurationManager = configurationManager;
        _dbProvider = dbProvider;
        _userManager = userManager;
        _sessionManager = sessionManager;
        _externalSessionCreator = externalSessionCreator;
        _memoryCache = memoryCache;
        _httpClientFactory = httpClientFactory;
        _idTokenHintProtector = dataProtectionProvider.CreateProtector(IdTokenHintProtectorPurpose);
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string> CompleteSignInAsync(OidcExternalIdentityRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var provider = _configurationManager.GetEnabledProvider(request.ProviderId)
            ?? throw new SecurityException("OpenID Connect provider is not enabled.");

        ValidateExternalIdentity(request, provider);
        var user = await ResolveUserAsync(request, provider, cancellationToken).ConfigureAwait(false);

        var authenticationResult = await _externalSessionCreator.CreateExternalSession(new ExternalAuthenticationRequest
        {
            UserId = user.Id,
            App = request.App,
            AppVersion = request.AppVersion,
            DeviceId = request.DeviceId,
            DeviceName = request.DeviceName,
            RemoteEndPoint = request.RemoteEndPoint
        }).ConfigureAwait(false);

        await StoreOidcSessionAsync(authenticationResult.AccessToken, request, cancellationToken).ConfigureAwait(false);

        var code = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
        _memoryCache.Set(code, authenticationResult, ExchangeCodeLifetime);
        return code;
    }

    /// <inheritdoc />
    public Task<AuthenticationResult> ExchangeCodeAsync(string code, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code) || !_memoryCache.TryGetValue(code, out AuthenticationResult? authenticationResult) || authenticationResult is null)
        {
            throw new ResourceNotFoundException("OpenID Connect exchange code was not found or has expired.");
        }

        _memoryCache.Remove(code);
        return Task.FromResult(authenticationResult);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ExternalIdentityDto>> GetExternalIdentitiesAsync(Guid userId, CancellationToken cancellationToken)
    {
        var dbContext = await _dbProvider.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using (dbContext.ConfigureAwait(false))
        {
            return await dbContext.OidcExternalIdentities
                .AsNoTracking()
                .Where(identity => identity.UserId.Equals(userId))
                .Select(identity => new ExternalIdentityDto
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
                })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
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

    /// <inheritdoc />
    public async Task<OidcLogoutResult> LogoutAsync(string accessToken, string? returnUrl, CancellationToken cancellationToken)
    {
        var dbContext = await _dbProvider.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        OidcSession? oidcSession;
        await using (dbContext.ConfigureAwait(false))
        {
            oidcSession = await dbContext.OidcSessions
                .FirstOrDefaultAsync(session => session.AccessToken == accessToken, cancellationToken)
                .ConfigureAwait(false);

            if (oidcSession is not null)
            {
                dbContext.OidcSessions.Remove(oidcSession);
                await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        await _sessionManager.Logout(accessToken).ConfigureAwait(false);

        var externalLogoutUrl = oidcSession is null
            ? null
            : await TryBuildExternalLogoutUrlAsync(oidcSession, returnUrl, cancellationToken).ConfigureAwait(false);

        return new OidcLogoutResult
        {
            LocalSessionRevoked = true,
            ExternalLogoutUrl = externalLogoutUrl
        };
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

        var user = await ProvisionUserAsync(request, provider, cancellationToken).ConfigureAwait(false);
        await CreateExternalIdentityAsync(user.Id, request, cancellationToken).ConfigureAwait(false);

        if (provider.AdminGroups.Count > 0 && HasAnyGroup(request.Groups, provider.AdminGroups))
        {
            await SetAdministratorAsync(user.Id, true, cancellationToken).ConfigureAwait(false);
            user = _userManager.GetUserById(user.Id) ?? user;
        }

        return user;
    }

    private async Task<User> ProvisionUserAsync(
        OidcExternalIdentityRequest request,
        OidcProviderOptions provider,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.PreferredUsername))
        {
            throw new SecurityException("OpenID Connect identity did not include a username claim.");
        }

        var existingUser = _userManager.GetUserByName(request.PreferredUsername);
        if (existingUser is not null && provider.ProvisioningMode is OidcUserProvisioningMode.LinkExistingByUsername or OidcUserProvisioningMode.CreateUser)
        {
            return existingUser;
        }

        if (provider.ProvisioningMode == OidcUserProvisioningMode.CreateUser)
        {
            return await _userManager.CreateUserAsync(request.PreferredUsername).ConfigureAwait(false);
        }

        throw new SecurityException("OpenID Connect identity is not linked to a Jellyfin user.");
    }

    private async Task CreateExternalIdentityAsync(Guid userId, OidcExternalIdentityRequest request, CancellationToken cancellationToken)
    {
        var dbContext = await _dbProvider.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using (dbContext.ConfigureAwait(false))
        {
            dbContext.OidcExternalIdentities.Add(new OidcExternalIdentity
            {
                UserId = userId,
                ProviderId = request.ProviderId,
                Issuer = request.Issuer,
                Subject = request.Subject,
                PreferredUsername = request.PreferredUsername,
                Email = request.Email,
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow
            });

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

            user.SetPermission(PermissionKind.IsAdministrator, isAdministrator);
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private void ValidateExternalIdentity(OidcExternalIdentityRequest request, OidcProviderOptions provider)
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

    private async Task StoreOidcSessionAsync(string accessToken, OidcExternalIdentityRequest request, CancellationToken cancellationToken)
    {
        var dbContext = await _dbProvider.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using (dbContext.ConfigureAwait(false))
        {
            dbContext.OidcSessions.Add(new OidcSession
            {
                AccessToken = accessToken,
                ProviderId = request.ProviderId,
                Issuer = request.Issuer,
                Subject = request.Subject,
                Sid = request.SessionId,
                ProtectedIdTokenHint = ProtectIdTokenHint(request.IdTokenHint),
                CreatedAt = DateTime.UtcNow
            });

            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<string?> TryBuildExternalLogoutUrlAsync(OidcSession session, string? returnUrl, CancellationToken cancellationToken)
    {
        var provider = _configurationManager.GetEnabledProvider(session.ProviderId);
        if (provider is null || !provider.EnableRpInitiatedLogout)
        {
            return null;
        }

        try
        {
            var discoveryUri = new Uri(new Uri(provider.Authority + "/", UriKind.Absolute), ".well-known/openid-configuration");
            using var response = await _httpClientFactory.CreateClient().GetAsync(discoveryUri, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (!document.RootElement.TryGetProperty("end_session_endpoint", out var endpointElement))
            {
                return null;
            }

            var endpoint = endpointElement.GetString();
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                return null;
            }

            var query = new List<string>();
            var idTokenHint = UnprotectIdTokenHint(session.ProtectedIdTokenHint);
            if (!string.IsNullOrWhiteSpace(idTokenHint))
            {
                query.Add("id_token_hint=" + Uri.EscapeDataString(idTokenHint));
            }

            if (!string.IsNullOrWhiteSpace(returnUrl))
            {
                query.Add("post_logout_redirect_uri=" + Uri.EscapeDataString(returnUrl));
            }

            return query.Count == 0 ? endpoint : endpoint + "?" + string.Join('&', query);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Unable to build OIDC provider logout URL.");
            return null;
        }
    }

    private string? ProtectIdTokenHint(string? idTokenHint)
    {
        return string.IsNullOrWhiteSpace(idTokenHint) ? null : _idTokenHintProtector.Protect(idTokenHint);
    }

    private string? UnprotectIdTokenHint(string? protectedIdTokenHint)
    {
        if (string.IsNullOrWhiteSpace(protectedIdTokenHint))
        {
            return null;
        }

        try
        {
            return _idTokenHintProtector.Unprotect(protectedIdTokenHint);
        }
        catch (CryptographicException ex)
        {
            _logger.LogDebug(ex, "Unable to unprotect OIDC id token hint.");
            return null;
        }
    }

    private static bool HasAnyGroup(IEnumerable<string> actualGroups, IEnumerable<string> requiredGroups)
    {
        var actual = new HashSet<string>(actualGroups, StringComparer.OrdinalIgnoreCase);
        return requiredGroups.Any(actual.Contains);
    }
}
