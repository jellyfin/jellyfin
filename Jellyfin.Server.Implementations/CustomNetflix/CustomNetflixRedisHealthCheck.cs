#pragma warning disable CS1591

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Jellyfin.Server.Implementations.CustomNetflix;

internal sealed class CustomNetflixRedisHealthCheck : IHealthCheck
{
    private readonly ICustomNetflixCacheService _cache;

    public CustomNetflixRedisHealthCheck(ICustomNetflixCacheService cache)
    {
        _cache = cache;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!_cache.IsEnabled)
        {
            return HealthCheckResult.Degraded("CustomNetflix Redis is not configured.");
        }

        try
        {
            await _cache.CheckHealthAsync(cancellationToken).ConfigureAwait(false);
            return HealthCheckResult.Healthy("CustomNetflix Redis is reachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Degraded("CustomNetflix Redis is configured but unreachable; CustomNetflix will continue without Redis cache.", ex);
        }
    }
}
