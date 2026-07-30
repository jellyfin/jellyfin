using System;
using System.Threading.Tasks;
using MediaBrowser.Controller.Configuration;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;

namespace Jellyfin.Server.Configuration
{
    /// <summary>
    /// Cors policy provider.
    /// </summary>
    public class CorsPolicyProvider : ICorsPolicyProvider
    {
        private readonly IServerConfigurationManager _serverConfigurationManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="CorsPolicyProvider"/> class.
        /// </summary>
        /// <param name="serverConfigurationManager">Instance of the <see cref="IServerConfigurationManager"/> interface.</param>
        public CorsPolicyProvider(IServerConfigurationManager serverConfigurationManager)
        {
            _serverConfigurationManager = serverConfigurationManager;
        }

        /// <inheritdoc />
        public Task<CorsPolicy?> GetPolicyAsync(HttpContext context, string? policyName)
        {
            var corsHosts = _serverConfigurationManager.Configuration.CorsHosts;
            var builder = new CorsPolicyBuilder()
                .AllowAnyMethod()
                .AllowAnyHeader();

            // Wildcard CORS must be explicitly configured. An empty list keeps
            // same-origin clients working without exposing the API cross-origin.
            if (corsHosts.Length == 1
                && string.Equals(corsHosts[0], CorsConstants.AnyOrigin, StringComparison.Ordinal))
            {
                builder.AllowAnyOrigin();
            }
            else if (corsHosts.Length > 0)
            {
                builder.WithOrigins(corsHosts)
                    .AllowCredentials();
            }

            return Task.FromResult<CorsPolicy?>(builder.Build());
        }
    }
}
