using Microsoft.AspNetCore.Cors.Infrastructure;

namespace Jellyfin.Server.Models
{
    /// <summary>
    /// Server Cors Policy.
    /// </summary>
    public static class ServerCorsPolicy
    {
        /// <summary>
        /// Default policy name.
        /// </summary>
        public const string DefaultPolicyName = "DefaultCorsPolicy";

        /// <summary>
        /// Default Policy. Allow Everything.
        /// </summary>
        public static readonly CorsPolicy DefaultPolicy = new CorsPolicy
        {
            // Allow any origin
            Origins = { "*" },

            // Allow any method
            Methods = { "*" },

            // Allow any header
            Headers = { "*" }
        };
    }
}