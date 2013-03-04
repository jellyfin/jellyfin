using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.IO;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Providers
{
    /// <summary>
    /// Class ProviderManager
    /// </summary>
    public class ProviderManager : BaseManager<Kernel>
    {
        /// <summary>
        /// The remote image cache
        /// </summary>
        private readonly FileSystemRepository _remoteImageCache;

        /// <summary>
        /// The currently running metadata providers
        /// </summary>
        private readonly ConcurrentDictionary<string, Tuple<BaseMetadataProvider, BaseItem, CancellationTokenSource>> _currentlyRunningProviders =
            new ConcurrentDictionary<string, Tuple<BaseMetadataProvider, BaseItem, CancellationTokenSource>>();

        /// <summary>
        /// The _logger
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// The _HTTP client
        /// </summary>
        private readonly IHttpClient _httpClient;

        private IServerConfigurationManager ConfigurationManager { get; set; }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="ProviderManager" /> class.
        /// </summary>
        /// <param name="kernel">The kernel.</param>
        /// <param name="httpClient">The HTTP client.</param>
        /// <param name="logger">The logger.</param>
        public ProviderManager(Kernel kernel, IHttpClient httpClient, ILogger logger, IServerConfigurationManager configurationManager)
            : base(kernel)
        {
            _logger = logger;
            _httpClient = httpClient;
            ConfigurationManager = configurationManager;
            _remoteImageCache = new FileSystemRepository(ImagesDataPath);

            configurationManager.ConfigurationUpdated += configurationManager_ConfigurationUpdated;
        }

        /// <summary>
        /// Handles the ConfigurationUpdated event of the configurationManager control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        void configurationManager_ConfigurationUpdated(object sender, EventArgs e)
        {
            // Validate currently executing providers, in the background
            Task.Run(() =>
            {
                ValidateCurrentlyRunningProviders();
            });
        }

        /// <summary>
        /// The _images data path
        /// </summary>
        private string _imagesDataPath;
        /// <summary>
        /// Gets the images data path.
        /// </summary>
        /// <value>The images data path.</value>
        public string ImagesDataPath
        {
            get
            {
                if (_imagesDataPath == null)
                {
                    _imagesDataPath = Path.Combine(ConfigurationManager.ApplicationPaths.DataPath, "remote-images");

                    if (!Directory.Exists(_imagesDataPath))
                    {
                        Directory.CreateDirectory(_imagesDataPath);
                    }
                }

                return _imagesDataPath;
            }
        }

        /// <summary>
        /// Gets or sets the supported providers key.
        /// </summary>
        /// <value>The supported providers key.</value>
        private Guid SupportedProvidersKey { get; set; }

        /// <summary>
        /// Runs all metadata providers for an entity, and returns true or false indicating if at least one was refreshed and requires persistence
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="force">if set to <c>true</c> [force].</param>
        /// <param name="allowSlowProviders">if set to <c>true</c> [allow slow providers].</param>
        /// <returns>Task{System.Boolean}.</returns>
        internal async Task<bool> ExecuteMetadataProviders(BaseItem item, CancellationToken cancellationToken, bool force = false, bool allowSlowProviders = true)
        {
            // Allow providers of the same priority to execute in parallel
            MetadataProviderPriority? currentPriority = null;
            var currentTasks = new List<Task<bool>>();

            var result = false;

            cancellationToken.ThrowIfCancellationRequested();

            // Determine if supported providers have changed
            var supportedProviders = Kernel.MetadataProviders.Where(p => p.Supports(item)).ToList();

            BaseProviderInfo supportedProvidersInfo;

            if (SupportedProvidersKey == Guid.Empty)
            {
                SupportedProvidersKey = "SupportedProviders".GetMD5();
            }

            var supportedProvidersHash = string.Join("+", supportedProviders.Select(i => i.GetType().Name)).GetMD5();
            bool providersChanged;

            item.ProviderData.TryGetValue(SupportedProvidersKey, out supportedProvidersInfo);
            if (supportedProvidersInfo == null)
            {
                // First time
                supportedProvidersInfo = new BaseProviderInfo { ProviderId = SupportedProvidersKey, FileSystemStamp = supportedProvidersHash };
                providersChanged = force = true;
            }
            else
            {
                // Force refresh if the supported providers have changed
                providersChanged = force = force || supportedProvidersInfo.FileSystemStamp != supportedProvidersHash;
            }

            // If providers have changed, clear provider info and update the supported providers hash
            if (providersChanged)
            {
                _logger.Debug("Providers changed for {0}. Clearing and forcing refresh.", item.Name);
                item.ProviderData.Clear();
                supportedProvidersInfo.FileSystemStamp = supportedProvidersHash;
            }

            if (force) item.ClearMetaValues();

            // Run the normal providers sequentially in order of priority
            foreach (var provider in supportedProviders)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Skip if internet providers are currently disabled
                if (provider.RequiresInternet && !ConfigurationManager.Configuration.EnableInternetProviders)
                {
                    continue;
                }

                // Skip if is slow and we aren't allowing slow ones
                if (provider.IsSlow && !allowSlowProviders)
                {
                    continue;
                }

                // Skip if internet provider and this type is not allowed
                if (provider.RequiresInternet && ConfigurationManager.Configuration.EnableInternetProviders && ConfigurationManager.Configuration.InternetProviderExcludeTypes.Contains(item.GetType().Name, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                // When a new priority is reached, await the ones that are currently running and clear the list
                if (currentPriority.HasValue && currentPriority.Value != provider.Priority && currentTasks.Count > 0)
                {
                    var results = await Task.WhenAll(currentTasks).ConfigureAwait(false);
                    result |= results.Contains(true);

                    currentTasks.Clear();
                }

                // Put this check below the await because the needs refresh of the next tier of providers may depend on the previous ones running
                //  This is the case for the fan art provider which depends on the movie and tv providers having run before them
                if (!force && !provider.NeedsRefresh(item))
                {
                    continue;
                }

                currentTasks.Add(provider.FetchAsync(item, force, cancellationToken));
                currentPriority = provider.Priority;
            }

            if (currentTasks.Count > 0)
            {
                var results = await Task.WhenAll(currentTasks).ConfigureAwait(false);
                result |= results.Contains(true);
            }

            if (providersChanged)
            {
                item.ProviderData[SupportedProvidersKey] = supportedProvidersInfo;
            }
            
            return result || providersChanged;
        }

        /// <summary>
        /// Notifies the kernal that a provider has begun refreshing
        /// </summary>
        /// <param name="provider">The provider.</param>
        /// <param name="item">The item.</param>
        /// <param name="cancellationTokenSource">The cancellation token source.</param>
        internal void OnProviderRefreshBeginning(BaseMetadataProvider provider, BaseItem item, CancellationTokenSource cancellationTokenSource)
        {
            var key = item.Id + provider.GetType().Name;

            Tuple<BaseMetadataProvider, BaseItem, CancellationTokenSource> current;

            if (_currentlyRunningProviders.TryGetValue(key, out current))
            {
                try
                {
                    current.Item3.Cancel();
                }
                catch (ObjectDisposedException)
                {
                    
                }
            }

            var tuple = new Tuple<BaseMetadataProvider, BaseItem, CancellationTokenSource>(provider, item, cancellationTokenSource);

            _currentlyRunningProviders.AddOrUpdate(key, tuple, (k, v) => tuple);
        }

        /// <summary>
        /// Notifies the kernal that a provider has completed refreshing
        /// </summary>
        /// <param name="provider">The provider.</param>
        /// <param name="item">The item.</param>
        internal void OnProviderRefreshCompleted(BaseMetadataProvider provider, BaseItem item)
        {
            var key = item.Id + provider.GetType().Name;

            Tuple<BaseMetadataProvider, BaseItem, CancellationTokenSource> current;

            if (_currentlyRunningProviders.TryRemove(key, out current))
            {
                current.Item3.Dispose();
            }
        }

        /// <summary>
        /// Validates the currently running providers and cancels any that should not be run due to configuration changes
        /// </summary>
        internal void ValidateCurrentlyRunningProviders()
        {
            _logger.Info("Validing currently running providers");

            var enableInternetProviders = ConfigurationManager.Configuration.EnableInternetProviders;
            var internetProviderExcludeTypes = ConfigurationManager.Configuration.InternetProviderExcludeTypes;

            foreach (var tuple in _currentlyRunningProviders.Values
                .Where(p => p.Item1.RequiresInternet && (!enableInternetProviders || internetProviderExcludeTypes.Contains(p.Item2.GetType().Name, StringComparer.OrdinalIgnoreCase)))
                .ToList())
            {
                tuple.Item3.Cancel();
            }
        }

        /// <summary>
        /// Downloads the and save image.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="source">The source.</param>
        /// <param name="targetName">Name of the target.</param>
        /// <param name="resourcePool">The resource pool.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{System.String}.</returns>
        /// <exception cref="System.ArgumentNullException">item</exception>
        public async Task<string> DownloadAndSaveImage(BaseItem item, string source, string targetName, SemaphoreSlim resourcePool, CancellationToken cancellationToken)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }
            if (string.IsNullOrEmpty(source))
            {
                throw new ArgumentNullException("source");
            }
            if (string.IsNullOrEmpty(targetName))
            {
                throw new ArgumentNullException("targetName");
            }
            if (resourcePool == null)
            {
                throw new ArgumentNullException("resourcePool");
            }

            //download and save locally
            var localPath = ConfigurationManager.Configuration.SaveLocalMeta ?
                Path.Combine(item.MetaLocation, targetName) :
                _remoteImageCache.GetResourcePath(item.GetType().FullName + item.Path.ToLower(), targetName);

            var img = await _httpClient.GetMemoryStream(source, resourcePool, cancellationToken).ConfigureAwait(false);

            if (ConfigurationManager.Configuration.SaveLocalMeta) // queue to media directories
            {
                await Kernel.FileSystemManager.SaveToLibraryFilesystem(item, localPath, img, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                // we can write directly here because it won't affect the watchers

                try
                {
                    using (var fs = new FileStream(localPath, FileMode.Create, FileAccess.Write, FileShare.Read, StreamDefaults.DefaultFileStreamBufferSize, FileOptions.Asynchronous))
                    {
                        await img.CopyToAsync(fs, StreamDefaults.DefaultCopyToBufferSize, cancellationToken).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    _logger.ErrorException("Error downloading and saving image " + localPath, e);
                    throw;
                }
                finally
                {
                    img.Dispose();
                }

            }
            return localPath;
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="dispose"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected override void Dispose(bool dispose)
        {
            if (dispose)
            {
                _remoteImageCache.Dispose();
            }

            base.Dispose(dispose);
        }
    }
}
