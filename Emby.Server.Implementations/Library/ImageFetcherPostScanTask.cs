using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Events;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.Library
{
    /// <summary>
    /// A library post scan/refresh task for pre-fetching remote images.
    /// </summary>
    public class ImageFetcherPostScanTask : ILibraryPostScanTask
    {
        private readonly ILibraryManager _libraryManager;
        private readonly IProviderManager _providerManager;
        private readonly ILogger<ImageFetcherPostScanTask> _logger;
        private readonly SemaphoreSlim _imageFetcherLock;

        private ConcurrentDictionary<Guid, (BaseItem item, ItemUpdateType updateReason)> _queuedItems;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageFetcherPostScanTask"/> class.
        /// </summary>
        /// <param name="libraryManager">An instance of <see cref="ILibraryManager"/>.</param>
        /// <param name="providerManager">An instance of <see cref="IProviderManager"/>.</param>
        /// <param name="logger">An instance of <see cref="ILogger{ImageFetcherPostScanTask}"/>.</param>
        public ImageFetcherPostScanTask(
            ILibraryManager libraryManager,
            IProviderManager providerManager,
            ILogger<ImageFetcherPostScanTask> logger)
        {
            _libraryManager = libraryManager;
            _providerManager = providerManager;
            _logger = logger;
            _queuedItems = new ConcurrentDictionary<Guid, (BaseItem item, ItemUpdateType updateReason)>();
            _imageFetcherLock = new SemaphoreSlim(1, 1);
            _libraryManager.ItemAdded += OnLibraryManagerItemAddedOrUpdated;
            _libraryManager.ItemUpdated += OnLibraryManagerItemAddedOrUpdated;
            _providerManager.RefreshCompleted += OnProviderManagerRefreshCompleted;
        }

        /// <inheritdoc />
        public async Task Run(IProgress<double> progress, CancellationToken cancellationToken)
        {
            // Sometimes a library scan will cause this to run twice if there's an item refresh going on.
            await _imageFetcherLock.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                var now = DateTime.UtcNow;
                var itemGuids = _queuedItems.Keys.ToList();

                for (var i = 0; i < itemGuids.Count; i++)
                {
                    if (!_queuedItems.TryGetValue(itemGuids[i], out var queuedItem))
                    {
                        continue;
                    }

                    _logger.LogDebug(
                        "Updating remote images for item {ItemId} with media type {ItemMediaType}",
                        queuedItem.item.Id.ToString("N", CultureInfo.InvariantCulture),
                        queuedItem.item.GetType());
                    await _libraryManager.UpdateImagesAsync(queuedItem.item, queuedItem.updateReason >= ItemUpdateType.ImageUpdate).ConfigureAwait(false);

                    _queuedItems.TryRemove(queuedItem.item.Id, out _);
                }

                if (itemGuids.Count > 0)
                {
                    _logger.LogInformation(
                        "Finished updating/pre-fetching {NumberOfImages} images. Elapsed time: {TimeElapsed}s.",
                        itemGuids.Count.ToString(CultureInfo.InvariantCulture),
                        (DateTime.UtcNow - now).TotalSeconds.ToString(CultureInfo.InvariantCulture));
                }
                else
                {
                    _logger.LogDebug("No images were updated.");
                }
            }
            finally
            {
                _imageFetcherLock.Release();
            }
        }

        private void OnLibraryManagerItemAddedOrUpdated(object sender, ItemChangeEventArgs itemChangeEventArgs)
        {
            if (!_queuedItems.ContainsKey(itemChangeEventArgs.Item.Id) && itemChangeEventArgs.Item.ImageInfos.Length > 0)
            {
                _queuedItems.AddOrUpdate(
                    itemChangeEventArgs.Item.Id,
                    (itemChangeEventArgs.Item, itemChangeEventArgs.UpdateReason),
                    (key, existingValue) => existingValue);
            }
        }

        private void OnProviderManagerRefreshCompleted(object sender, GenericEventArgs<BaseItem> e)
        {
            if (!_queuedItems.ContainsKey(e.Argument.Id) && e.Argument.ImageInfos.Length > 0)
            {
                _queuedItems.AddOrUpdate(
                    e.Argument.Id,
                    (e.Argument, ItemUpdateType.None),
                    (key, existingValue) => existingValue);
            }

            // The RefreshCompleted event is a bit awkward in that it seems to _only_ be fired on
            // the item that was refreshed regardless of children refreshes. So we take it as a signal
            // that the refresh is entirely completed.
            Run(null, CancellationToken.None).GetAwaiter().GetResult();
        }
    }
}
