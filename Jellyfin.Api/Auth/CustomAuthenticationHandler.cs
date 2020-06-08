using System.Security.Authentication;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using System.Web;
using Jellyfin.Api.Constants;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace Jellyfin.Api.Auth
{
    /// <summary>
    /// Custom authentication handler wrapping the legacy authentication.
    /// </summary>
    public class CustomAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly IAuthService _authService;
        private readonly IAuthenticationRepository _authRepo;
        private readonly IUserManager _userManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomAuthenticationHandler" /> class.
        /// </summary>
        /// <param name="authService">The jellyfin authentication service.</param>
        /// <param name="options">Options monitor.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="encoder">The url encoder.</param>
        /// <param name="clock">The system clock.</param>
        /// <param name="authRepo">The auth repo.</param>
        /// <param name="userManager">The user manager.</param>
        public CustomAuthenticationHandler(
            IAuthService authService,
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock,
            IAuthenticationRepository authRepo,
            IUserManager userManager) : base(options, logger, encoder, clock)
        {
            _authService = authService;
            _authRepo = authRepo;
            _userManager = userManager;
        }

        /// <inheritdoc />
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var authenticatedAttribute = new AuthenticatedAttribute
            {
                IgnoreLegacyAuth = true
            };

            try
            {
                var user = _authService.Authenticate(Request, authenticatedAttribute);
                if (user == null)
                {
                    return Task.FromResult(AuthenticateResult.NoResult());
                    // TODO return when legacy API is removed.
                    // Don't spam the log with "Invalid User"
                    // return Task.FromResult(AuthenticateResult.Fail("Invalid user"));
                }

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(
                        ClaimTypes.Role,
                        value: user.HasPermission(PermissionKind.IsAdministrator) ? UserRoles.Administrator : UserRoles.User)
                };

                // Load properties from authentication
                var authClaims = GetAuthenticationClaims();
                var tokenClaims = GetTokenClaims(authClaims);
                if (tokenClaims != null)
                {
                    claims.Add(new Claim(InternalClaimTypes.UserId, tokenClaims.UserId.ToString("N", CultureInfo.InvariantCulture)));
                    claims.Add(new Claim(InternalClaimTypes.DeviceId, tokenClaims.DeviceId));
                    claims.Add(new Claim(InternalClaimTypes.Device, tokenClaims.Device));
                    claims.Add(new Claim(InternalClaimTypes.Client, tokenClaims.Client));
                    claims.Add(new Claim(InternalClaimTypes.Version, tokenClaims.Version));
                    claims.Add(new Claim(InternalClaimTypes.Token, tokenClaims.Token));
                }

                var identity = new ClaimsIdentity(claims, Scheme.Name);
                var principal = new ClaimsPrincipal(identity);
                var ticket = new AuthenticationTicket(principal, Scheme.Name);

                return Task.FromResult(AuthenticateResult.Success(ticket));
            }
            catch (AuthenticationException ex)
            {
                return Task.FromResult(AuthenticateResult.Fail(ex));
            }
            catch (SecurityException ex)
            {
                return Task.FromResult(AuthenticateResult.Fail(ex));
            }
        }

        private Dictionary<string, string>? GetAuthenticationClaims()
        {
            const string jellyfinAuthHeader = "X-Jellyfin-Authorization";
            const string embyAuthHeader = "X-Emby-Authorization";
            if (!Request.Headers.TryGetValue(jellyfinAuthHeader, out var auth)
                || !Request.Headers.TryGetValue(embyAuthHeader, out auth)
                || !Request.Headers.TryGetValue(HeaderNames.Authorization, out auth))
            {
                return null;
            }

            // Take first auth auth header.
            var authHeaderValue = auth[0];
            var parts = authHeaderValue.Split(new[] { ' ' }, 2);
            if (parts.Length != 2)
            {
                return null;
            }

            var acceptedNames = new[] { "Jellyfin", "MediaBrowser", "Emby" };

            // It has to be a digest request
            if (!acceptedNames.Contains(parts[0], StringComparer.OrdinalIgnoreCase))
            {
                return null;
            }

            // Remove until the first space
            authHeaderValue = parts[1];
            parts = authHeaderValue.Split(',');

            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in parts)
            {
                var param = item.Trim().Split(new[] { '=' }, 2);

                if (param.Length == 2)
                {
                    var value = NormalizeValue(param[1].Trim(new[] { '"' }));
                    result.Add(param[0], value);
                }
            }

            return result;
        }

        private AuthorizationInfo? GetTokenClaims(Dictionary<string, string>? authenticationClaims)
        {
            string? deviceId = null;
            string? device = null;
            string? client = null;
            string? version = null;
            string? token = null;
            authenticationClaims?.TryGetValue("DeviceId", out deviceId);
            authenticationClaims?.TryGetValue("Device", out device);
            authenticationClaims?.TryGetValue("Client", out client);
            authenticationClaims?.TryGetValue("Version", out version);
            authenticationClaims?.TryGetValue("Token", out token);

            var info = new AuthorizationInfo
            {
                Client = client,
                Device = device,
                DeviceId = deviceId,
                Version = version,
                Token = token
            };

            if (string.IsNullOrEmpty(token))
            {
                Request.Headers.TryGetValue("X-Jellyfin-Token", out var value);
                token = value;
            }

            if (string.IsNullOrEmpty(token))
            {
                Request.Headers.TryGetValue("X-Emby-Token", out var value);
                token = value;
            }

            if (string.IsNullOrEmpty(token))
            {
                Request.Headers.TryGetValue("X-MediaBrowser-Token", out var value);
                token = value;
            }

            if (string.IsNullOrEmpty(token))
            {
                Request.Query.TryGetValue("api_key", out var value);
                token = value;
            }

            var result = _authRepo.Get(new AuthenticationInfoQuery { AccessToken = token });
            var tokenInfo = result.Items.Count > 0 ? result.Items[0] : null;
            if (tokenInfo == null)
            {
                return null;
            }

            var updateToken = false;

            // TODO: Remove these checks for IsNullOrWhiteSpace
            if (string.IsNullOrWhiteSpace(info.Client))
            {
                info.Client = tokenInfo.AppName;
            }

            if (string.IsNullOrWhiteSpace(info.DeviceId))
            {
                info.DeviceId = tokenInfo.DeviceId;
            }

            // Temporary. TODO - allow clients to specify that the token has been shared with a casting device
            var allowTokenInfoUpdate = info.Client == null || info.Client.IndexOf("chromecast", StringComparison.OrdinalIgnoreCase) == -1;

            if (string.IsNullOrWhiteSpace(info.Device))
            {
                info.Device = tokenInfo.DeviceName;
            }
            else if (!string.Equals(info.Device, tokenInfo.DeviceName, StringComparison.OrdinalIgnoreCase))
            {
                if (allowTokenInfoUpdate)
                {
                    updateToken = true;
                    tokenInfo.DeviceName = info.Device;
                }
            }

            if (string.IsNullOrWhiteSpace(info.Version))
            {
                info.Version = tokenInfo.AppVersion;
            }
            else if (!string.Equals(info.Version, tokenInfo.AppVersion, StringComparison.OrdinalIgnoreCase))
            {
                if (allowTokenInfoUpdate)
                {
                    updateToken = true;
                    tokenInfo.AppVersion = info.Version;
                }
            }

            if ((DateTime.UtcNow - tokenInfo.DateLastActivity).TotalMinutes > 3)
            {
                tokenInfo.DateLastActivity = DateTime.UtcNow;
                updateToken = true;
            }

            if (!tokenInfo.UserId.Equals(Guid.Empty))
            {
                info.User = _userManager.GetUserById(tokenInfo.UserId);

                if (info.User != null &&
                    !string.Equals(info.User.Name, tokenInfo.UserName, StringComparison.OrdinalIgnoreCase))
                {
                    tokenInfo.UserName = info.User.Name;
                    updateToken = true;
                }
            }

            if (updateToken)
            {
                _authRepo.Update(tokenInfo);
            }

            return info;
        }

        private static string NormalizeValue(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            return HttpUtility.HtmlEncode(value);
        }
    }
}
