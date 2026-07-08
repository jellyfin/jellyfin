using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Constants;
using Jellyfin.Api.Extensions;
using MediaBrowser.Common;
using MediaBrowser.Common.Api;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Authentication;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace Jellyfin.Api.Controllers;

/// <summary>
/// Native OpenID Connect authentication controller.
/// </summary>
[Route("auth/oidc")]
[Tags("Authentication")]
public class OidcController : BaseJellyfinApiController
{
    private const string AppProperty = "jellyfin:app";
    private const string AppVersionProperty = "jellyfin:appVersion";
    private const string DeviceIdProperty = "jellyfin:deviceId";
    private const string DeviceNameProperty = "jellyfin:deviceName";
    private const string LinkUserIdProperty = "jellyfin:linkUserId";
    private const string LinkCodeQueryParameter = "code";
    private const string ExchangeCodeQueryParameter = "oidc_code";
    private readonly IOidcConfigurationManager _configurationManager;
    private readonly IOidcAuthenticationManager _authenticationManager;
    private readonly IAuthenticationSchemeProvider _schemeProvider;
    private readonly IUserManager _userManager;
    private readonly IApplicationHost _applicationHost;

    /// <summary>
    /// Initializes a new instance of the <see cref="OidcController"/> class.
    /// </summary>
    /// <param name="configurationManager">The OIDC configuration manager.</param>
    /// <param name="authenticationManager">The OIDC authentication manager.</param>
    /// <param name="schemeProvider">The authentication scheme provider.</param>
    /// <param name="userManager">The user manager.</param>
    /// <param name="applicationHost">The application host.</param>
    public OidcController(
        IOidcConfigurationManager configurationManager,
        IOidcAuthenticationManager authenticationManager,
        IAuthenticationSchemeProvider schemeProvider,
        IUserManager userManager,
        IApplicationHost applicationHost)
    {
        _configurationManager = configurationManager;
        _authenticationManager = authenticationManager;
        _schemeProvider = schemeProvider;
        _userManager = userManager;
        _applicationHost = applicationHost;
    }

