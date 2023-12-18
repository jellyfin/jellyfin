using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Jellyfin.Server.HealthChecks;

/// <summary>
/// Implementation of the <see cref="DbContextHealthCheck{TContext}"/> for a <see cref="IDbContextFactory{TContext}"/>.
/// </summary>
/// <typeparam name="TContext">The type of database context.</typeparam>
public class DbContextFactoryHealthCheck<TContext> : IHealthCheck
    where TContext : DbContext
{
    private readonly IDbContextFactory<TContext> _dbContextFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="DbContextFactoryHealthCheck{TContext}"/> class.
    /// </summary>
    /// <param name="contextFactory">Instance of the <see cref="IDbContextFactory{TContext}"/> interface.</param>
    public DbContextFactoryHealthCheck(IDbContextFactory<TContext> contextFactory)
    {
        _dbContextFactory = contextFactory;
    }

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using (dbContext.ConfigureAwait(false))
        {
            if (await dbContext.Database.CanConnectAsync(cancellationToken).ConfigureAwait(false))
            {
                return HealthCheckResult.Healthy();
            }
        }

        return HealthCheckResult.Unhealthy();
    }
}
