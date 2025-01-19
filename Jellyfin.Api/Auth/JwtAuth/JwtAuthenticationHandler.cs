using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Jellyfin.Api.Constants;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Jellyfin.Api.Auth.JwtAuth
{
    /// <summary>
    /// JWT authentication handler.
    /// </summary>
    public class JwtAuthenticationHandler : AuthenticationHandler<JwtBearerOptions>
    {
        private readonly JwtOptions _jwtOptions;
        private readonly ILogger<JwtAuthenticationHandler> _logger;
        private ConfigurationManager<OpenIdConnectConfiguration>? _configurationManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="JwtAuthenticationHandler"/> class.
        /// </summary>
        /// <param name="jwtOptions">JWT options.</param>
        /// <param name="options">The options.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="encoder">The encoder.</param>
        public JwtAuthenticationHandler(
            IOptions<JwtOptions> jwtOptions,
            IOptionsMonitor<JwtBearerOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
            _jwtOptions = jwtOptions.Value;
            _logger = logger.CreateLogger<JwtAuthenticationHandler>();
        }

        /// <inheritdoc />
        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            try
            {
                if (!_jwtOptions.IsEnabled || string.IsNullOrEmpty(_jwtOptions.JwksUrl))
                {
                    return AuthenticateResult.NoResult();
                }

                var token = Context.Request.Headers[_jwtOptions.AuthorizationHeader].ToString().Replace(_jwtOptions.BearerTokenPrefix, string.Empty);
                if (string.IsNullOrEmpty(token))
                {
                    return AuthenticateResult.NoResult();
                }

                if (_configurationManager == null)
                {
                    _configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                        _jwtOptions.JwksUrl,
                        new OpenIdConnectConfigurationRetriever());
                }

                var config = await _configurationManager.GetConfigurationAsync(Context.RequestAborted).ConfigureAwait(false);

                var validationParameters = _jwtOptions.ValidationParameters ?? new TokenValidationParameters();
                validationParameters.ValidIssuer = _jwtOptions.Issuer;
                validationParameters.ValidAudience = _jwtOptions.Audience;
                validationParameters.IssuerSigningKeys = config.SigningKeys;

                var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                var claimsPrincipal = tokenHandler.ValidateToken(token, validationParameters, out _);

                var claims = new List<Claim>();
                var name = claimsPrincipal.FindFirst(_jwtOptions.ClaimMappings.NameClaimType)?.Value;
                var userId = claimsPrincipal.FindFirst(_jwtOptions.ClaimMappings.UserIdClaimType)?.Value;
                var roles = claimsPrincipal.FindAll(_jwtOptions.ClaimMappings.RolesClaimType);

                if (!string.IsNullOrEmpty(name))
                {
                    claims.Add(new Claim(ClaimTypes.Name, name));
                }

                if (!string.IsNullOrEmpty(userId))
                {
                    claims.Add(new Claim(InternalClaimTypes.UserId, userId));
                }

                var isAdmin = false;
                foreach (var role in roles)
                {
                    if (role.Value.Equals(_jwtOptions.ClaimMappings.AdminRole, StringComparison.OrdinalIgnoreCase))
                    {
                        isAdmin = true;
                        break;
                    }
                }

                claims.Add(new Claim(ClaimTypes.Role, isAdmin ? UserRoles.Administrator : UserRoles.User));

                var identity = new ClaimsIdentity(claims, Scheme.Name);
                var principal = new ClaimsPrincipal(identity);
                var ticket = new AuthenticationTicket(principal, Scheme.Name);

                return AuthenticateResult.Success(ticket);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error authenticating JWT token");
                return AuthenticateResult.Fail(ex);
            }
        }
    }
}
