using System;
using Microsoft.AspNetCore.Cors.Infrastructure;

namespace Jellyfin.Server.Models
{
    /// <summary>
    /// Server Cors Policy.
    /// </summary>
    public class ServerCorsPolicy
    {
        /// <summary>
        /// Default policy name.
        /// </summary>
        public const string DefaultPolicyName = nameof(ServerCorsPolicy);

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerCorsPolicy"/> class.
        /// </summary>
        /// <param name="corsHosts">The configured cors hosts.</param>
        public ServerCorsPolicy(string[] corsHosts)
        {
            var builder = new CorsPolicyBuilder()
                .AllowAnyMethod()
                .AllowAnyHeader();

            // No hosts configured or only default configured.
            if (corsHosts.Length == 0
                || (corsHosts.Length == 1
                    && string.Equals(corsHosts[0], "*", StringComparison.Ordinal)))
            {
                builder.AllowAnyOrigin();
            }
            else
            {
                builder.WithOrigins(corsHosts)
                    .AllowCredentials();
            }

            Policy = builder.Build();
        }

        /// <summary>
        /// Gets the cors policy.
        /// </summary>
        public CorsPolicy Policy { get; }
    }
}
