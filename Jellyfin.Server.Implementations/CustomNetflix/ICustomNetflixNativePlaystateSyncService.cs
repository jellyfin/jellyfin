#pragma warning disable CS1591

using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.CustomNetflix;
using MediaBrowser.Controller.Entities;

namespace Jellyfin.Server.Implementations.CustomNetflix;

internal interface ICustomNetflixNativePlaystateSyncService
{
    Task SyncProgressAsync(CustomNetflixProfileDto profile, User user, BaseItem item, WatchProgressRow progress, string eventType, CancellationToken cancellationToken);

    Task SyncPlayedAsync(CustomNetflixProfileDto profile, User user, BaseItem item, bool played, CancellationToken cancellationToken);
}
