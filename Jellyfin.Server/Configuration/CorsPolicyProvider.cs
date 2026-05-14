using System;
using System.Threading.Tasks;
using MediaBrowser.Model.Configuration;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Jellyfin.Server.Configuration
{
    /// <summary>
    /// Cors policy provider.
    /// </summary>
    public class CorsPolicyProvider : ICorsPolicyProvider
    {
        private readonly IOptions<ServerConfiguration> _serverConfig;

        /// <summary>
        /// Initializes a new instance of the <see cref="CorsPolicyProvider"/> class.
        /// </summary>
        /// <param name="serverConfiguration">Instance of the server configuration.</param>
        public CorsPolicyProvider(IOptions<ServerConfiguration> serverConfiguration)
        {
            _serverConfig = serverConfiguration;
        }

        /// <inheritdoc />
        public Task<CorsPolicy?> GetPolicyAsync(HttpContext context, string? policyName)
        {
            var corsHosts = _serverConfig.Value.CorsHosts;
            var builder = new CorsPolicyBuilder()
                .AllowAnyMethod()
                .AllowAnyHeader();

            // No hosts configured or only default configured.
            if (corsHosts.Length == 0
                || (corsHosts.Length == 1
                    && string.Equals(corsHosts[0], CorsConstants.AnyOrigin, StringComparison.Ordinal)))
            {
                builder.AllowAnyOrigin();
            }
            else
            {
                builder.WithOrigins(corsHosts)
                    .AllowCredentials();
            }

            return Task.FromResult<CorsPolicy?>(builder.Build());
        }
    }
}
