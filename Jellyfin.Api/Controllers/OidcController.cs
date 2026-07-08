using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Constants;
using Jellyfin.Api.Extensions;
using MediaBrowser.Common.Api;
using MediaBrowser.Controller.Authentication;
using MediaBrowser.Model.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;

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
    private const string ReturnUrlProperty = "jellyfin:returnUrl";
    private readonly IOidcConfigurationManager _configurationManager;
    private readonly IOidcAuthenticationManager _authenticationManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="OidcController"/> class.
    /// </summary>
    /// <param name="configurationManager">The OIDC configuration manager.</param>
    /// <param name="authenticationManager">The OIDC authentication manager.</param>
    public OidcController(
        IOidcConfigurationManager configurationManager,
        IOidcAuthenticationManager authenticationManager)
    {
        _configurationManager = configurationManager;
        _authenticationManager = authenticationManager;
    }

    /// <summary>
    /// Gets enabled OpenID Connect providers.
    /// </summary>
    /// <response code="200">Providers returned.</response>
    /// <returns>The enabled providers.</returns>
    [HttpGet("providers")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<OidcProviderInfo>> GetProviders()
    {
        return Ok(_configurationManager.GetProviderInfos());
    }

    /// <summary>
    /// Gets the OpenID Connect configuration.
    /// </summary>
    /// <response code="200">Configuration returned.</response>
    /// <returns>The secret-safe configuration.</returns>
    [HttpGet("configuration")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<OidcConfigurationDto> GetConfiguration()
    {
        return Ok(_configurationManager.GetConfiguration());
    }

    /// <summary>
    /// Updates the OpenID Connect configuration.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <response code="204">Configuration updated.</response>
    /// <response code="400">Configuration is invalid.</response>
    /// <returns>A no content response.</returns>
    [HttpPost("configuration")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> UpdateConfiguration(
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

        return NoContent();
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
    public ActionResult Start(
        [FromRoute, Required] string providerId,
        [FromQuery, Required] string app,
        [FromQuery, Required] string appVersion,
        [FromQuery, Required] string deviceId,
        [FromQuery, Required] string deviceName,
        [FromQuery] string? returnUrl)
    {
        ValidateStart(providerId, app, appVersion, deviceId, deviceName, returnUrl);
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
    public ActionResult<OidcStartResult> CreateStartUrl(
        [FromRoute, Required] string providerId,
        [FromBody, Required] OidcStartRequest request)
    {
        ValidateStart(providerId, request.App, request.AppVersion, request.DeviceId, request.DeviceName, request.ReturnUrl);

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
            Url = url ?? string.Empty,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10)
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
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> Complete([FromRoute, Required] string providerId, CancellationToken cancellationToken)
    {
        var provider = _configurationManager.GetEnabledProvider(providerId);
        if (provider is null)
        {
            return NotFound();
        }

        var authenticationResult = await HttpContext.AuthenticateAsync(AuthenticationSchemes.GetOidcExternalCookieScheme(providerId)).ConfigureAwait(false);
        if (!authenticationResult.Succeeded || authenticationResult.Principal is null || authenticationResult.Properties is null)
        {
            return Unauthorized();
        }

        var request = BuildExternalIdentityRequest(provider, authenticationResult.Principal, authenticationResult.Properties);
        var code = await _authenticationManager.CompleteSignInAsync(request, cancellationToken).ConfigureAwait(false);
        await HttpContext.SignOutAsync(AuthenticationSchemes.GetOidcExternalCookieScheme(providerId)).ConfigureAwait(false);

        var returnUrl = GetProperty(authenticationResult.Properties, ReturnUrlProperty);
        if (!string.IsNullOrWhiteSpace(returnUrl))
        {
            return Redirect(AppendExchangeCode(returnUrl, code));
        }

        return new OkObjectResult(new OidcExchangeRequest
        {
            Code = code
        });
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
    public async Task<ActionResult<AuthenticationResult>> Exchange(
        [FromRoute, Required] string providerId,
        [FromBody, Required] OidcExchangeRequest request,
        CancellationToken cancellationToken)
    {
        if (_configurationManager.GetEnabledProvider(providerId) is null)
        {
            return NotFound();
        }

        return Ok(await _authenticationManager.ExchangeCodeAsync(request.Code, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>
    /// Logs out a Jellyfin session and returns provider logout information when available.
    /// </summary>
    /// <param name="request">The logout request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The logout result.</returns>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<OidcLogoutResult>> Logout(
        [FromBody] OidcLogoutRequest? request,
        CancellationToken cancellationToken)
    {
        var postLogoutRedirectUri = BuildPostLogoutRedirectUri(request?.ReturnUrl);
        var accessToken = User.GetToken() ?? string.Empty;
        var result = await _authenticationManager.LogoutAsync(accessToken, postLogoutRedirectUri, cancellationToken).ConfigureAwait(false);
        return Ok(result);
    }

    /// <summary>
    /// Gets external identities linked to a user.
    /// </summary>
    /// <param name="userId">The user id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The external identities.</returns>
    [HttpGet("/Users/{userId}/ExternalIdentities")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ExternalIdentityDto>>> GetExternalIdentities(
        [FromRoute, Required] Guid userId,
        CancellationToken cancellationToken)
    {
        return Ok(await _authenticationManager.GetExternalIdentitiesAsync(userId, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>
    /// Deletes an external identity link.
    /// </summary>
    /// <param name="userId">The user id.</param>
    /// <param name="identityId">The identity id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A no content response.</returns>
    [HttpDelete("/Users/{userId}/ExternalIdentities/{identityId}")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> DeleteExternalIdentity(
        [FromRoute, Required] Guid userId,
        [FromRoute, Required] Guid identityId,
        CancellationToken cancellationToken)
    {
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
            RedirectUri = Url.ActionLink(nameof(Complete), values: new { providerId }) ?? $"/auth/oidc/{providerId}/complete"
        };

        properties.Items[AppProperty] = app;
        properties.Items[AppVersionProperty] = appVersion;
        properties.Items[DeviceIdProperty] = deviceId;
        properties.Items[DeviceNameProperty] = deviceName;
        if (!string.IsNullOrWhiteSpace(returnUrl))
        {
            properties.Items[ReturnUrlProperty] = returnUrl;
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
            Issuer = GetRequiredClaim(principal, "iss"),
            Subject = GetRequiredClaim(principal, "sub"),
            PreferredUsername = principal.FindFirst(provider.UsernameClaim)?.Value,
            Email = principal.FindFirst(provider.EmailClaim)?.Value,
            Groups = principal.FindAll(provider.RoleClaim).Select(claim => claim.Value).ToList(),
            SessionId = principal.FindFirst("sid")?.Value,
            IdTokenHint = GetProperty(properties, AuthenticationSchemes.OidcIdTokenProperty),
            App = GetProperty(properties, AppProperty) ?? string.Empty,
            AppVersion = GetProperty(properties, AppVersionProperty) ?? string.Empty,
            DeviceId = GetProperty(properties, DeviceIdProperty) ?? string.Empty,
            DeviceName = GetProperty(properties, DeviceNameProperty) ?? string.Empty,
            RemoteEndPoint = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty
        };
    }

    private void ValidateStart(
        string providerId,
        string app,
        string appVersion,
        string deviceId,
        string deviceName,
        string? returnUrl)
    {
        if (_configurationManager.GetEnabledProvider(providerId) is null)
        {
            throw new ArgumentException("OpenID Connect provider is not enabled.", nameof(providerId));
        }

        if (string.IsNullOrWhiteSpace(app) || string.IsNullOrWhiteSpace(appVersion) || string.IsNullOrWhiteSpace(deviceId) || string.IsNullOrWhiteSpace(deviceName))
        {
            throw new ArgumentException("App, app version, device id, and device name are required.");
        }

        if (!string.IsNullOrWhiteSpace(returnUrl) && !IsSafeRelativeUrl(returnUrl))
        {
            throw new ArgumentException("Return URL must be a relative URL.", nameof(returnUrl));
        }
    }

    private string? BuildPostLogoutRedirectUri(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return null;
        }

        if (!IsSafeRelativeUrl(returnUrl))
        {
            throw new ArgumentException("Return URL must be a relative URL.", nameof(returnUrl));
        }

        return UriHelper.BuildAbsolute(Request.Scheme, Request.Host, Request.PathBase, returnUrl);
    }

    private static string GetRequiredClaim(ClaimsPrincipal principal, string claimType)
    {
        return principal.FindFirst(claimType)?.Value
               ?? throw new ArgumentException("OpenID Connect identity is missing claim '" + claimType + "'.");
    }

    private static string AppendExchangeCode(string returnUrl, string code)
    {
        var separator = returnUrl.Contains("?", StringComparison.Ordinal) ? '&' : '?';
        return returnUrl + separator + "oidc_code=" + Uri.EscapeDataString(code);
    }

    private static bool IsSafeRelativeUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Relative, out _) && url.Length > 0 && url[0] == '/' && !url.StartsWith("//", StringComparison.Ordinal);
    }

    private static string? GetProperty(AuthenticationProperties properties, string key)
    {
        return properties.Items.TryGetValue(key, out var value) ? value : null;
    }
}
