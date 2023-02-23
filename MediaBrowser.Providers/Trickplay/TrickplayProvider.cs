using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Trickplay;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Providers.Trickplay
{
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
        private readonly ILogger<TrickplayProvider> _logger;
        private readonly IServerConfigurationManager _configurationManager;
        private readonly ITrickplayManager _trickplayManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="TrickplayProvider"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="configurationManager">The configuration manager.</param>
        /// <param name="trickplayManager">The trickplay manager.</param>
        public TrickplayProvider(
            ILogger<TrickplayProvider> logger,
            IServerConfigurationManager configurationManager,
            ITrickplayManager trickplayManager)
        {
            _logger = logger;
            _configurationManager = configurationManager;
            _trickplayManager = trickplayManager;
        }

        /// <inheritdoc />
        public string Name => "Trickplay Preview";

        /// <inheritdoc />
        public int Order => 1000;

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

        private async Task<ItemUpdateType> FetchInternal(Video item, MetadataRefreshOptions options, CancellationToken cancellationToken)
        {
            // TODO: implement all config options -->
            // TODO: this is always blocking for metadata collection, make non-blocking option
            await _trickplayManager.RefreshTrickplayData(item, options.ReplaceAllImages, cancellationToken).ConfigureAwait(false);

            // The core doesn't need to trigger any save operations over this
            return ItemUpdateType.None;
        }
    }
}
