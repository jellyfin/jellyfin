using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.IO;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Concurrent;
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

        /// <summary>
        /// Initializes a new instance of the <see cref="ProviderManager" /> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client.</param>
        /// <param name="configurationManager">The configuration manager.</param>
        /// <param name="directoryWatchers">The directory watchers.</param>
        /// <param name="logManager">The log manager.</param>
        public ProviderManager(IHttpClient httpClient, IServerConfigurationManager configurationManager, IDirectoryWatchers directoryWatchers, ILogManager logManager)
        {
            _logger = logManager.GetLogger("ProviderManager");
            _httpClient = httpClient;
            ConfigurationManager = configurationManager;
            _directoryWatchers = directoryWatchers;
            _remoteImageCache = new FileSystemRepository(configurationManager.ApplicationPaths.DownloadedImagesDataPath);

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
            Task.Run(() => ValidateCurrentlyRunningProviders());
        }

        /// <summary>
        /// Adds the metadata providers.
        /// </summary>
        /// <param name="providers">The providers.</param>
        public void AddMetadataProviders(IEnumerable<BaseMetadataProvider> providers)
        {
            MetadataProviders = providers.OrderBy(e => e.Priority).ToArray();
        }

        /// <summary>
        /// The _supported providers key
        /// </summary>
        private readonly Guid _supportedProvidersKey = "SupportedProviders".GetMD5();

        /// <summary>
        /// Runs all metadata providers for an entity, and returns true or false indicating if at least one was refreshed and requires persistence
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="force">if set to <c>true</c> [force].</param>
        /// <param name="allowSlowProviders">if set to <c>true</c> [allow slow providers].</param>
        /// <returns>Task{System.Boolean}.</returns>
        public async Task<bool> ExecuteMetadataProviders(BaseItem item, CancellationToken cancellationToken, bool force = false, bool allowSlowProviders = true)
        {
            // Allow providers of the same priority to execute in parallel
            MetadataProviderPriority? currentPriority = null;
            var currentTasks = new List<Task<bool>>();

            var result = false;

            cancellationToken.ThrowIfCancellationRequested();

            // Determine if supported providers have changed
            var supportedProviders = MetadataProviders.Where(p => p.Supports(item)).ToList();

            BaseProviderInfo supportedProvidersInfo;

            var supportedProvidersValue = string.Join("+", supportedProviders.Select(i => i.GetType().Name));
            var providersChanged = false;

            item.ProviderData.TryGetValue(_supportedProvidersKey, out supportedProvidersInfo);
            if (supportedProvidersInfo != null)
            {
                // Force refresh if the supported providers have changed
                providersChanged = force = force || !string.Equals(supportedProvidersInfo.FileSystemStamp, supportedProvidersValue);

                // If providers have changed, clear provider info and update the supported providers hash
                if (providersChanged)
                {
                    _logger.Debug("Providers changed for {0}. Clearing and forcing refresh.", item.Name);
                    item.ProviderData.Clear();
                }
            }

            if (providersChanged)
            {
                supportedProvidersInfo.FileSystemStamp = supportedProvidersValue;
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

                currentTasks.Add(FetchAsync(provider, item, force, cancellationToken));
                currentPriority = provider.Priority;
            }

            if (currentTasks.Count > 0)
            {
                var results = await Task.WhenAll(currentTasks).ConfigureAwait(false);
                result |= results.Contains(true);
            }

            if (providersChanged)
            {
                item.ProviderData[_supportedProvidersKey] = supportedProvidersInfo;
            }
            
            return result || providersChanged;
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
        private async Task<bool> FetchAsync(BaseMetadataProvider provider, BaseItem item, bool force, CancellationToken cancellationToken)
        {
            if (item == null)
            {
                throw new ArgumentNullException();
            }

            cancellationToken.ThrowIfCancellationRequested();

            _logger.Debug("Running {0} for {1}", provider.GetType().Name, item.Path ?? item.Name ?? "--Unknown--");

            // This provides the ability to cancel just this one provider
            var innerCancellationTokenSource = new CancellationTokenSource();

            OnProviderRefreshBeginning(provider, item, innerCancellationTokenSource);

            try
            {
                return await provider.FetchAsync(item, force, CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, innerCancellationTokenSource.Token).Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException ex)
            {
                _logger.Debug("{0} canceled for {1}", provider.GetType().Name, item.Name);

                // If the outer cancellation token is the one that caused the cancellation, throw it
                if (cancellationToken.IsCancellationRequested && ex.CancellationToken == cancellationToken)
                {
                    throw;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.ErrorException("{0} failed refreshing {1}", ex, provider.GetType().Name, item.Name);

                provider.SetLastRefreshed(item, DateTime.UtcNow, ProviderRefreshStatus.Failure);
                return true;
            }
            finally
            {
                innerCancellationTokenSource.Dispose();

                OnProviderRefreshCompleted(provider, item);
            }
        }

        /// <summary>
        /// Notifies the kernal that a provider has begun refreshing
        /// </summary>
        /// <param name="provider">The provider.</param>
        /// <param name="item">The item.</param>
        /// <param name="cancellationTokenSource">The cancellation token source.</param>
        public void OnProviderRefreshBeginning(BaseMetadataProvider provider, BaseItem item, CancellationTokenSource cancellationTokenSource)
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
        public void OnProviderRefreshCompleted(BaseMetadataProvider provider, BaseItem item)
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
        private void ValidateCurrentlyRunningProviders()
        {
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
            var localPath = (ConfigurationManager.Configuration.SaveLocalMeta && item.MetaLocation != null) ?
                Path.Combine(item.MetaLocation, targetName) :
                _remoteImageCache.GetResourcePath(item.GetType().FullName + item.Path.ToLower(), targetName);

            var img = await _httpClient.GetMemoryStream(source, resourcePool, cancellationToken).ConfigureAwait(false);

            if (ConfigurationManager.Configuration.SaveLocalMeta) // queue to media directories
            {
                await SaveToLibraryFilesystem(item, localPath, img, cancellationToken).ConfigureAwait(false);
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
            if (cancellationToken == null)
            {
                throw new ArgumentNullException();
            }

            cancellationToken.ThrowIfCancellationRequested();

            //Tell the watchers to ignore
            _directoryWatchers.TemporarilyIgnore(path);

            dataToSave.Position = 0;

            try
            {
                using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read, StreamDefaults.DefaultFileStreamBufferSize, FileOptions.Asynchronous))
                {
                    using (var input = dataToSave)
                    {
                        await input.CopyToAsync(fs, StreamDefaults.DefaultCopyToBufferSize, cancellationToken).ConfigureAwait(false);
                    }

                    // If this is ever used for something other than metadata we can add a file type param
                    item.ResolveArgs.AddMetadataFile(path);
                }
            }
            finally
            {
                //Remove the ignore
                _directoryWatchers.RemoveTempIgnore(path);
            }
        }
    }
}
