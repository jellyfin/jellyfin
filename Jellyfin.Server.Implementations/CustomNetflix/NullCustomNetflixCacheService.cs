#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Server.Implementations.CustomNetflix;

internal sealed class NullCustomNetflixCacheService : ICustomNetflixCacheService
{
    public bool IsEnabled => false;

    public Task CheckHealthAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task<string?> GetStringAsync(string key, CancellationToken cancellationToken)
        => Task.FromResult<string?>(null);

    public Task SetStringAsync(string key, string value, TimeSpan? expiry, CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task RemoveAsync(string key, CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task RemoveAsync(IReadOnlyList<string> keys, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
