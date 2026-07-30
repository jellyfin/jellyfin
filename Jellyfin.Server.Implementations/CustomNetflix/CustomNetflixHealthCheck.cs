#pragma warning disable CS1591

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Jellyfin.Server.Implementations.CustomNetflix;

internal sealed class CustomNetflixHealthCheck : IHealthCheck
{
    private readonly ICustomNetflixRepository _repository;

    public CustomNetflixHealthCheck(ICustomNetflixRepository repository)
    {
        _repository = repository;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!_repository.IsEnabled)
        {
            return HealthCheckResult.Unhealthy("CustomNetflix PostgreSQL is required but not configured.");
        }

        try
        {
            await _repository.CheckHealthAsync(cancellationToken).ConfigureAwait(false);
            return HealthCheckResult.Healthy("CustomNetflix PostgreSQL is reachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("CustomNetflix PostgreSQL is configured but unreachable.", ex);
        }
    }
}
