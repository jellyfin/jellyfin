using System;
using Microsoft.IdentityModel.Tokens;

namespace Jellyfin.Api.Auth.JwtAuth
{
    /// <summary>
    /// JWT runtime options.
    /// </summary>
    public class JwtOptions
    {
        private readonly JwtAuthenticationOptions _config;

        /// <summary>
        /// Initializes a new instance of the <see cref="JwtOptions"/> class.
        /// </summary>
        /// <param name="config">The JWT authentication configuration.</param>
        public JwtOptions(JwtAuthenticationOptions config)
        {
            _config = config;
        }

        /// <summary>
        /// Gets a value indicating whether JWT authentication is enabled.
        /// </summary>
        public bool IsEnabled => _config.Enable;

        /// <summary>
        /// Gets the JWKS endpoint URL.
        /// </summary>
        public string? JwksUrl => _config.JwksUrl;

        /// <summary>
        /// Gets the authorization header name.
        /// </summary>
        public string AuthorizationHeader => _config.AuthorizationHeader;

        /// <summary>
        /// Gets the bearer token prefix.
        /// </summary>
        public string BearerTokenPrefix => _config.BearerTokenPrefix;

        /// <summary>
        /// Gets the expected audience.
        /// </summary>
        public string? Audience => _config.Audience;

        /// <summary>
        /// Gets the issuer.
        /// </summary>
        public string? Issuer => _config.Issuer;

        /// <summary>
        /// Gets the claim mappings.
        /// </summary>
        public JwtClaimMappings ClaimMappings { get; } = new JwtClaimMappings();

        /// <summary>
        /// Gets or sets the token validation parameters.
        /// </summary>
        public TokenValidationParameters? ValidationParameters { get; set; }
    }

    /// <summary>
    /// JWT claim mapping configuration.
    /// </summary>
    public class JwtClaimMappings
    {
        private readonly JwtClaimMappingOptions _config;

        /// <summary>
        /// Initializes a new instance of the <see cref="JwtClaimMappings"/> class.
        /// </summary>
        /// <param name="config">The claim mapping configuration.</param>
        public JwtClaimMappings(JwtClaimMappingOptions config)
        {
            _config = config;
        }

        /// <summary>
        /// Gets the name claim mapping.
        /// </summary>
        public string NameClaimType => _config.NameClaimType;

        /// <summary>
        /// Gets the user ID claim mapping.
        /// </summary>
        public string UserIdClaimType => _config.UserIdClaimType;

        /// <summary>
        /// Gets the admin role value.
        /// </summary>
        public string AdminRole => _config.AdminRole;

        /// <summary>
        /// Gets the roles claim type.
        /// </summary>
        public string RolesClaimType => _config.RolesClaimType;
    }
}
