using MediaBrowser.Common.IO;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Providers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Providers
{
    /// <summary>
    /// Class ProviderManager
    /// </summary>
    public class ProviderManager : IProviderManager
    {
        /// <summary>
        /// The _logger
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// The _HTTP client
        /// </summary>
        private readonly IHttpClient _httpClient;

        /// <summary>
        /// The _directory watchers
        /// </summary>
        private readonly IDirectoryWatchers _directoryWatchers;

        /// <summary>
        /// Gets or sets the configuration manager.
        /// </summary>
        /// <value>The configuration manager.</value>
        private IServerConfigurationManager ConfigurationManager { get; set; }

        /// <summary>
        /// Gets the list of currently registered metadata prvoiders
        /// </summary>
        /// <value>The metadata providers enumerable.</value>
        private BaseMetadataProvider[] MetadataProviders { get; set; }

        private IImageProvider[] ImageProviders { get; set; }
        private readonly IFileSystem _fileSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProviderManager" /> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client.</param>
        /// <param name="configurationManager">The configuration manager.</param>
        /// <param name="directoryWatchers">The directory watchers.</param>
        /// <param name="logManager">The log manager.</param>
        public ProviderManager(IHttpClient httpClient, IServerConfigurationManager configurationManager, IDirectoryWatchers directoryWatchers, ILogManager logManager, IFileSystem fileSystem)
        {
            _logger = logManager.GetLogger("ProviderManager");
            _httpClient = httpClient;
            ConfigurationManager = configurationManager;
            _directoryWatchers = directoryWatchers;
            _fileSystem = fileSystem;
        }

        /// <summary>
        /// Adds the metadata providers.
        /// </summary>
        /// <param name="providers">The providers.</param>
        /// <param name="imageProviders">The image providers.</param>
        public void AddParts(IEnumerable<BaseMetadataProvider> providers, IEnumerable<IImageProvider> imageProviders)
        {
            MetadataProviders = providers.OrderBy(e => e.Priority).ToArray();

            ImageProviders = imageProviders.OrderByDescending(i => i.Priority).ToArray();
        }

        /// <summary>
        /// Runs all metadata providers for an entity, and returns true or false indicating if at least one was refreshed and requires persistence
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="force">if set to <c>true</c> [force].</param>
        /// <param name="allowSlowProviders">if set to <c>true</c> [allow slow providers].</param>
        /// <returns>Task{System.Boolean}.</returns>
        public async Task<ItemUpdateType?> ExecuteMetadataProviders(BaseItem item, CancellationToken cancellationToken, bool force = false, bool allowSlowProviders = true)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            ItemUpdateType? result = null;

            cancellationToken.ThrowIfCancellationRequested();

            var enableInternetProviders = ConfigurationManager.Configuration.EnableInternetProviders;
            var excludeTypes = ConfigurationManager.Configuration.InternetProviderExcludeTypes;

            // Run the normal providers sequentially in order of priority
            foreach (var provider in MetadataProviders)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!ProviderSupportsItem(provider, item))
                {
                    continue;
                }

                // Skip if internet providers are currently disabled
                if (provider.RequiresInternet && !enableInternetProviders)
                {
                    continue;
                }

                // Skip if is slow and we aren't allowing slow ones
                if (provider.IsSlow && !allowSlowProviders)
                {
                    continue;
                }

                // Skip if internet provider and this type is not allowed
                if (provider.RequiresInternet &&
                    enableInternetProviders &&
                    excludeTypes.Length > 0 &&
                    excludeTypes.Contains(item.GetType().Name, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Put this check below the await because the needs refresh of the next tier of providers may depend on the previous ones running
                //  This is the case for the fan art provider which depends on the movie and tv providers having run before them
                if (provider.RequiresInternet && item.DontFetchMeta && provider.EnforceDontFetchMetadata)
                {
                    continue;
                }

                try
                {
                    if (!force && !provider.NeedsRefresh(item))
                    {
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error("Error determining NeedsRefresh for {0}", ex, item.Path);
                }

                var updateType = await FetchAsync(provider, item, force, cancellationToken).ConfigureAwait(false);

                if (updateType.HasValue)
                {
                    if (result.HasValue)
                    {
                        result = result.Value | updateType.Value;
                    }
                    else
                    {
                        result = updateType;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Providers the supports item.
        /// </summary>
        /// <param name="provider">The provider.</param>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        private bool ProviderSupportsItem(BaseMetadataProvider provider, BaseItem item)
        {
            try
            {
                return provider.Supports(item);
            }
            catch (Exception ex)
            {
                _logger.ErrorException("{0} failed in Supports for type {1}", ex, provider.GetType().Name, item.GetType().Name);
                return false;
            }
        }

        /// <summary>
        /// Fetches metadata and returns true or false indicating if any work that requires persistence was done
        /// </summary>
        /// <param name="provider">The provider.</param>
        /// <param name="item">The item.</param>
        /// <param name="force">if set to <c>true</c> [force].</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{System.Boolean}.</returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        private async Task<ItemUpdateType?> FetchAsync(BaseMetadataProvider provider, BaseItem item, bool force, CancellationToken cancellationToken)
        {
            if (item == null)
            {
                throw new ArgumentNullException();
            }

            cancellationToken.ThrowIfCancellationRequested();

            // Don't clog up the log with these providers
            if (!(provider is IDynamicInfoProvider))
            {
                _logger.Debug("Running {0} for {1}", provider.GetType().Name, item.Path ?? item.Name ?? "--Unknown--");
            }

            // This provides the ability to cancel just this one provider
            var innerCancellationTokenSource = new CancellationTokenSource();

            try
            {
                var changed = await provider.FetchAsync(item, force, CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, innerCancellationTokenSource.Token).Token).ConfigureAwait(false);

                if (changed)
                {
                    return provider.ItemUpdateType;
                }

                return null;
            }
            catch (OperationCanceledException ex)
            {
                _logger.Debug("{0} canceled for {1}", provider.GetType().Name, item.Name);

                // If the outer cancellation token is the one that caused the cancellation, throw it
                if (cancellationToken.IsCancellationRequested && ex.CancellationToken == cancellationToken)
                {
                    throw;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.ErrorException("{0} failed refreshing {1} {2}", ex, provider.GetType().Name, item.Name, item.Path ?? string.Empty);

                provider.SetLastRefreshed(item, DateTime.UtcNow, ProviderRefreshStatus.Failure);

                return ItemUpdateType.Unspecified;
            }
            finally
            {
                innerCancellationTokenSource.Dispose();
            }
        }

        /// <summary>
        /// Saves to library filesystem.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="path">The path.</param>
        /// <param name="dataToSave">The data to save.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public async Task SaveToLibraryFilesystem(BaseItem item, string path, Stream dataToSave, CancellationToken cancellationToken)
        {
            if (item == null)
            {
                throw new ArgumentNullException();
            }
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException();
            }
            if (dataToSave == null)
            {
                throw new ArgumentNullException();
            }

            if (cancellationToken.IsCancellationRequested)
            {
                dataToSave.Dispose();
                cancellationToken.ThrowIfCancellationRequested();
            }

            //Tell the watchers to ignore
            _directoryWatchers.TemporarilyIgnore(path);

            if (dataToSave.CanSeek)
            {
                dataToSave.Position = 0;
            }

            try
            {
                using (dataToSave)
                {
                    using (var fs = _fileSystem.GetFileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read, true))
                    {
                        await dataToSave.CopyToAsync(fs, StreamDefaults.DefaultCopyToBufferSize, cancellationToken).ConfigureAwait(false);
                    }
                }

                // If this is ever used for something other than metadata we can add a file type param
                item.ResolveArgs.AddMetadataFile(path);
            }
            finally
            {
                //Remove the ignore
                _directoryWatchers.RemoveTempIgnore(path);
            }
        }


        /// <summary>
        /// Saves the image.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="url">The URL.</param>
        /// <param name="resourcePool">The resource pool.</param>
        /// <param name="type">The type.</param>
        /// <param name="imageIndex">Index of the image.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public async Task SaveImage(BaseItem item, string url, SemaphoreSlim resourcePool, ImageType type, int? imageIndex, CancellationToken cancellationToken)
        {
            var response = await _httpClient.GetResponse(new HttpRequestOptions
            {
                CancellationToken = cancellationToken,
                ResourcePool = resourcePool,
                Url = url

            }).ConfigureAwait(false);

            await SaveImage(item, response.Content, response.ContentType, type, imageIndex, url, cancellationToken)
                    .ConfigureAwait(false);
        }

        /// <summary>
        /// Saves the image.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="source">The source.</param>
        /// <param name="mimeType">Type of the MIME.</param>
        /// <param name="type">The type.</param>
        /// <param name="imageIndex">Index of the image.</param>
        /// <param name="sourceUrl">The source URL.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public Task SaveImage(BaseItem item, Stream source, string mimeType, ImageType type, int? imageIndex, string sourceUrl, CancellationToken cancellationToken)
        {
            return new ImageSaver(ConfigurationManager, _directoryWatchers, _fileSystem).SaveImage(item, source, mimeType, type, imageIndex, sourceUrl, cancellationToken);
        }

        /// <summary>
        /// Gets the available remote images.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="providerName">Name of the provider.</param>
        /// <param name="type">The type.</param>
        /// <returns>Task{IEnumerable{RemoteImageInfo}}.</returns>
        public async Task<IEnumerable<RemoteImageInfo>> GetAvailableRemoteImages(BaseItem item, CancellationToken cancellationToken, string providerName = null, ImageType? type = null)
        {
            var providers = GetImageProviders(item);

            if (!string.IsNullOrEmpty(providerName))
            {
                providers = providers.Where(i => string.Equals(i.Name, providerName, StringComparison.OrdinalIgnoreCase));
            }

            var preferredLanguage = ConfigurationManager.Configuration.PreferredMetadataLanguage;

            var tasks = providers.Select(i => Task.Run(async () =>
            {
                try
                {
                    if (type.HasValue)
                    {
                        var result = await i.GetImages(item, type.Value, cancellationToken).ConfigureAwait(false);

                        return FilterImages(result, preferredLanguage);
                    }
                    else
                    {
                        var result = await i.GetAllImages(item, cancellationToken).ConfigureAwait(false);
                        return FilterImages(result, preferredLanguage);
                    }
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("{0} failed in GetImages for type {1}", ex, i.GetType().Name, item.GetType().Name);
                    return new List<RemoteImageInfo>();
                }

            }, cancellationToken));

            var results = await Task.WhenAll(tasks).ConfigureAwait(false);

            return results.SelectMany(i => i);
        }

        private IEnumerable<RemoteImageInfo> FilterImages(IEnumerable<RemoteImageInfo> images, string preferredLanguage)
        {
            if (string.Equals(preferredLanguage, "en", StringComparison.OrdinalIgnoreCase))
            {
                images = images.Where(i => string.IsNullOrEmpty(i.Language) ||
                                           string.Equals(i.Language, "en", StringComparison.OrdinalIgnoreCase));
            }

            return images;
        }

        /// <summary>
        /// Gets the supported image providers.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>IEnumerable{IImageProvider}.</returns>
        public IEnumerable<IImageProvider> GetImageProviders(BaseItem item)
        {
            return ImageProviders.Where(i =>
            {
                try
                {
                    return i.Supports(item);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("{0} failed in Supports for type {1}", ex, i.GetType().Name, item.GetType().Name);
                    return false;
                }
            });
        }
    }
}
