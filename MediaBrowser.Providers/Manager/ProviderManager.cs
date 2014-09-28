using MediaBrowser.Common.IO;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Providers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.Manager
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
        private readonly ILibraryMonitor _libraryMonitor;

        /// <summary>
        /// Gets or sets the configuration manager.
        /// </summary>
        /// <value>The configuration manager.</value>
        private IServerConfigurationManager ConfigurationManager { get; set; }

        private IImageProvider[] ImageProviders { get; set; }

        private readonly IFileSystem _fileSystem;

        private IMetadataService[] _metadataServices = { };
        private IItemIdentityProvider[] _identityProviders = { };
        private IItemIdentityConverter[] _identityConverters = { };
        private IMetadataProvider[] _metadataProviders = { };
        private IEnumerable<IMetadataSaver> _savers;
        private IImageSaver[] _imageSavers;

        private IExternalId[] _externalIds;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProviderManager" /> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client.</param>
        /// <param name="configurationManager">The configuration manager.</param>
        /// <param name="libraryMonitor">The directory watchers.</param>
        /// <param name="logManager">The log manager.</param>
        /// <param name="fileSystem">The file system.</param>
        public ProviderManager(IHttpClient httpClient, IServerConfigurationManager configurationManager, ILibraryMonitor libraryMonitor, ILogManager logManager, IFileSystem fileSystem)
        {
            _logger = logManager.GetLogger("ProviderManager");
            _httpClient = httpClient;
            ConfigurationManager = configurationManager;
            _libraryMonitor = libraryMonitor;
            _fileSystem = fileSystem;
        }

        /// <summary>
        /// Adds the metadata providers.
        /// </summary>
        /// <param name="imageProviders">The image providers.</param>
        /// <param name="metadataServices">The metadata services.</param>
        /// <param name="identityProviders">The identity providers.</param>
        /// <param name="identityConverters">The identity converters.</param>
        /// <param name="metadataProviders">The metadata providers.</param>
        /// <param name="metadataSavers">The metadata savers.</param>
        /// <param name="imageSavers">The image savers.</param>
        /// <param name="externalIds">The external ids.</param>
        public void AddParts(IEnumerable<IImageProvider> imageProviders, IEnumerable<IMetadataService> metadataServices,
                             IEnumerable<IItemIdentityProvider> identityProviders, IEnumerable<IItemIdentityConverter> identityConverters,
                             IEnumerable<IMetadataProvider> metadataProviders, IEnumerable<IMetadataSaver> metadataSavers,
                             IEnumerable<IImageSaver> imageSavers, IEnumerable<IExternalId> externalIds)
        {
            ImageProviders = imageProviders.ToArray();

            _metadataServices = metadataServices.OrderBy(i => i.Order).ToArray();
            _identityProviders = identityProviders.ToArray();
            _identityConverters = identityConverters.ToArray();
            _metadataProviders = metadataProviders.ToArray();
            _savers = metadataSavers.ToArray();
            _imageSavers = imageSavers.ToArray();
            _externalIds = externalIds.OrderBy(i => i.Name).ToArray();
        }

        public Task RefreshMetadata(IHasMetadata item, MetadataRefreshOptions options, CancellationToken cancellationToken)
        {
            var service = _metadataServices.FirstOrDefault(i => i.CanRefresh(item));

            if (service != null)
            {
                return service.RefreshMetadata(item, options, cancellationToken);
            }

            _logger.Error("Unable to find a metadata service for item of type " + item.GetType().Name);
            return Task.FromResult(true);
        }

        public async Task SaveImage(IHasImages item, string url, SemaphoreSlim resourcePool, ImageType type, int? imageIndex, CancellationToken cancellationToken)
        {
            var response = await _httpClient.GetResponse(new HttpRequestOptions
            {
                CancellationToken = cancellationToken,
                ResourcePool = resourcePool,
                Url = url

            }).ConfigureAwait(false);

            await SaveImage(item, response.Content, response.ContentType, type, imageIndex, cancellationToken)
                    .ConfigureAwait(false);
        }

        public Task SaveImage(IHasImages item, Stream source, string mimeType, ImageType type, int? imageIndex, CancellationToken cancellationToken)
        {
            return new ImageSaver(ConfigurationManager, _libraryMonitor, _fileSystem, _logger).SaveImage(item, source, mimeType, type, imageIndex, cancellationToken);
        }

        public async Task<IEnumerable<RemoteImageInfo>> GetAvailableRemoteImages(IHasImages item, RemoteImageQuery query, CancellationToken cancellationToken)
        {
            var providers = GetRemoteImageProviders(item, query.IncludeDisabledProviders);

            if (!string.IsNullOrEmpty(query.ProviderName))
            {
                var providerName = query.ProviderName;

                providers = providers.Where(i => string.Equals(i.Name, providerName, StringComparison.OrdinalIgnoreCase));
            }

            var preferredLanguage = item.GetPreferredMetadataLanguage();

            var languages = new List<string>();
            if (!query.IncludeAllLanguages && !string.IsNullOrWhiteSpace(preferredLanguage))
            {
                languages.Add(preferredLanguage);
            }

            var tasks = providers.Select(i => GetImages(item, cancellationToken, i, languages, query.ImageType));

            var results = await Task.WhenAll(tasks).ConfigureAwait(false);

            var images = results.SelectMany(i => i);

            return images;
        }

        /// <summary>
        /// Gets the images.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="provider">The provider.</param>
        /// <param name="preferredLanguages">The preferred languages.</param>
        /// <param name="type">The type.</param>
        /// <returns>Task{IEnumerable{RemoteImageInfo}}.</returns>
        private async Task<IEnumerable<RemoteImageInfo>> GetImages(IHasImages item, CancellationToken cancellationToken, IRemoteImageProvider provider, List<string> preferredLanguages, ImageType? type = null)
        {
            try
            {
                var result = await provider.GetImages(item, cancellationToken).ConfigureAwait(false);

                if (type.HasValue)
                {
                    result = result.Where(i => i.Type == type.Value);
                }

                if (preferredLanguages.Count > 0)
                {
                    result = result.Where(i => string.IsNullOrEmpty(i.Language) ||
                                               preferredLanguages.Contains(i.Language, StringComparer.OrdinalIgnoreCase) ||
                                               string.Equals(i.Language, "en", StringComparison.OrdinalIgnoreCase));
                }

                return result;
            }
            catch (OperationCanceledException)
            {
                return new List<RemoteImageInfo>();
            }
            catch (Exception ex)
            {
                _logger.ErrorException("{0} failed in GetImageInfos for type {1}", ex, provider.GetType().Name, item.GetType().Name);
                return new List<RemoteImageInfo>();
            }
        }

        /// <summary>
        /// Gets the supported image providers.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>IEnumerable{IImageProvider}.</returns>
        public IEnumerable<ImageProviderInfo> GetRemoteImageProviderInfo(IHasImages item)
        {
            return GetRemoteImageProviders(item, true).Select(i => new ImageProviderInfo
            {
                Name = i.Name,
                SupportedImages = i.GetSupportedImages(item).ToList()
            });
        }

        public IEnumerable<IImageProvider> GetImageProviders(IHasImages item)
        {
            return GetImageProviders(item, GetMetadataOptions(item), false);
        }

        private IEnumerable<IImageProvider> GetImageProviders(IHasImages item, MetadataOptions options, bool includeDisabled)
        {
            // Avoid implicitly captured closure
            var currentOptions = options;

            return ImageProviders.Where(i => CanRefresh(i, item, options, includeDisabled))
            .OrderBy(i =>
            {
                // See if there's a user-defined order
                if (!(i is ILocalImageProvider))
                {
                    var index = Array.IndexOf(currentOptions.ImageFetcherOrder, i.Name);

                    if (index != -1)
                    {
                        return index;
                    }
                }

                // Not configured. Just return some high number to put it at the end.
                return 100;
            })
            .ThenBy(GetOrder);
        }

        public IEnumerable<IMetadataProvider<T>> GetMetadataProviders<T>(IHasMetadata item)
            where T : IHasMetadata
        {
            var options = GetMetadataOptions(item);

            return GetMetadataProvidersInternal<T>(item, options, false);
        }

        private IEnumerable<IMetadataProvider<T>> GetMetadataProvidersInternal<T>(IHasMetadata item, MetadataOptions options, bool includeDisabled)
            where T : IHasMetadata
        {
            // Avoid implicitly captured closure
            var currentOptions = options;

            return _metadataProviders.OfType<IMetadataProvider<T>>()
                .Where(i => CanRefresh(i, item, currentOptions, includeDisabled))
                .OrderBy(i => GetConfiguredOrder(i, options))
                .ThenBy(GetDefaultOrder);
        }

        public IEnumerable<IItemIdentityProvider<TLookupInfo, TIdentity>> GetItemIdentityProviders<TLookupInfo, TIdentity>()
            where TLookupInfo : ItemLookupInfo
            where TIdentity : IItemIdentity
        {
            return _identityProviders.OfType<IItemIdentityProvider<TLookupInfo, TIdentity>>();
        }

        public IEnumerable<IItemIdentityConverter<TIdentity>> GetItemIdentityConverters<TIdentity>()
            where TIdentity : IItemIdentity
        {
            return _identityConverters.OfType<IItemIdentityConverter<TIdentity>>();
        }

        private IEnumerable<IRemoteImageProvider> GetRemoteImageProviders(IHasImages item, bool includeDisabled)
        {
            var options = GetMetadataOptions(item);

            return GetImageProviders(item, options, includeDisabled).OfType<IRemoteImageProvider>();
        }

        private bool CanRefresh(IMetadataProvider provider, IHasMetadata item, MetadataOptions options, bool includeDisabled)
        {
            if (!includeDisabled)
            {
                // If locked only allow local providers
                if (item.IsLocked && !(provider is ILocalMetadataProvider) && !(provider is IForcedProvider))
                {
                    return false;
                }

                if (provider is IRemoteMetadataProvider)
                {
                    if (!ConfigurationManager.Configuration.EnableInternetProviders)
                    {
                        return false;
                    }

                    if (Array.IndexOf(options.DisabledMetadataFetchers, provider.Name) != -1)
                    {
                        return false;
                    }
                }
            }

            if (!item.SupportsLocalMetadata && provider is ILocalMetadataProvider)
            {
                return false;
            }

            // If this restriction is ever lifted, movie xml providers will have to be updated to prevent owned items like trailers from reading those files
            if (item.IsOwnedItem)
            {
                if (provider is ILocalMetadataProvider || provider is IRemoteMetadataProvider)
                {
                    return false;
                }
            }

            return true;
        }

        private bool CanRefresh(IImageProvider provider, IHasImages item, MetadataOptions options, bool includeDisabled)
        {
            if (!includeDisabled)
            {
                // If locked only allow local providers
                if (item.IsLocked && !(provider is ILocalImageProvider))
                {
                    return false;
                }

                if (provider is IRemoteImageProvider || provider is IDynamicImageProvider)
                {
                    if (Array.IndexOf(options.DisabledImageFetchers, provider.Name) != -1)
                    {
                        return false;
                    }

                    if (provider is IRemoteImageProvider)
                    {
                        if (!ConfigurationManager.Configuration.EnableInternetProviders)
                        {
                            return false;
                        }
                    }
                }
            }

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
        /// Gets the order.
        /// </summary>
        /// <param name="provider">The provider.</param>
        /// <returns>System.Int32.</returns>
        private int GetOrder(IImageProvider provider)
        {
            var hasOrder = provider as IHasOrder;

            if (hasOrder == null)
            {
                return 0;
            }

            return hasOrder.Order;
        }

        private int GetConfiguredOrder(IMetadataProvider provider, MetadataOptions options)
        {
            // See if there's a user-defined order
            if (provider is ILocalMetadataProvider)
            {
                var index = Array.IndexOf(options.LocalMetadataReaderOrder, provider.Name);

                if (index != -1)
                {
                    return index;
                }
            }

            // See if there's a user-defined order
            if (provider is IRemoteMetadataProvider)
            {
                var index = Array.IndexOf(options.MetadataFetcherOrder, provider.Name);

                if (index != -1)
                {
                    return index;
                }
            }

            // Not configured. Just return some high number to put it at the end.
            return 100;
        }

        private int GetDefaultOrder(IMetadataProvider provider)
        {
            var hasOrder = provider as IHasOrder;

            if (hasOrder != null)
            {
                return hasOrder.Order;
            }

            return 0;
        }

        public IEnumerable<MetadataPluginSummary> GetAllMetadataPlugins()
        {
            var list = new List<MetadataPluginSummary>
            {
                GetPluginSummary<Game>(),
                GetPluginSummary<GameSystem>(),
                GetPluginSummary<Movie>(),
                GetPluginSummary<Trailer>(),
                GetPluginSummary<BoxSet>(),
                GetPluginSummary<Book>(),
                GetPluginSummary<Series>(),
                GetPluginSummary<Season>(),
                GetPluginSummary<Episode>(),
                GetPluginSummary<Person>(),
                GetPluginSummary<MusicAlbum>(),
                GetPluginSummary<MusicArtist>(),
                GetPluginSummary<Audio>(),
                GetPluginSummary<Genre>(),
                GetPluginSummary<Studio>(),
                GetPluginSummary<GameGenre>(),
                GetPluginSummary<MusicGenre>(),
                GetPluginSummary<AdultVideo>(),
                GetPluginSummary<MusicVideo>(),
                GetPluginSummary<Video>(),
                GetPluginSummary<LiveTvChannel>(),
                GetPluginSummary<LiveTvProgram>(),
                GetPluginSummary<LiveTvVideoRecording>(),
                GetPluginSummary<LiveTvAudioRecording>()
            };

            return list;
        }

        private MetadataPluginSummary GetPluginSummary<T>()
            where T : BaseItem, new()
        {
            // Give it a dummy path just so that it looks like a file system item
            var dummy = new T()
            {
                Path = "C:\\",

                // Dummy this up to fool the local trailer check
                Parent = new Folder()
            };

            var options = GetMetadataOptions(dummy);

            var summary = new MetadataPluginSummary
            {
                ItemType = typeof(T).Name
            };

            var imageProviders = GetImageProviders(dummy, options, true).ToList();

            AddMetadataPlugins(summary.Plugins, dummy, options);
            AddImagePlugins(summary.Plugins, dummy, imageProviders);

            var supportedImageTypes = imageProviders.OfType<IRemoteImageProvider>()
                .SelectMany(i => i.GetSupportedImages(dummy))
                .ToList();

            supportedImageTypes.AddRange(imageProviders.OfType<IDynamicImageProvider>()
                .SelectMany(i => i.GetSupportedImages(dummy)));

            summary.SupportedImageTypes = supportedImageTypes.Distinct().ToList();

            return summary;
        }

        private void AddMetadataPlugins<T>(List<MetadataPlugin> list, T item, MetadataOptions options)
            where T : IHasMetadata
        {
            var providers = GetMetadataProvidersInternal<T>(item, options, true).ToList();

            // Locals
            list.AddRange(providers.Where(i => (i is ILocalMetadataProvider)).Select(i => new MetadataPlugin
            {
                Name = i.Name,
                Type = MetadataPluginType.LocalMetadataProvider
            }));

            if (ConfigurationManager.Configuration.EnableInternetProviders)
            {
                // Fetchers
                list.AddRange(providers.Where(i => (i is IRemoteMetadataProvider)).Select(i => new MetadataPlugin
                {
                    Name = i.Name,
                    Type = MetadataPluginType.MetadataFetcher
                }));
            }

            if (item.IsSaveLocalMetadataEnabled())
            {
                // Savers
                list.AddRange(_savers.Where(i => IsSaverEnabledForItem(i, item, ItemUpdateType.MetadataEdit, true)).OrderBy(i => i.Name).Select(i => new MetadataPlugin
                {
                    Name = i.Name,
                    Type = MetadataPluginType.MetadataSaver
                }));
            }
        }

        private void AddImagePlugins<T>(List<MetadataPlugin> list, T item, List<IImageProvider> imageProviders)
            where T : IHasImages
        {

            // Locals
            list.AddRange(imageProviders.Where(i => (i is ILocalImageProvider)).Select(i => new MetadataPlugin
            {
                Name = i.Name,
                Type = MetadataPluginType.LocalImageProvider
            }));

            var enableInternet = ConfigurationManager.Configuration.EnableInternetProviders;

            // Fetchers
            list.AddRange(imageProviders.Where(i => i is IDynamicImageProvider || (enableInternet && i is IRemoteImageProvider)).Select(i => new MetadataPlugin
            {
                Name = i.Name,
                Type = MetadataPluginType.ImageFetcher
            }));
        }

        public MetadataOptions GetMetadataOptions(IHasImages item)
        {
            var type = item.GetType().Name;

            return ConfigurationManager.Configuration.MetadataOptions
                .FirstOrDefault(i => string.Equals(i.ItemType, type, StringComparison.OrdinalIgnoreCase)) ??
                new MetadataOptions();
        }

        private readonly ConcurrentDictionary<string, SemaphoreSlim> _fileLocks = new ConcurrentDictionary<string, SemaphoreSlim>();

        /// <summary>
        /// Saves the metadata.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="updateType">Type of the update.</param>
        /// <returns>Task.</returns>
        public Task SaveMetadata(IHasMetadata item, ItemUpdateType updateType)
        {
            return SaveMetadata(item, updateType, _savers);
        }

        /// <summary>
        /// Saves the metadata.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="updateType">Type of the update.</param>
        /// <param name="savers">The savers.</param>
        /// <returns>Task.</returns>
        public Task SaveMetadata(IHasMetadata item, ItemUpdateType updateType, IEnumerable<string> savers)
        {
            return SaveMetadata(item, updateType, _savers.Where(i => savers.Contains(i.Name, StringComparer.OrdinalIgnoreCase)));
        }

        /// <summary>
        /// Saves the metadata.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="updateType">Type of the update.</param>
        /// <param name="savers">The savers.</param>
        /// <returns>Task.</returns>
        private async Task SaveMetadata(IHasMetadata item, ItemUpdateType updateType, IEnumerable<IMetadataSaver> savers)
        {
            foreach (var saver in savers.Where(i => IsSaverEnabledForItem(i, item, updateType, false)))
            {
                _logger.Debug("Saving {0} to {1}.", item.Path ?? item.Name, saver.Name);

                var fileSaver = saver as IMetadataFileSaver;

                if (fileSaver != null)
                {
                    string path = null;

                    try
                    {
                        path = fileSaver.GetSavePath(item);
                    }
                    catch (Exception ex)
                    {
                        _logger.ErrorException("Error in {0} GetSavePath", ex, saver.Name);
                        continue;
                    }

                    var semaphore = _fileLocks.GetOrAdd(path, key => new SemaphoreSlim(1, 1));

                    await semaphore.WaitAsync().ConfigureAwait(false);

                    try
                    {
                        _libraryMonitor.ReportFileSystemChangeBeginning(path);
                        saver.Save(item, CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        _logger.ErrorException("Error in metadata saver", ex);
                    }
                    finally
                    {
                        _libraryMonitor.ReportFileSystemChangeComplete(path, false);
                        semaphore.Release();
                    }
                }
                else
                {
                    try
                    {
                        saver.Save(item, CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        _logger.ErrorException("Error in metadata saver", ex);
                    }
                }
            }
        }

        /// <summary>
        /// Determines whether [is saver enabled for item] [the specified saver].
        /// </summary>
        /// <param name="saver">The saver.</param>
        /// <param name="item">The item.</param>
        /// <param name="updateType">Type of the update.</param>
        /// <param name="includeDisabled">if set to <c>true</c> [include disabled].</param>
        /// <returns><c>true</c> if [is saver enabled for item] [the specified saver]; otherwise, <c>false</c>.</returns>
        private bool IsSaverEnabledForItem(IMetadataSaver saver, IHasMetadata item, ItemUpdateType updateType, bool includeDisabled)
        {
            var options = GetMetadataOptions(item);

            try
            {
                var isEnabledFor = saver.IsEnabledFor(item, updateType);

                if (!includeDisabled)
                {
                    if (options.DisabledMetadataSavers.Contains(saver.Name, StringComparer.OrdinalIgnoreCase))
                    {
                        return false;
                    }

                    if (!item.IsSaveLocalMetadataEnabled())
                    {
                        if (updateType >= ItemUpdateType.MetadataEdit)
                        {
                            var fileSaver = saver as IMetadataFileSaver;

                            // Manual edit occurred
                            // Even if save local is off, save locally anyway if the metadata file already exists
                            if (fileSaver == null || !isEnabledFor || !File.Exists(fileSaver.GetSavePath(item)))
                            {
                                return false;
                            }
                        }
                        else
                        {
                            // Manual edit did not occur
                            // Since local metadata saving is disabled, consider it disabled
                            return false;
                        }
                    }
                }

                return isEnabledFor;
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error in {0}.IsEnabledFor", ex, saver.Name);
                return false;
            }
        }

        public async Task<IEnumerable<RemoteSearchResult>> GetRemoteSearchResults<TItemType, TLookupType>(RemoteSearchQuery<TLookupType> searchInfo,
            CancellationToken cancellationToken)
            where TItemType : BaseItem, new()
            where TLookupType : ItemLookupInfo
        {
            // Give it a dummy path just so that it looks like a file system item
            var dummy = new TItemType
            {
                Path = "C:\\",

                // Dummy this up to fool the local trailer check
                Parent = new Folder()
            };

            var options = GetMetadataOptions(dummy);

            var providers = GetMetadataProvidersInternal<TItemType>(dummy, options, searchInfo.IncludeDisabledProviders)
                .OfType<IRemoteSearchProvider<TLookupType>>();

            if (!string.IsNullOrEmpty(searchInfo.SearchProviderName))
            {
                providers = providers.Where(i => string.Equals(i.Name, searchInfo.SearchProviderName, StringComparison.OrdinalIgnoreCase));
            }

            if (string.IsNullOrWhiteSpace(searchInfo.SearchInfo.MetadataLanguage))
            {
                searchInfo.SearchInfo.MetadataLanguage = ConfigurationManager.Configuration.PreferredMetadataLanguage;
            }
            if (string.IsNullOrWhiteSpace(searchInfo.SearchInfo.MetadataCountryCode))
            {
                searchInfo.SearchInfo.MetadataCountryCode = ConfigurationManager.Configuration.MetadataCountryCode;
            }

            foreach (var provider in providers)
            {
                var results = await GetSearchResults(provider, searchInfo.SearchInfo, cancellationToken).ConfigureAwait(false);

                var list = results.ToList();

                if (list.Count > 0)
                {
                    return list.Take(10);
                }
            }

            // Nothing found
            return new List<RemoteSearchResult>();
        }

        private async Task<IEnumerable<RemoteSearchResult>> GetSearchResults<TLookupType>(IRemoteSearchProvider<TLookupType> provider, TLookupType searchInfo,
            CancellationToken cancellationToken)
            where TLookupType : ItemLookupInfo
        {
            var results = await provider.GetSearchResults(searchInfo, cancellationToken).ConfigureAwait(false);

            var list = results.ToList();

            foreach (var item in list)
            {
                item.SearchProviderName = provider.Name;
            }

            return list;
        }

        public Task<HttpResponseInfo> GetSearchImage(string providerName, string url, CancellationToken cancellationToken)
        {
            var provider = _metadataProviders.OfType<IRemoteSearchProvider>().FirstOrDefault(i => string.Equals(i.Name, providerName, StringComparison.OrdinalIgnoreCase));

            if (provider == null)
            {
                throw new ArgumentException("Search provider not found.");
            }

            return provider.GetImageResponse(url, cancellationToken);
        }

        public IEnumerable<IExternalId> GetExternalIds(IHasProviderIds item)
        {
            return _externalIds.Where(i =>
            {
                try
                {
                    return i.Supports(item);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error in {0}.Suports", ex, i.GetType().Name);
                    return false;
                }
            });
        }

        public IEnumerable<ExternalUrl> GetExternalUrls(IHasProviderIds item)
        {
            return GetExternalIds(item)
                .Select(i =>
            {
                if (string.IsNullOrEmpty(i.UrlFormatString))
                {
                    return null;
                }

                var value = item.GetProviderId(i.Key);

                if (string.IsNullOrEmpty(value))
                {
                    return null;
                }

                return new ExternalUrl
                {
                    Name = i.Name,
                    Url = string.Format(i.UrlFormatString, value)
                };

            }).Where(i => i != null);
        }

        public IEnumerable<ExternalIdInfo> GetExternalIdInfos(IHasProviderIds item)
        {
            return GetExternalIds(item)
                .Select(i => new ExternalIdInfo
                {
                    Name = i.Name,
                    Key = i.Key,
                    UrlFormatString = i.UrlFormatString

                });
        }
    }
}