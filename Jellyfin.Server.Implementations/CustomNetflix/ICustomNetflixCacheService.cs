#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Server.Implementations.CustomNetflix;

internal interface ICustomNetflixCacheService
{
    bool IsEnabled { get; }

    Task CheckHealthAsync(CancellationToken cancellationToken);

    Task<string?> GetStringAsync(string key, CancellationToken cancellationToken);

    Task SetStringAsync(string key, string value, TimeSpan? expiry, CancellationToken cancellationToken);

    Task RemoveAsync(string key, CancellationToken cancellationToken);

    Task RemoveAsync(IReadOnlyList<string> keys, CancellationToken cancellationToken);
}
