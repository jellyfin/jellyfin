using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Trickplay;
using MediaBrowser.Model.Configuration;

namespace MediaBrowser.Providers.Trickplay;

/// <summary>
/// Class TrickplayProvider. Provides images and metadata for trickplay
/// scrubbing previews.
/// </summary>
public class TrickplayProvider : ICustomMetadataProvider<Episode>,
    ICustomMetadataProvider<MusicVideo>,
    ICustomMetadataProvider<Movie>,
    ICustomMetadataProvider<Trailer>,
    ICustomMetadataProvider<Video>,
    IHasItemChangeMonitor,
    IHasOrder,
    IForcedProvider
{
    private readonly IServerConfigurationManager _config;
    private readonly ITrickplayManager _trickplayManager;
    private readonly ILibraryManager _libraryManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="TrickplayProvider"/> class.
    /// </summary>
    /// <param name="config">The configuration manager.</param>
    /// <param name="trickplayManager">The trickplay manager.</param>
    /// <param name="libraryManager">The library manager.</param>
    public TrickplayProvider(
        IServerConfigurationManager config,
        ITrickplayManager trickplayManager,
        ILibraryManager libraryManager)
    {
        _config = config;
        _trickplayManager = trickplayManager;
        _libraryManager = libraryManager;
    }

    /// <inheritdoc />
    public string Name => "Trickplay Provider";

    /// <inheritdoc />
    public int Order => 100;

    /// <inheritdoc />
    public bool HasChanged(BaseItem item, IDirectoryService directoryService)
    {
        if (item.IsFileProtocol)
        {
            var file = directoryService.GetFile(item.Path);
            if (file is not null && item.DateModified != file.LastWriteTimeUtc)
            {
                return true;
            }
        }

        return false;
    }

    /// <inheritdoc />
    public Task<ItemUpdateType> FetchAsync(Episode item, MetadataRefreshOptions options, CancellationToken cancellationToken)
    {
        return FetchInternal(item, options, cancellationToken);
    }

    /// <inheritdoc />
    public Task<ItemUpdateType> FetchAsync(MusicVideo item, MetadataRefreshOptions options, CancellationToken cancellationToken)
    {
        return FetchInternal(item, options, cancellationToken);
    }

    /// <inheritdoc />
    public Task<ItemUpdateType> FetchAsync(Movie item, MetadataRefreshOptions options, CancellationToken cancellationToken)
    {
        return FetchInternal(item, options, cancellationToken);
    }

    /// <inheritdoc />
    public Task<ItemUpdateType> FetchAsync(Trailer item, MetadataRefreshOptions options, CancellationToken cancellationToken)
    {
        return FetchInternal(item, options, cancellationToken);
    }

    /// <inheritdoc />
    public Task<ItemUpdateType> FetchAsync(Video item, MetadataRefreshOptions options, CancellationToken cancellationToken)
    {
        return FetchInternal(item, options, cancellationToken);
    }

    private async Task<ItemUpdateType> FetchInternal(Video video, MetadataRefreshOptions options, CancellationToken cancellationToken)
    {
        var libraryOptions = _libraryManager.GetLibraryOptions(video);
        bool? enableDuringScan = libraryOptions?.ExtractTrickplayImagesDuringLibraryScan;
        bool replace = options.RegenerateTrickplay && options.MetadataRefreshMode > MetadataRefreshMode.Default;

        if (!enableDuringScan.GetValueOrDefault(false))
        {
            return ItemUpdateType.None;
        }

        if (_config.Configuration.TrickplayOptions.ScanBehavior == TrickplayScanBehavior.Blocking)
        {
            await _trickplayManager.RefreshTrickplayDataAsync(video, replace, libraryOptions, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            _ = _trickplayManager.RefreshTrickplayDataAsync(video, replace, libraryOptions, cancellationToken).ConfigureAwait(false);
        }

        // The core doesn't need to trigger any save operations over this
        return ItemUpdateType.None;
    }
}
