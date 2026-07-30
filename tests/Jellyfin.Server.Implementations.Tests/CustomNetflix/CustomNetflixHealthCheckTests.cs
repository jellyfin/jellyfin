using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Xunit;

namespace Jellyfin.Server.Implementations.CustomNetflix.Tests;

public sealed class CustomNetflixHealthCheckTests
{
    [Fact]
    public async Task DisabledRepository_IsUnhealthyForReadiness()
    {
        var healthCheck = new CustomNetflixHealthCheck(new DisabledCustomNetflixRepository());

        var result = await healthCheck.CheckHealthAsync(
            new HealthCheckContext(),
            TestContext.Current.CancellationToken);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    [Fact]
    public async Task ConfiguredButInvalidRepository_IsUnhealthy()
    {
        var healthCheck = new CustomNetflixHealthCheck(
            new DisabledCustomNetflixRepository(true, "invalid connection"));

        var result = await healthCheck.CheckHealthAsync(
            new HealthCheckContext(),
            TestContext.Current.CancellationToken);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }
}
