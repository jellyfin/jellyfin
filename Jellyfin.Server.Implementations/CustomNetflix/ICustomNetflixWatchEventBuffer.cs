#pragma warning disable CS1591

using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Server.Implementations.CustomNetflix;

internal interface ICustomNetflixWatchEventBuffer
{
    ValueTask EnqueueAsync(WatchEventRow watchEvent, CancellationToken cancellationToken);

    Task FlushAsync(CancellationToken cancellationToken);
}
