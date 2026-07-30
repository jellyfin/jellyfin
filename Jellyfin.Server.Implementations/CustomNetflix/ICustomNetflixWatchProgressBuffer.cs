#pragma warning disable CS1591

using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Server.Implementations.CustomNetflix;

internal interface ICustomNetflixWatchProgressBuffer
{
    ValueTask EnqueueAsync(WatchProgressRow progress, CancellationToken cancellationToken);

    Task FlushAsync(CancellationToken cancellationToken);
}
