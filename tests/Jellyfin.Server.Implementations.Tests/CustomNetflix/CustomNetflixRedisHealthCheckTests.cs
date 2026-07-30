using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Server.Implementations.CustomNetflix;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.CustomNetflix;

public class CustomNetflixRedisHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_ReturnsDegradedWhenRedisIsDisabled()
    {
        var healthCheck = new CustomNetflixRedisHealthCheck(new FakeCacheService(isEnabled: false));

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        Assert.Equal(HealthStatus.Degraded, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsHealthyWhenRedisPingSucceeds()
    {
        var healthCheck = new CustomNetflixRedisHealthCheck(new FakeCacheService(isEnabled: true));

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsDegradedWhenRedisPingFails()
    {
        var healthCheck = new CustomNetflixRedisHealthCheck(new FakeCacheService(isEnabled: true, throwOnHealth: true));

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        Assert.Equal(HealthStatus.Degraded, result.Status);
    }

    private sealed class FakeCacheService : ICustomNetflixCacheService
    {
        private readonly bool _throwOnHealth;

        public FakeCacheService(bool isEnabled, bool throwOnHealth = false)
        {
            IsEnabled = isEnabled;
            _throwOnHealth = throwOnHealth;
        }

        public bool IsEnabled { get; }

        public Task CheckHealthAsync(CancellationToken cancellationToken)
        {
            if (_throwOnHealth)
            {
                throw new InvalidOperationException("Redis unavailable.");
            }

            return Task.CompletedTask;
        }

        public Task<string?> GetStringAsync(string key, CancellationToken cancellationToken)
            => Task.FromResult<string?>(null);

        public Task SetStringAsync(string key, string value, TimeSpan? expiry, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task RemoveAsync(string key, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task RemoveAsync(IReadOnlyList<string> keys, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}