    /// <summary>
    /// Gets enabled OpenID Connect providers.
    /// </summary>
    /// <response code="200">Providers returned.</response>
    /// <returns>The enabled providers.</returns>
    [HttpGet("providers")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<OidcProviderInfo>>> GetProviders()
    {
        var providers = new List<OidcProviderInfo>();
        foreach (var provider in _configurationManager.GetProviderInfos())
        {
            if (await AreProviderSchemesRegisteredAsync(provider.ProviderId).ConfigureAwait(false))
            {
                provider.RedirectUri = GetProviderCallbackUri(provider.ProviderId);
                providers.Add(provider);
            }
        }

        return new OkObjectResult(providers);
    }

    /// <summary>
    /// Gets the OpenID Connect configuration.
    /// </summary>
    /// <response code="200">Configuration returned.</response>
    /// <returns>The secret-safe configuration.</returns>
    [HttpGet("configuration", Name = "GetOidcConfiguration")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public ActionResult<OidcConfigurationDto> GetConfiguration()
    {
        var configuration = _configurationManager.GetConfiguration();
        AddProviderRedirectUris(configuration);
        return Ok(configuration);
    }

    /// <summary>
    /// Updates the OpenID Connect configuration.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <response code="200">Configuration updated.</response>
    /// <response code="400">Configuration is invalid.</response>
    /// <returns>The update result.</returns>
    [HttpPost("configuration", Name = "UpdateOidcConfiguration")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<OidcConfigurationUpdateResult>> UpdateConfiguration(
        [FromBody, Required] OidcConfigurationUpdateDto configuration,
        CancellationToken cancellationToken)
    {
        try
        {
            await _configurationManager.UpdateConfigurationAsync(configuration, cancellationToken).ConfigureAwait(false);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }

        var requiresRestart = _configurationManager.GetConfiguration().RequiresRestart;
        if (requiresRestart)
        {
            _applicationHost.NotifyPendingRestart();
        }

        return Ok(new OidcConfigurationUpdateResult
        {
            RequiresRestart = requiresRestart
        });
    }

    /// <summary>
    /// Starts browser OpenID Connect authentication.
    /// </summary>
    /// <param name="providerId">The provider id.</param>
    /// <param name="app">The app name.</param>
    /// <param name="appVersion">The app version.</param>
    /// <param name="deviceId">The device id.</param>
    /// <param name="deviceName">The device name.</param>
    /// <param name="returnUrl">The optional relative return URL.</param>
    /// <returns>An OpenID Connect challenge.</returns>
    [HttpGet("{providerId}/start")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Start(
        [FromRoute, Required] string providerId,
        [FromQuery, Required] string app,
        [FromQuery, Required] string appVersion,
        [FromQuery, Required] string deviceId,
        [FromQuery, Required] string deviceName,
        [FromQuery] string? returnUrl)
    {
        if (!await IsProviderStartableAsync(providerId).ConfigureAwait(false))
        {
            return NotFound();
        }

        ValidateStart(app, appVersion, deviceId, deviceName, returnUrl);
        var properties = BuildAuthenticationProperties(providerId, app, appVersion, deviceId, deviceName, returnUrl);
        return Challenge(properties, AuthenticationSchemes.GetOidcScheme(providerId));
    }

    /// <summary>
    /// Creates an OpenID Connect start URL for clients that launch an external browser.
    /// </summary>
    /// <param name="providerId">The provider id.</param>
    /// <param name="request">The start request.</param>
    /// <returns>The start URL.</returns>
    [HttpPost("{providerId}/start")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OidcStartResult>> CreateStartUrl(
        [FromRoute, Required] string providerId,
        [FromBody, Required] OidcStartRequest request)
    {
        if (!await IsProviderStartableAsync(providerId).ConfigureAwait(false))
        {
            return NotFound();
        }

        ValidateStart(request.App, request.AppVersion, request.DeviceId, request.DeviceName, request.ReturnUrl);

        var url = Url.ActionLink(
            nameof(Start),
            values: new
            {
                providerId,
                app = request.App,
                appVersion = request.AppVersion,
                deviceId = request.DeviceId,
                deviceName = request.DeviceName,
                returnUrl = request.ReturnUrl
            });

        return Ok(new OidcStartResult
        {
            Url = url ?? BuildStartPath(providerId, request)
        });
    }

    /// <summary>
    /// Completes OpenID Connect authentication after provider callback.
    /// </summary>
    /// <param name="providerId">The provider id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A one-time exchange code or redirect.</returns>
    [HttpGet("{providerId}/complete")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(OidcExchangeResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Complete([FromRoute, Required] string providerId, CancellationToken cancellationToken)
    {
        if (!await IsProviderStartableAsync(providerId).ConfigureAwait(false))
        {
            return NotFound();
        }

        // ASP.NET Core's OpenID Connect handler owns the callback, correlation, and nonce validation.
        // This endpoint only consumes the provider-specific external cookie produced by that handler.
        var provider = _configurationManager.GetEnabledProvider(providerId)!;
        var authenticationResult = await HttpContext.AuthenticateAsync(AuthenticationSchemes.GetOidcExternalCookieScheme(providerId)).ConfigureAwait(false);
        if (!authenticationResult.Succeeded || authenticationResult.Principal is null || authenticationResult.Properties is null)
        {
            return Unauthorized();
        }

        string code;
        var returnUrl = GetProperty(authenticationResult.Properties, OidcConstants.ReturnUrlProperty);
        try
        {
            try
            {
                var request = BuildExternalIdentityRequest(provider, authenticationResult.Principal, authenticationResult.Properties);
                code = await _authenticationManager.CompleteSignInAsync(request, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (IsOidcLocalFailure(ex) && !string.IsNullOrWhiteSpace(returnUrl) && IsSafeRelativeUrl(returnUrl))
            {
                return LocalRedirect(AppendOidcError(returnUrl, OidcConstants.LocalFailureError));
            }
        }
        finally
        {
            await HttpContext.SignOutAsync(AuthenticationSchemes.GetOidcExternalCookieScheme(providerId)).ConfigureAwait(false);
        }

        if (!string.IsNullOrWhiteSpace(returnUrl))
        {
            if (!IsSafeRelativeUrl(returnUrl))
            {
                return Unauthorized();
            }

            return LocalRedirect(AppendExchangeCode(returnUrl, code));
        }

        return new OkObjectResult(new OidcExchangeResult
        {
            Code = code
        });
    }

    /// <summary>
    /// Starts OpenID Connect account linking for the current user.
    /// </summary>
    /// <param name="providerId">The provider id.</param>
    /// <param name="returnUrl">The optional relative return URL.</param>
    /// <returns>An OpenID Connect challenge.</returns>
    [HttpGet("{providerId}/link/start")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> StartLink(
        [FromRoute, Required] string providerId,
        [FromQuery] string? returnUrl)
    {
        if (!await IsProviderStartableAsync(providerId).ConfigureAwait(false))
        {
            return NotFound();
        }

        if (!string.IsNullOrWhiteSpace(returnUrl) && !IsSafeRelativeUrl(returnUrl))
        {
            return BadRequest("Return URL must be a relative URL.");
        }

        var userId = User.GetUserId();
        if (userId.Equals(Guid.Empty))
        {
            return Unauthorized();
        }

        var properties = BuildLinkAuthenticationProperties(providerId, userId, returnUrl);
        return Challenge(properties, AuthenticationSchemes.GetOidcScheme(providerId));
    }

    /// <summary>
    /// Launches OpenID Connect account linking from a one-time start code.
    /// </summary>
    /// <param name="providerId">The provider id.</param>
    /// <param name="code">The one-time account-linking start code.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An OpenID Connect challenge.</returns>
    [HttpGet("{providerId}/link/launch")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> LaunchLink(
        [FromRoute, Required] string providerId,
        [FromQuery(Name = LinkCodeQueryParameter), Required] string code,
        CancellationToken cancellationToken)
    {
        if (!await IsProviderStartableAsync(providerId).ConfigureAwait(false))
        {
            return NotFound();
        }

        var linkRequest = await _authenticationManager.ConsumeLinkCodeAsync(providerId, code, cancellationToken).ConfigureAwait(false);
        var properties = BuildLinkAuthenticationProperties(providerId, linkRequest.UserId, linkRequest.ReturnUrl);
        return Challenge(properties, AuthenticationSchemes.GetOidcScheme(providerId));
    }

    /// <summary>
    /// Creates an OpenID Connect account-linking start URL for the current user.
    /// </summary>
    /// <param name="providerId">The provider id.</param>
    /// <param name="request">The link start request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The link start URL.</returns>
    [HttpPost("{providerId}/link/start")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<OidcStartResult>> CreateLinkStartUrl(
        [FromRoute, Required] string providerId,
        [FromBody, Required] OidcLinkStartRequest request,
        CancellationToken cancellationToken)
    {
        if (!await IsProviderStartableAsync(providerId).ConfigureAwait(false))
        {
            return NotFound();
        }

        if (!string.IsNullOrWhiteSpace(request.ReturnUrl) && !IsSafeRelativeUrl(request.ReturnUrl))
        {
            return BadRequest("Return URL must be a relative URL.");
        }

        var userId = User.GetUserId();
        if (userId.Equals(Guid.Empty))
        {
            return Unauthorized();
        }

        var code = await _authenticationManager.CreateLinkCodeAsync(providerId, userId, request.ReturnUrl, cancellationToken).ConfigureAwait(false);
        var url = Url.ActionLink(
            nameof(LaunchLink),
            values: new
            {
                providerId,
                code
            });

        return Ok(new OidcStartResult
        {
            Url = url ?? $"/auth/oidc/{providerId}/link/launch?{LinkCodeQueryParameter}={Uri.EscapeDataString(code)}"
        });
    }

    /// <summary>
    /// Completes OpenID Connect account linking for the current user.
    /// </summary>
    /// <param name="providerId">The provider id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The linked external identity or a redirect.</returns>
    [HttpGet("{providerId}/link/complete")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ExternalIdentityDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> CompleteLink([FromRoute, Required] string providerId, CancellationToken cancellationToken)
    {
        if (!await IsProviderStartableAsync(providerId).ConfigureAwait(false))
        {
            return NotFound();
        }

        var provider = _configurationManager.GetEnabledProvider(providerId)!;
        var authenticationResult = await HttpContext.AuthenticateAsync(AuthenticationSchemes.GetOidcExternalCookieScheme(providerId)).ConfigureAwait(false);
        if (!authenticationResult.Succeeded || authenticationResult.Principal is null || authenticationResult.Properties is null)
        {
            return Unauthorized();
        }

        ExternalIdentityDto linkedIdentity;
        var returnUrl = GetProperty(authenticationResult.Properties, OidcConstants.ReturnUrlProperty);
        try
        {
            try
            {
                if (!Guid.TryParse(GetProperty(authenticationResult.Properties, LinkUserIdProperty), out var userId))
                {
                    return Unauthorized();
                }

                var request = BuildExternalIdentityRequest(provider, authenticationResult.Principal, authenticationResult.Properties);
                linkedIdentity = await _authenticationManager.LinkExternalIdentityAsync(userId, request, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (IsOidcLocalFailure(ex) && !string.IsNullOrWhiteSpace(returnUrl) && IsSafeRelativeUrl(returnUrl))
            {
                return LocalRedirect(AppendOidcError(returnUrl, OidcConstants.LocalFailureError));
            }
        }
        finally
        {
            await HttpContext.SignOutAsync(AuthenticationSchemes.GetOidcExternalCookieScheme(providerId)).ConfigureAwait(false);
        }

        if (!string.IsNullOrWhiteSpace(returnUrl))
        {
            if (!IsSafeRelativeUrl(returnUrl))
            {
                return Unauthorized();
            }

            return LocalRedirect(returnUrl);
        }

        return new OkObjectResult(linkedIdentity);
    }

    /// <summary>
    /// Exchanges a one-time OpenID Connect code for a Jellyfin authentication result.
    /// </summary>
    /// <param name="providerId">The provider id.</param>
    /// <param name="request">The exchange request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The Jellyfin authentication result.</returns>
    [HttpPost("{providerId}/exchange")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AuthenticationResult>> Exchange(
        [FromRoute, Required] string providerId,
        [FromBody, Required] OidcExchangeRequest request,
        CancellationToken cancellationToken)
    {
        if (_configurationManager.GetEnabledProvider(providerId) is null)
        {
            return NotFound();
        }

        return Ok(await _authenticationManager.ExchangeCodeAsync(providerId, request.Code, HttpContext.GetNormalizedRemoteIP().ToString(), cancellationToken).ConfigureAwait(false));
    }

    /// <summary>
    /// Gets external identities linked to a user.
    /// </summary>
    /// <param name="userId">The user id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The external identities.</returns>
    [HttpGet("users/{userId}/external-identities")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ExternalIdentityDto>>> GetExternalIdentities(
        [FromRoute, Required] Guid userId,
        CancellationToken cancellationToken)
    {
        if (_userManager.GetUserById(userId) is null)
        {
            return NotFound();
        }

        return Ok(await _authenticationManager.GetExternalIdentitiesAsync(userId, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>
    /// Creates an external identity link for a user.
    /// </summary>
    /// <param name="userId">The user id.</param>
    /// <param name="request">The external identity link request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created external identity.</returns>
    [HttpPost("users/{userId}/external-identities")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExternalIdentityDto>> CreateExternalIdentity(
        [FromRoute, Required] Guid userId,
        [FromBody, Required] OidcExternalIdentityCreateRequest request,
        CancellationToken cancellationToken)
    {
        if (_userManager.GetUserById(userId) is null)
        {
            return NotFound();
        }

        return Ok(await _authenticationManager.CreateExternalIdentityAsync(userId, request, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>
    /// Deletes an external identity link.
    /// </summary>
    /// <param name="userId">The user id.</param>
    /// <param name="identityId">The identity id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A no content response.</returns>
    [HttpDelete("users/{userId}/external-identities/{identityId}")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteExternalIdentity(
        [FromRoute, Required] Guid userId,
        [FromRoute, Required] Guid identityId,
        CancellationToken cancellationToken)
    {
        if (_userManager.GetUserById(userId) is null)
        {
            return NotFound();
        }

        await _authenticationManager.DeleteExternalIdentityAsync(userId, identityId, cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    private AuthenticationProperties BuildAuthenticationProperties(
        string providerId,
        string app,
        string appVersion,
        string deviceId,
        string deviceName,
        string? returnUrl)
    {
        var properties = new AuthenticationProperties
        {
            RedirectUri = GetCompletePath(providerId)
        };

        properties.Items[AppProperty] = app;
        properties.Items[AppVersionProperty] = appVersion;
        properties.Items[DeviceIdProperty] = deviceId;
        properties.Items[DeviceNameProperty] = deviceName;
        if (!string.IsNullOrWhiteSpace(returnUrl))
        {
            properties.Items[OidcConstants.ReturnUrlProperty] = returnUrl;
        }

        return properties;
    }

    private AuthenticationProperties BuildLinkAuthenticationProperties(
        string providerId,
        Guid userId,
        string? returnUrl)
    {
        var properties = new AuthenticationProperties
        {
            RedirectUri = GetLinkCompletePath(providerId)
        };

        properties.Items[LinkUserIdProperty] = userId.ToString("N");
        if (!string.IsNullOrWhiteSpace(returnUrl))
        {
            properties.Items[OidcConstants.ReturnUrlProperty] = returnUrl;
        }

        return properties;
    }

    private OidcExternalIdentityRequest BuildExternalIdentityRequest(
        OidcProviderOptions provider,
        ClaimsPrincipal principal,
        AuthenticationProperties properties)
    {
        return new OidcExternalIdentityRequest
        {
            ProviderId = provider.ProviderId,
            Issuer = GetClaimValue(principal, "iss"),
            Subject = GetClaimValue(principal, "sub"),
            PreferredUsername = principal.FindFirst(provider.UsernameClaim)?.Value,
            Email = principal.FindFirst(provider.EmailClaim)?.Value,
            Groups = principal.FindAll(provider.RoleClaim).Select(claim => claim.Value).ToList(),
            App = GetProperty(properties, AppProperty) ?? string.Empty,
            AppVersion = GetProperty(properties, AppVersionProperty) ?? string.Empty,
            DeviceId = GetProperty(properties, DeviceIdProperty) ?? string.Empty,
            DeviceName = GetProperty(properties, DeviceNameProperty) ?? string.Empty,
            RemoteEndPoint = HttpContext.GetNormalizedRemoteIP().ToString()
        };
    }

    private async Task<bool> IsProviderStartableAsync(string providerId)
    {
        return _configurationManager.GetEnabledProvider(providerId) is not null
               && await AreProviderSchemesRegisteredAsync(providerId).ConfigureAwait(false);
    }

    private async Task<bool> AreProviderSchemesRegisteredAsync(string providerId)
    {
        return await _schemeProvider.GetSchemeAsync(AuthenticationSchemes.GetOidcScheme(providerId)).ConfigureAwait(false) is not null
               && await _schemeProvider.GetSchemeAsync(AuthenticationSchemes.GetOidcExternalCookieScheme(providerId)).ConfigureAwait(false) is not null;
    }

    private string GetProviderCallbackUri(string providerId)
    {
        return UriHelper.BuildAbsolute(
            Request.Scheme,
            Request.Host,
            Request.PathBase,
            new PathString(AuthenticationSchemes.GetOidcCallbackPath(providerId)));
    }

    private string GetCompletePath(string providerId)
    {
        return Url.Action(nameof(Complete), values: new { providerId }) ?? $"/auth/oidc/{providerId}/complete";
    }

    private static string BuildStartPath(string providerId, OidcStartRequest request)
    {
        var query = new Dictionary<string, string?>
        {
            ["app"] = request.App,
            ["appVersion"] = request.AppVersion,
            ["deviceId"] = request.DeviceId,
            ["deviceName"] = request.DeviceName,
            ["returnUrl"] = request.ReturnUrl
        };

        return QueryHelpers.AddQueryString($"/auth/oidc/{providerId}/start", query);
    }

    private string GetLinkCompletePath(string providerId)
    {
        return Url.Action(nameof(CompleteLink), values: new { providerId }) ?? $"/auth/oidc/{providerId}/link/complete";
    }

    private void AddProviderRedirectUris(OidcConfigurationDto configuration)
    {
        foreach (var provider in configuration.Providers)
        {
            if (!string.IsNullOrWhiteSpace(provider.ProviderId))
            {
                provider.RedirectUri = GetProviderCallbackUri(provider.ProviderId);
            }
        }
    }

    private void ValidateStart(
        string app,
        string appVersion,
        string deviceId,
        string deviceName,
        string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(app) || string.IsNullOrWhiteSpace(appVersion) || string.IsNullOrWhiteSpace(deviceId) || string.IsNullOrWhiteSpace(deviceName))
        {
            throw new ArgumentException("App, app version, device id, and device name are required.");
        }

        if (!string.IsNullOrWhiteSpace(returnUrl) && !IsSafeRelativeUrl(returnUrl))
        {
            throw new ArgumentException("Return URL must be a relative URL.", nameof(returnUrl));
        }
    }

    private static string GetClaimValue(ClaimsPrincipal principal, string claimType)
    {
        return principal.FindFirst(claimType)?.Value ?? string.Empty;
    }

    private static string AppendExchangeCode(string returnUrl, string code)
    {
        return QueryHelpers.AddQueryString(returnUrl, ExchangeCodeQueryParameter, code);
    }

    private static string AppendOidcError(string returnUrl, string error)
    {
        return QueryHelpers.AddQueryString(returnUrl, OidcConstants.ErrorQueryParameter, error);
    }

    private static bool IsOidcLocalFailure(Exception ex)
    {
        return ex is ResourceNotFoundException
            or MediaBrowser.Controller.Net.SecurityException
            or ArgumentException;
    }

    private bool IsSafeRelativeUrl(string url)
    {
        return OidcConstants.IsSafeRelativeUrl(url);
    }

    private static string? GetProperty(AuthenticationProperties properties, string key)
    {
        return properties.Items.TryGetValue(key, out var value) ? value : null;
    }
}
