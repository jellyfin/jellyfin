using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Server.Implementations;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Jellyfin.Server.HealthChecks
{
    /// <summary>
    /// Checks connectivity to the database.
    /// </summary>
    public class JellyfinDbHealthCheck : IHealthCheck
    {
        private readonly JellyfinDbProvider _dbProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="JellyfinDbHealthCheck"/> class.
        /// </summary>
        /// <param name="dbProvider">The jellyfin db provider.</param>
        public JellyfinDbHealthCheck(JellyfinDbProvider dbProvider)
        {
            _dbProvider = dbProvider;
        }

        /// <inheritdoc />
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            await using var jellyfinDb = _dbProvider.CreateContext();
            if (await jellyfinDb.Database.CanConnectAsync(cancellationToken).ConfigureAwait(false))
            {
                return HealthCheckResult.Healthy("Database connection successful.");
            }

            return HealthCheckResult.Unhealthy("Unable to connect to the database.");
        }
    }
}
