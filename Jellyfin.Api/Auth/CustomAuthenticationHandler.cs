using System;
using System.Globalization;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Jellyfin.Api.Attributes;
using Jellyfin.Api.Constants;
using Jellyfin.Data;
using Jellyfin.Database.Implementations.Enums;
using MediaBrowser.Controller.Authentication;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.ShareLinks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Jellyfin.Api.Auth
{
    /// <summary>
    /// Custom authentication handler wrapping the legacy authentication.
    /// </summary>
    public class CustomAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private const string ShareLinkAuthenticationType = "ShareLink";
        private const string ShareLinkTokenQueryParameter = "share_token";
        private const string ShareLinkErrorHeader = "X-ShareLink-Error";

        private readonly IAuthService _authService;
        private readonly IShareLinkTokenService _shareLinkTokenService;
        private readonly ILogger<CustomAuthenticationHandler> _logger;

        /// <summary>
        /// Route value names that bind a media item on endpoints a share token may reach.
        /// </summary>
        private static readonly string[] RouteItemIdKeys = ["itemId", "routeItemId"];

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomAuthenticationHandler" /> class.
        /// </summary>
        /// <param name="authService">The jellyfin authentication service.</param>
        /// <param name="shareLinkTokenService">The share link token service.</param>
        /// <param name="options">Options monitor.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="encoder">The url encoder.</param>
        public CustomAuthenticationHandler(
            IAuthService authService,
            IShareLinkTokenService shareLinkTokenService,
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
            _authService = authService;
            _shareLinkTokenService = shareLinkTokenService;
            _logger = logger.CreateLogger<CustomAuthenticationHandler>();
        }

        /// <inheritdoc />
        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            try
            {
                var authorizationInfo = await _authService.Authenticate(Request).ConfigureAwait(false);

                // Normal credentials always win: when a request carries both valid normal
                // credentials and a share_token, the share_token is ignored on purpose.
                if (authorizationInfo.HasToken)
                {
                    var role = UserRoles.User;
                    if (authorizationInfo.IsApiKey
                        || (authorizationInfo.User?.HasPermission(PermissionKind.IsAdministrator) ?? false))
                    {
                        role = UserRoles.Administrator;
                    }

                    var claims = new[]
                    {
                        new Claim(ClaimTypes.Name, authorizationInfo.User?.Username ?? string.Empty),
                        new Claim(ClaimTypes.Role, role),
                        new Claim(InternalClaimTypes.UserId, authorizationInfo.UserId.ToString("N", CultureInfo.InvariantCulture)),
                        new Claim(InternalClaimTypes.DeviceId, authorizationInfo.DeviceId ?? string.Empty),
                        new Claim(InternalClaimTypes.Device, authorizationInfo.Device ?? string.Empty),
                        new Claim(InternalClaimTypes.Client, authorizationInfo.Client ?? string.Empty),
                        new Claim(InternalClaimTypes.Version, authorizationInfo.Version ?? string.Empty),
                        new Claim(InternalClaimTypes.Token, authorizationInfo.Token),
                        new Claim(InternalClaimTypes.IsApiKey, authorizationInfo.IsApiKey.ToString(CultureInfo.InvariantCulture))
                    };

                    var identity = new ClaimsIdentity(claims, Scheme.Name);
                    var principal = new ClaimsPrincipal(identity);
                    var ticket = new AuthenticationTicket(principal, Scheme.Name);

                    return AuthenticateResult.Success(ticket);
                }

                var shareToken = Request.Query[ShareLinkTokenQueryParameter].ToString();
                if (!string.IsNullOrEmpty(shareToken))
                {
                    return await AuthenticateShareTokenAsync(shareToken).ConfigureAwait(false);
                }

                return AuthenticateResult.NoResult();
            }
            catch (AuthenticationException ex)
            {
                _logger.LogDebug(ex, "Error authenticating with {Handler}", nameof(CustomAuthenticationHandler));
                return AuthenticateResult.NoResult();
            }
            catch (SecurityException ex)
            {
                return AuthenticateResult.Fail(ex);
            }
        }

        /// <inheritdoc />
        protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            // Share-token validation failures surface as 403 + X-ShareLink-Error (ADR decision 9),
            // not as a 401 challenge.
            if (Response.Headers.ContainsKey(ShareLinkErrorHeader))
            {
                Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }

            await base.HandleChallengeAsync(properties).ConfigureAwait(false);
        }

        private async Task<AuthenticateResult> AuthenticateShareTokenAsync(string shareToken)
        {
            // A share token is only a credential on endpoints explicitly marked as share-link
            // accessible; everywhere else the request must fall back to normal auth (401).
            if (Context.GetEndpoint()?.Metadata.GetMetadata<ShareLinkAccessAttribute>() is null)
            {
                return AuthenticateResult.NoResult();
            }

            var result = await _shareLinkTokenService
                .ValidateAsync(shareToken, GetRouteItemId(), Context.RequestAborted)
                .ConfigureAwait(false);

            if (!result.IsValid)
            {
                var errorCode = result.ErrorCode?.ToString() ?? ShareLinkErrorCode.DecryptFailed.ToString();
                Response.Headers[ShareLinkErrorHeader] = errorCode;
                return AuthenticateResult.Fail($"Share link validation failed: {errorCode}");
            }

            var userId = result.UserId.ToString("N", CultureInfo.InvariantCulture);
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, UserRoles.User),
                new Claim(InternalClaimTypes.UserId, userId),
                new Claim(InternalClaimTypes.Token, shareToken),
                new Claim(InternalClaimTypes.IsShareLink, bool.TrueString),
                new Claim(InternalClaimTypes.ShareItemId, result.ItemId.ToString("N", CultureInfo.InvariantCulture)),
                new Claim(InternalClaimTypes.ShareJti, result.Jti ?? string.Empty),
                new Claim(InternalClaimTypes.ShareExpiresAt, result.ExpiresAt?.ToString(CultureInfo.InvariantCulture) ?? string.Empty),
                new Claim(InternalClaimTypes.ShareDurationSeconds, result.DurationSeconds?.ToString(CultureInfo.InvariantCulture) ?? string.Empty)
            };

            var identity = new ClaimsIdentity(claims, ShareLinkAuthenticationType);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return AuthenticateResult.Success(ticket);
        }

        /// <summary>
        /// Resolves the media item a marked endpoint operates on.
        /// </summary>
        /// <remarks>
        /// Endpoints bind the item under different route parameter names ("itemId" on most
        /// controllers, "routeItemId" on the subtitle routes). Every name is inspected so a
        /// share token cannot slip past item binding by hitting a route that happens to use a
        /// different parameter name. Returns null when no bound item could be resolved, which
        /// callers must treat as a denial.
        /// </remarks>
        /// <returns>The bound item id, or <c>null</c> if the route carries no item id.</returns>
        internal Guid? GetRouteItemId()
        {
            foreach (var name in RouteItemIdKeys)
            {
                if (Request.RouteValues.TryGetValue(name, out var value)
                    && Guid.TryParse(value?.ToString(), out var itemId))
                {
                    return itemId;
                }
            }

            return null;
        }
    }
}
