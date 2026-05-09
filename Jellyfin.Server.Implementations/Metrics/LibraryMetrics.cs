using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using Prometheus;

namespace Jellyfin.Server.Implementations.Metrics;

/// <summary>
/// Exposes Prometheus metrics describing the contents of the media library.
/// </summary>
public sealed class LibraryMetrics : IMetricsCollector
{
    private static readonly BaseItemKind[] _trackedKinds =
    [
        BaseItemKind.Movie,
        BaseItemKind.Series,
        BaseItemKind.Season,
        BaseItemKind.Episode,
        BaseItemKind.MusicArtist,
        BaseItemKind.MusicAlbum,
        BaseItemKind.Audio,
        BaseItemKind.AudioBook,
        BaseItemKind.Book,
        BaseItemKind.Photo,
        BaseItemKind.Video,
    ];

    private static readonly Gauge _items = Prometheus.Metrics
        .CreateGauge("jellyfin_library_items", "Number of items in the media library grouped by item type.", "type");

    private readonly ILibraryManager _libraryManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="LibraryMetrics"/> class.
    /// </summary>
    /// <param name="libraryManager">The library manager.</param>
    public LibraryMetrics(ILibraryManager libraryManager)
    {
        _libraryManager = libraryManager;
    }

    /// <inheritdoc />
    public string Name => nameof(LibraryMetrics);

    /// <inheritdoc />
    public Task CollectAsync(CancellationToken cancellationToken)
    {
        foreach (var kind in _trackedKinds)
        {
            var count = _libraryManager.GetCount(new InternalItemsQuery
            {
                IncludeItemTypes = [kind],
            });
            _items.WithLabels(kind.ToString()).Set(count);
        }

        return Task.CompletedTask;
    }
}
