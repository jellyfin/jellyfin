namespace Jellyfin.Api.Auth.JwtAuth
{
    /// <summary>
    /// JWT authentication configuration options.
    /// </summary>
    public class JwtAuthenticationOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether JWT authentication is enabled.
        /// </summary>
        public bool Enable { get; set; }

        /// <summary>
        /// Gets or sets the JWKS endpoint URL.
        /// </summary>
        public string? JwksUrl { get; set; }

        /// <summary>
        /// Gets or sets the authorization header name.
        /// </summary>
        public string AuthorizationHeader { get; set; } = "Authorization";

        /// <summary>
        /// Gets or sets the bearer token prefix.
        /// </summary>
        public string BearerTokenPrefix { get; set; } = "Bearer ";

        /// <summary>
        /// Gets or sets the expected audience.
        /// </summary>
        public string? Audience { get; set; }

        /// <summary>
        /// Gets or sets the issuer.
        /// </summary>
        public string? Issuer { get; set; }

        /// <summary>
        /// Gets or sets the claim mappings.
        /// </summary>
        public JwtClaimMappingOptions ClaimMappings { get; set; } = new JwtClaimMappingOptions();
    }

    /// <summary>
    /// JWT claim mapping configuration options.
    /// </summary>
    public class JwtClaimMappingOptions
    {
        /// <summary>
        /// Gets or sets the name claim mapping.
        /// </summary>
        public string NameClaimType { get; set; } = "name";

        /// <summary>
        /// Gets or sets the user ID claim mapping.
        /// </summary>
        public string UserIdClaimType { get; set; } = "sub";

        /// <summary>
        /// Gets or sets the admin role value.
        /// </summary>
        public string AdminRole { get; set; } = "admin";

        /// <summary>
        /// Gets or sets the roles claim type.
        /// </summary>
        public string RolesClaimType { get; set; } = "roles";
    }
}
