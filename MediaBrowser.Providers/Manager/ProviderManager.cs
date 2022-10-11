#nullable disable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Data.Events;
using Jellyfin.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Progress;
using MediaBrowser.Controller;
using MediaBrowser.Controller.BaseItemManager;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Subtitles;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Extensions;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;
using Priority_Queue;
using Book = MediaBrowser.Controller.Entities.Book;
using Episode = MediaBrowser.Controller.Entities.TV.Episode;
using Movie = MediaBrowser.Controller.Entities.Movies.Movie;
using MusicAlbum = MediaBrowser.Controller.Entities.Audio.MusicAlbum;
using Season = MediaBrowser.Controller.Entities.TV.Season;
using Series = MediaBrowser.Controller.Entities.TV.Series;

namespace MediaBrowser.Providers.Manager
{
    /// <summary>
    /// Class ProviderManager.
    /// </summary>
    public class ProviderManager : IProviderManager, IDisposable
    {
        private readonly object _refreshQueueLock = new object();
        private readonly ILogger<ProviderManager> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILibraryMonitor _libraryMonitor;
        private readonly IFileSystem _fileSystem;
        private readonly IServerApplicationPaths _appPaths;
        private readonly ILibraryManager _libraryManager;
        private readonly ISubtitleManager _subtitleManager;
        private readonly IServerConfigurationManager _configurationManager;
        private readonly IBaseItemManager _baseItemManager;
        private readonly ConcurrentDictionary<Guid, double> _activeRefreshes = new ConcurrentDictionary<Guid, double>();
        private readonly CancellationTokenSource _disposeCancellationTokenSource = new CancellationTokenSource();
        private readonly SimplePriorityQueue<Tuple<Guid, MetadataRefreshOptions>> _refreshQueue =
            new SimplePriorityQueue<Tuple<Guid, MetadataRefreshOptions>>();

        private IMetadataService[] _metadataServices = Array.Empty<IMetadataService>();
        private IMetadataProvider[] _metadataProviders = Array.Empty<IMetadataProvider>();
        private IMetadataSaver[] _savers = Array.Empty<IMetadataSaver>();
        private IExternalId[] _externalIds = Array.Empty<IExternalId>();
        private bool _isProcessingRefreshQueue;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProviderManager"/> class.
        /// </summary>
        /// <param name="httpClientFactory">The Http client factory.</param>
        /// <param name="subtitleManager">The subtitle manager.</param>
        /// <param name="configurationManager">The configuration manager.</param>
        /// <param name="libraryMonitor">The library monitor.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="fileSystem">The filesystem.</param>
        /// <param name="appPaths">The server application paths.</param>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="baseItemManager">The BaseItem manager.</param>
        public ProviderManager(
            IHttpClientFactory httpClientFactory,
            ISubtitleManager subtitleManager,
            IServerConfigurationManager configurationManager,
            ILibraryMonitor libraryMonitor,
            ILogger<ProviderManager> logger,
            IFileSystem fileSystem,
            IServerApplicationPaths appPaths,
            ILibraryManager libraryManager,
            IBaseItemManager baseItemManager)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _configurationManager = configurationManager;
            _libraryMonitor = libraryMonitor;
            _fileSystem = fileSystem;
            _appPaths = appPaths;
            _libraryManager = libraryManager;
            _subtitleManager = subtitleManager;
            _baseItemManager = baseItemManager;
        }

        /// <inheritdoc/>
        public event EventHandler<GenericEventArgs<BaseItem>> RefreshStarted;

        /// <inheritdoc/>
        public event EventHandler<GenericEventArgs<BaseItem>> RefreshCompleted;

        /// <inheritdoc/>
        public event EventHandler<GenericEventArgs<Tuple<BaseItem, double>>> RefreshProgress;

        private IImageProvider[] ImageProviders { get; set; }

        /// <inheritdoc/>
        public void AddParts(
            IEnumerable<IImageProvider> imageProviders,
            IEnumerable<IMetadataService> metadataServices,
            IEnumerable<IMetadataProvider> metadataProviders,
            IEnumerable<IMetadataSaver> metadataSavers,
            IEnumerable<IExternalId> externalIds)
        {
            ImageProviders = imageProviders.ToArray();

            _metadataServices = metadataServices.OrderBy(i => i.Order).ToArray();
            _metadataProviders = metadataProviders.ToArray();
            _externalIds = externalIds.OrderBy(i => i.ProviderName).ToArray();

            _savers = metadataSavers.ToArray();
        }

        /// <inheritdoc/>
        public Task<ItemUpdateType> RefreshSingleItem(BaseItem item, MetadataRefreshOptions options, CancellationToken cancellationToken)
        {
            var type = item.GetType();

            var service = _metadataServices.FirstOrDefault(current => current.CanRefreshPrimary(type));

            if (service == null)
            {
                foreach (var current in _metadataServices)
                {
                    if (current.CanRefresh(item))
                    {
                        service = current;
                        break;
                    }
                }
            }

            if (service != null)
            {
                return service.RefreshMetadata(item, options, cancellationToken);
            }

            _logger.LogError("Unable to find a metadata service for item of type {TypeName}", item.GetType().Name);
            return Task.FromResult(ItemUpdateType.None);
        }

        /// <inheritdoc/>
        public async Task SaveImage(BaseItem item, string url, ImageType type, int? imageIndex, CancellationToken cancellationToken)
        {
            var httpClient = _httpClientFactory.CreateClient(NamedClient.Default);
            using var response = await httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new HttpRequestException("Invalid image received.", null, response.StatusCode);
            }

            var contentType = response.Content.Headers.ContentType?.MediaType;

            // Workaround for tvheadend channel icons
            // TODO: Isolate this hack into the tvh plugin
            if (string.IsNullOrEmpty(contentType))
            {
                if (url.IndexOf("/imagecache/", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    contentType = "image/png";
                }
            }

            // thetvdb will sometimes serve a rubbish 404 html page with a 200 OK code, because reasons...
            if (contentType.Equals(MediaTypeNames.Text.Html, StringComparison.OrdinalIgnoreCase))
            {
                throw new HttpRequestException("Invalid image received.", null, HttpStatusCode.NotFound);
            }

            // some iptv/epg providers don't correctly report media type, extract from url if no extension found
            if (string.IsNullOrWhiteSpace(MimeTypes.ToExtension(contentType)))
            {
                contentType = MimeTypes.GetMimeType(url);
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            await SaveImage(
                item,
                stream,
                contentType,
                type,
                imageIndex,
                cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public Task SaveImage(BaseItem item, Stream source, string mimeType, ImageType type, int? imageIndex, CancellationToken cancellationToken)
        {
            return new ImageSaver(_configurationManager, _libraryMonitor, _fileSystem, _logger).SaveImage(item, source, mimeType, type, imageIndex, cancellationToken);
        }

        /// <inheritdoc/>
        public Task SaveImage(BaseItem item, string source, string mimeType, ImageType type, int? imageIndex, bool? saveLocallyWithMedia, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                throw new ArgumentNullException(nameof(source));
            }

            var fileStream = AsyncFile.OpenRead(source);
            return new ImageSaver(_configurationManager, _libraryMonitor, _fileSystem, _logger).SaveImage(item, fileStream, mimeType, type, imageIndex, saveLocallyWithMedia, cancellationToken);
        }

        /// <inheritdoc/>
        public Task SaveImage(Stream source, string mimeType, string path)
        {
            return new ImageSaver(_configurationManager, _libraryMonitor, _fileSystem, _logger)
                .SaveImage(source, path);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<RemoteImageInfo>> GetAvailableRemoteImages(BaseItem item, RemoteImageQuery query, CancellationToken cancellationToken)
        {
            var providers = GetRemoteImageProviders(item, query.IncludeDisabledProviders);

            if (!string.IsNullOrEmpty(query.ProviderName))
            {
                var providerName = query.ProviderName;

                providers = providers.Where(i => string.Equals(i.Name, providerName, StringComparison.OrdinalIgnoreCase));
            }

            var preferredLanguage = item.GetPreferredMetadataLanguage();

            var tasks = providers.Select(i => GetImages(item, i, preferredLanguage, query.IncludeAllLanguages, cancellationToken, query.ImageType));

            var results = await Task.WhenAll(tasks).ConfigureAwait(false);

            return results.SelectMany(i => i.ToList());
        }

        /// <summary>
        /// Gets the images.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="provider">The provider.</param>
        /// <param name="preferredLanguage">The preferred language.</param>
        /// <param name="includeAllLanguages">Whether to include all languages in results.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="type">The type.</param>
        /// <returns>Task{IEnumerable{RemoteImageInfo}}.</returns>
        private async Task<IEnumerable<RemoteImageInfo>> GetImages(
            BaseItem item,
            IRemoteImageProvider provider,
            string preferredLanguage,
            bool includeAllLanguages,
            CancellationToken cancellationToken,
            ImageType? type = null)
        {
            bool hasPreferredLanguage = !string.IsNullOrWhiteSpace(preferredLanguage);

            try
            {
                var result = await provider.GetImages(item, cancellationToken).ConfigureAwait(false);

                if (type.HasValue)
                {
                    result = result.Where(i => i.Type == type.Value);
                }

                if (!includeAllLanguages && hasPreferredLanguage)
                {
                    // Filter out languages that do not match the preferred languages.
                    //
                    // TODO: should exception case of "en" (English) eventually be removed?
                    result = result.Where(i => string.IsNullOrWhiteSpace(i.Language) ||
                                               string.Equals(preferredLanguage, i.Language, StringComparison.OrdinalIgnoreCase) ||
                                               string.Equals(i.Language, "en", StringComparison.OrdinalIgnoreCase));
                }

                return result.OrderByLanguageDescending(preferredLanguage);
            }
            catch (OperationCanceledException)
            {
                return new List<RemoteImageInfo>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{ProviderName} failed in GetImageInfos for type {ItemType} at {ItemPath}", provider.GetType().Name, item.GetType().Name, item.Path);
                return new List<RemoteImageInfo>();
            }
        }

        /// <inheritdoc/>
        public IEnumerable<ImageProviderInfo> GetRemoteImageProviderInfo(BaseItem item)
        {
            return GetRemoteImageProviders(item, true).Select(i => new ImageProviderInfo(i.Name, i.GetSupportedImages(item).ToArray()));
        }

        /// <summary>
        /// Gets the image providers for the provided item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="refreshOptions">The image refresh options.</param>
        /// <returns>The image providers for the item.</returns>
        public IEnumerable<IImageProvider> GetImageProviders(BaseItem item, ImageRefreshOptions refreshOptions)
        {
            return GetImageProviders(item, _libraryManager.GetLibraryOptions(item), GetMetadataOptions(item), refreshOptions, false);
        }

        private IEnumerable<IImageProvider> GetImageProviders(BaseItem item, LibraryOptions libraryOptions, MetadataOptions options, ImageRefreshOptions refreshOptions, bool includeDisabled)
        {
            // Avoid implicitly captured closure
            var currentOptions = options;

            var typeOptions = libraryOptions.GetTypeOptions(item.GetType().Name);
            var typeFetcherOrder = typeOptions?.ImageFetcherOrder;

            return ImageProviders.Where(i => CanRefresh(i, item, libraryOptions, refreshOptions, includeDisabled))
                .OrderBy(i =>
                {
                    // See if there's a user-defined order
                    if (i is not ILocalImageProvider)
                    {
                        var fetcherOrder = typeFetcherOrder ?? currentOptions.ImageFetcherOrder;
                        var index = Array.IndexOf(fetcherOrder, i.Name);

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

        /// <summary>
        /// Gets the metadata providers for the provided item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="libraryOptions">The library options.</param>
        /// <typeparam name="T">The type of metadata provider.</typeparam>
        /// <returns>The metadata providers.</returns>
        public IEnumerable<IMetadataProvider<T>> GetMetadataProviders<T>(BaseItem item, LibraryOptions libraryOptions)
            where T : BaseItem
        {
            var globalMetadataOptions = GetMetadataOptions(item);

            return GetMetadataProvidersInternal<T>(item, libraryOptions, globalMetadataOptions, false, false);
        }

        private IEnumerable<IMetadataProvider<T>> GetMetadataProvidersInternal<T>(BaseItem item, LibraryOptions libraryOptions, MetadataOptions globalMetadataOptions, bool includeDisabled, bool forceEnableInternetMetadata)
            where T : BaseItem
        {
            // Avoid implicitly captured closure
            var currentOptions = globalMetadataOptions;

            return _metadataProviders.OfType<IMetadataProvider<T>>()
                .Where(i => CanRefresh(i, item, libraryOptions, includeDisabled, forceEnableInternetMetadata))
                .OrderBy(i => GetConfiguredOrder(item, i, libraryOptions, globalMetadataOptions))
                .ThenBy(GetDefaultOrder);
        }

        private IEnumerable<IRemoteImageProvider> GetRemoteImageProviders(BaseItem item, bool includeDisabled)
        {
            var options = GetMetadataOptions(item);
            var libraryOptions = _libraryManager.GetLibraryOptions(item);

            return GetImageProviders(
                item,
                libraryOptions,
                options,
                new ImageRefreshOptions(new DirectoryService(_fileSystem)),
                includeDisabled).OfType<IRemoteImageProvider>();
        }

        private bool CanRefresh(
            IMetadataProvider provider,
            BaseItem item,
            LibraryOptions libraryOptions,
            bool includeDisabled,
            bool forceEnableInternetMetadata)
        {
            if (!includeDisabled)
            {
                // If locked only allow local providers
                if (item.IsLocked && provider is not ILocalMetadataProvider && provider is not IForcedProvider)
                {
                    return false;
                }

                if (provider is IRemoteMetadataProvider)
                {
                    if (!forceEnableInternetMetadata && !_baseItemManager.IsMetadataFetcherEnabled(item, libraryOptions, provider.Name))
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
            if (!item.OwnerId.Equals(default))
            {
                if (provider is ILocalMetadataProvider || provider is IRemoteMetadataProvider)
                {
                    return false;
                }
            }

            return true;
        }

        private bool CanRefresh(
            IImageProvider provider,
            BaseItem item,
            LibraryOptions libraryOptions,
            ImageRefreshOptions refreshOptions,
            bool includeDisabled)
        {
            if (!includeDisabled)
            {
                // If locked only allow local providers
                if (item.IsLocked && provider is not ILocalImageProvider)
                {
                    if (refreshOptions.ImageRefreshMode != MetadataRefreshMode.FullRefresh)
                    {
                        return false;
                    }
                }

                if (provider is IRemoteImageProvider || provider is IDynamicImageProvider)
                {
                    if (!_baseItemManager.IsImageFetcherEnabled(item, libraryOptions, provider.Name))
                    {
                        return false;
                    }
                }
            }

            try
            {
                return provider.Supports(item);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{ProviderName} failed in Supports for type {ItemType} at {ItemPath}", provider.GetType().Name, item.GetType().Name, item.Path);
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
            if (provider is not IHasOrder hasOrder)
            {
                return 0;
            }

            return hasOrder.Order;
        }

        private int GetConfiguredOrder(BaseItem item, IMetadataProvider provider, LibraryOptions libraryOptions, MetadataOptions globalMetadataOptions)
        {
            // See if there's a user-defined order
            if (provider is ILocalMetadataProvider)
            {
                var configuredOrder = libraryOptions.LocalMetadataReaderOrder ?? globalMetadataOptions.LocalMetadataReaderOrder;

                var index = Array.IndexOf(configuredOrder, provider.Name);

                if (index != -1)
                {
                    return index;
                }
            }

            // See if there's a user-defined order
            if (provider is IRemoteMetadataProvider)
            {
                var typeOptions = libraryOptions.GetTypeOptions(item.GetType().Name);
                var typeFetcherOrder = typeOptions?.MetadataFetcherOrder;

                var fetcherOrder = typeFetcherOrder ?? globalMetadataOptions.MetadataFetcherOrder;

                var index = Array.IndexOf(fetcherOrder, provider.Name);

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
            if (provider is IHasOrder hasOrder)
            {
                return hasOrder.Order;
            }

            return 0;
        }

        /// <inheritdoc/>
        public MetadataPluginSummary[] GetAllMetadataPlugins()
        {
            return new[]
            {
                GetPluginSummary<Movie>(),
                GetPluginSummary<BoxSet>(),
                GetPluginSummary<Book>(),
                GetPluginSummary<Series>(),
                GetPluginSummary<Season>(),
                GetPluginSummary<Episode>(),
                GetPluginSummary<MusicAlbum>(),
                GetPluginSummary<MusicArtist>(),
                GetPluginSummary<Audio>(),
                GetPluginSummary<AudioBook>(),
                GetPluginSummary<Studio>(),
                GetPluginSummary<MusicVideo>(),
                GetPluginSummary<Video>()
            };
        }

        private MetadataPluginSummary GetPluginSummary<T>()
            where T : BaseItem, new()
        {
            // Give it a dummy path just so that it looks like a file system item
            var dummy = new T
            {
                Path = Path.Combine(_appPaths.InternalMetadataPath, "dummy"),
                ParentId = Guid.NewGuid()
            };

            var options = GetMetadataOptions(dummy);

            var summary = new MetadataPluginSummary
            {
                ItemType = typeof(T).Name
            };

            var libraryOptions = new LibraryOptions();

            var imageProviders = GetImageProviders(
                dummy,
                libraryOptions,
                options,
                new ImageRefreshOptions(new DirectoryService(_fileSystem)),
                true).ToList();

            var pluginList = summary.Plugins.ToList();

            AddMetadataPlugins(pluginList, dummy, libraryOptions, options);
            AddImagePlugins(pluginList, imageProviders);

            var subtitleProviders = _subtitleManager.GetSupportedProviders(dummy);

            // Subtitle fetchers
            pluginList.AddRange(subtitleProviders.Select(i => new MetadataPlugin
            {
                Name = i.Name,
                Type = MetadataPluginType.SubtitleFetcher
            }));

            summary.Plugins = pluginList.ToArray();

            var supportedImageTypes = imageProviders.OfType<IRemoteImageProvider>()
                .SelectMany(i => i.GetSupportedImages(dummy))
                .ToList();

            supportedImageTypes.AddRange(imageProviders.OfType<IDynamicImageProvider>()
                .SelectMany(i => i.GetSupportedImages(dummy)));

            summary.SupportedImageTypes = supportedImageTypes.Distinct().ToArray();

            return summary;
        }

        private void AddMetadataPlugins<T>(List<MetadataPlugin> list, T item, LibraryOptions libraryOptions, MetadataOptions options)
            where T : BaseItem
        {
            var providers = GetMetadataProvidersInternal<T>(item, libraryOptions, options, true, true).ToList();

            // Locals
            list.AddRange(providers.Where(i => i is ILocalMetadataProvider).Select(i => new MetadataPlugin
            {
                Name = i.Name,
                Type = MetadataPluginType.LocalMetadataProvider
            }));

            // Fetchers
            list.AddRange(providers.Where(i => i is IRemoteMetadataProvider).Select(i => new MetadataPlugin
            {
                Name = i.Name,
                Type = MetadataPluginType.MetadataFetcher
            }));

            // Savers
            list.AddRange(_savers.Where(i => IsSaverEnabledForItem(i, item, libraryOptions, ItemUpdateType.MetadataEdit, true)).OrderBy(i => i.Name).Select(i => new MetadataPlugin
            {
                Name = i.Name,
                Type = MetadataPluginType.MetadataSaver
            }));
        }

        private void AddImagePlugins(List<MetadataPlugin> list, List<IImageProvider> imageProviders)
        {
            // Locals
            list.AddRange(imageProviders.Where(i => i is ILocalImageProvider).Select(i => new MetadataPlugin
            {
                Name = i.Name,
                Type = MetadataPluginType.LocalImageProvider
            }));

            // Fetchers
            list.AddRange(imageProviders.Where(i => i is IDynamicImageProvider || (i is IRemoteImageProvider)).Select(i => new MetadataPlugin
            {
                Name = i.Name,
                Type = MetadataPluginType.ImageFetcher
            }));
        }

        /// <inheritdoc/>
        public MetadataOptions GetMetadataOptions(BaseItem item)
        {
            var type = item.GetType().Name;

            return _configurationManager.Configuration.MetadataOptions
                .FirstOrDefault(i => string.Equals(i.ItemType, type, StringComparison.OrdinalIgnoreCase)) ??
                new MetadataOptions();
        }

        /// <inheritdoc/>
        public Task SaveMetadataAsync(BaseItem item, ItemUpdateType updateType)
            => SaveMetadataAsync(item, updateType, _savers);

        /// <inheritdoc/>
        public Task SaveMetadataAsync(BaseItem item, ItemUpdateType updateType, IEnumerable<string> savers)
            => SaveMetadataAsync(item, updateType, _savers.Where(i => savers.Contains(i.Name, StringComparison.OrdinalIgnoreCase)));

        /// <summary>
        /// Saves the metadata.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="updateType">Type of the update.</param>
        /// <param name="savers">The savers.</param>
        private async Task SaveMetadataAsync(BaseItem item, ItemUpdateType updateType, IEnumerable<IMetadataSaver> savers)
        {
            var libraryOptions = _libraryManager.GetLibraryOptions(item);

            foreach (var saver in savers.Where(i => IsSaverEnabledForItem(i, item, libraryOptions, updateType, false)))
            {
                _logger.LogDebug("Saving {0} to {1}.", item.Path ?? item.Name, saver.Name);

                if (saver is IMetadataFileSaver fileSaver)
                {
                    string path;

                    try
                    {
                        path = fileSaver.GetSavePath(item);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in {0} GetSavePath", saver.Name);
                        continue;
                    }

                    try
                    {
                        _libraryMonitor.ReportFileSystemChangeBeginning(path);
                        await saver.SaveAsync(item, CancellationToken.None).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in metadata saver");
                    }
                    finally
                    {
                        _libraryMonitor.ReportFileSystemChangeComplete(path, false);
                    }
                }
                else
                {
                    try
                    {
                        await saver.SaveAsync(item, CancellationToken.None).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in metadata saver");
                    }
                }
            }
        }

        /// <summary>
        /// Determines whether [is saver enabled for item] [the specified saver].
        /// </summary>
        private bool IsSaverEnabledForItem(IMetadataSaver saver, BaseItem item, LibraryOptions libraryOptions, ItemUpdateType updateType, bool includeDisabled)
        {
            var options = GetMetadataOptions(item);

            try
            {
                if (!saver.IsEnabledFor(item, updateType))
                {
                    return false;
                }

                if (!includeDisabled)
                {
                    if (libraryOptions.MetadataSavers == null)
                    {
                        if (options.DisabledMetadataSavers.Contains(saver.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            return false;
                        }

                        if (!item.IsSaveLocalMetadataEnabled())
                        {
                            if (updateType >= ItemUpdateType.MetadataEdit)
                            {
                                // Manual edit occurred
                                // Even if save local is off, save locally anyway if the metadata file already exists
                                if (saver is not IMetadataFileSaver fileSaver || !File.Exists(fileSaver.GetSavePath(item)))
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
                    else
                    {
                        if (!libraryOptions.MetadataSavers.Contains(saver.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            return false;
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in {0}.IsEnabledFor", saver.Name);
                return false;
            }
        }

        /// <inheritdoc/>
        public Task<IEnumerable<RemoteSearchResult>> GetRemoteSearchResults<TItemType, TLookupType>(RemoteSearchQuery<TLookupType> searchInfo, CancellationToken cancellationToken)
            where TItemType : BaseItem, new()
            where TLookupType : ItemLookupInfo
        {
            BaseItem referenceItem = null;

            if (!searchInfo.ItemId.Equals(default))
            {
                referenceItem = _libraryManager.GetItemById(searchInfo.ItemId);
            }

            return GetRemoteSearchResults<TItemType, TLookupType>(searchInfo, referenceItem, cancellationToken);
        }

        private async Task<IEnumerable<RemoteSearchResult>> GetRemoteSearchResults<TItemType, TLookupType>(RemoteSearchQuery<TLookupType> searchInfo, BaseItem referenceItem, CancellationToken cancellationToken)
            where TItemType : BaseItem, new()
            where TLookupType : ItemLookupInfo
        {
            LibraryOptions libraryOptions;

            if (referenceItem == null)
            {
                // Give it a dummy path just so that it looks like a file system item
                var dummy = new TItemType
                {
                    Path = Path.Combine(_appPaths.InternalMetadataPath, "dummy"),
                    ParentId = Guid.NewGuid()
                };

                dummy.SetParent(new Folder());

                referenceItem = dummy;
                libraryOptions = new LibraryOptions();
            }
            else
            {
                libraryOptions = _libraryManager.GetLibraryOptions(referenceItem);
            }

            var options = GetMetadataOptions(referenceItem);

            var providers = GetMetadataProvidersInternal<TItemType>(referenceItem, libraryOptions, options, searchInfo.IncludeDisabledProviders, false)
                .OfType<IRemoteSearchProvider<TLookupType>>();

            if (!string.IsNullOrEmpty(searchInfo.SearchProviderName))
            {
                providers = providers.Where(i => string.Equals(i.Name, searchInfo.SearchProviderName, StringComparison.OrdinalIgnoreCase));
            }

            if (string.IsNullOrWhiteSpace(searchInfo.SearchInfo.MetadataLanguage))
            {
                searchInfo.SearchInfo.MetadataLanguage = _configurationManager.Configuration.PreferredMetadataLanguage;
            }

            if (string.IsNullOrWhiteSpace(searchInfo.SearchInfo.MetadataCountryCode))
            {
                searchInfo.SearchInfo.MetadataCountryCode = _configurationManager.Configuration.MetadataCountryCode;
            }

            var resultList = new List<RemoteSearchResult>();

            foreach (var provider in providers)
            {
                try
                {
                    var results = await GetSearchResults(provider, searchInfo.SearchInfo, cancellationToken).ConfigureAwait(false);

                    foreach (var result in results)
                    {
                        var existingMatch = resultList.FirstOrDefault(i => i.ProviderIds.Any(p => string.Equals(result.GetProviderId(p.Key), p.Value, StringComparison.OrdinalIgnoreCase)));

                        if (existingMatch == null)
                        {
                            resultList.Add(result);
                        }
                        else
                        {
                            foreach (var providerId in result.ProviderIds)
                            {
                                if (!existingMatch.ProviderIds.ContainsKey(providerId.Key))
                                {
                                    existingMatch.ProviderIds.Add(providerId.Key, providerId.Value);
                                }
                            }

                            if (string.IsNullOrWhiteSpace(existingMatch.ImageUrl))
                            {
                                existingMatch.ImageUrl = result.ImageUrl;
                            }
                        }
                    }
                }
#pragma warning disable CA1031 // do not catch general exception types
                catch (Exception ex)
#pragma warning restore CA1031 // do not catch general exception types
                {
                    _logger.LogError(ex, "Provider {ProviderName} failed to retrieve search results", provider.Name);
                }
            }

            return resultList;
        }

        private async Task<IEnumerable<RemoteSearchResult>> GetSearchResults<TLookupType>(
            IRemoteSearchProvider<TLookupType> provider,
            TLookupType searchInfo,
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

        /// <inheritdoc/>
        public Task<HttpResponseMessage> GetSearchImage(string providerName, string url, CancellationToken cancellationToken)
        {
            var provider = _metadataProviders.OfType<IRemoteSearchProvider>().FirstOrDefault(i => string.Equals(i.Name, providerName, StringComparison.OrdinalIgnoreCase));

            if (provider == null)
            {
                throw new ArgumentException("Search provider not found.");
            }

            return provider.GetImageResponse(url, cancellationToken);
        }

        private IEnumerable<IExternalId> GetExternalIds(IHasProviderIds item)
        {
            return _externalIds.Where(i =>
            {
                try
                {
                    return i.Supports(item);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in {0}.Supports", i.GetType().Name);
                    return false;
                }
            });
        }

        /// <inheritdoc/>
        public IEnumerable<ExternalUrl> GetExternalUrls(BaseItem item)
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
                    Name = i.ProviderName,
                    Url = string.Format(
                        CultureInfo.InvariantCulture,
                        i.UrlFormatString,
                        value)
                };
            }).Where(i => i != null).Concat(item.GetRelatedUrls());
        }

        /// <inheritdoc/>
        public IEnumerable<ExternalIdInfo> GetExternalIdInfos(IHasProviderIds item)
        {
            return GetExternalIds(item)
                .Select(i => new ExternalIdInfo(
                    name: i.ProviderName,
                    key: i.Key,
                    type: i.Type,
                    urlFormatString: i.UrlFormatString));
        }

        /// <inheritdoc/>
        public Dictionary<Guid, Guid> GetRefreshQueue()
        {
            lock (_refreshQueueLock)
            {
                var dict = new Dictionary<Guid, Guid>();

                foreach (var item in _refreshQueue)
                {
                    dict[item.Item1] = item.Item1;
                }

                return dict;
            }
        }

        /// <inheritdoc/>
        public void OnRefreshStart(BaseItem item)
        {
            _logger.LogDebug("OnRefreshStart {0}", item.Id.ToString("N", CultureInfo.InvariantCulture));
            _activeRefreshes[item.Id] = 0;
            RefreshStarted?.Invoke(this, new GenericEventArgs<BaseItem>(item));
        }

        /// <inheritdoc/>
        public void OnRefreshComplete(BaseItem item)
        {
            _logger.LogDebug("OnRefreshComplete {0}", item.Id.ToString("N", CultureInfo.InvariantCulture));

            _activeRefreshes.Remove(item.Id, out _);

            RefreshCompleted?.Invoke(this, new GenericEventArgs<BaseItem>(item));
        }

        /// <inheritdoc/>
        public double? GetRefreshProgress(Guid id)
        {
            if (_activeRefreshes.TryGetValue(id, out double value))
            {
                return value;
            }

            return null;
        }

        /// <inheritdoc/>
        public void OnRefreshProgress(BaseItem item, double progress)
        {
            var id = item.Id;
            _logger.LogDebug("OnRefreshProgress {0} {1}", id.ToString("N", CultureInfo.InvariantCulture), progress);

            // TODO: Need to hunt down the conditions for this happening
            _activeRefreshes.AddOrUpdate(
                id,
                (_) => throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Cannot update refresh progress of item '{0}' ({1}) because a refresh for this item is not running",
                        item.GetType().Name,
                        item.Id.ToString("N", CultureInfo.InvariantCulture))),
                (_, _) => progress);

            RefreshProgress?.Invoke(this, new GenericEventArgs<Tuple<BaseItem, double>>(new Tuple<BaseItem, double>(item, progress)));
        }

        /// <inheritdoc/>
        public void QueueRefresh(Guid itemId, MetadataRefreshOptions options, RefreshPriority priority)
        {
            if (_disposed)
            {
                return;
            }

            _refreshQueue.Enqueue(new Tuple<Guid, MetadataRefreshOptions>(itemId, options), (int)priority);

            lock (_refreshQueueLock)
            {
                if (!_isProcessingRefreshQueue)
                {
                    _isProcessingRefreshQueue = true;
                    Task.Run(StartProcessingRefreshQueue);
                }
            }
        }

        private async Task StartProcessingRefreshQueue()
        {
            var libraryManager = _libraryManager;

            if (_disposed)
            {
                return;
            }

            var cancellationToken = _disposeCancellationTokenSource.Token;

            while (_refreshQueue.TryDequeue(out Tuple<Guid, MetadataRefreshOptions> refreshItem))
            {
                if (_disposed)
                {
                    return;
                }

                try
                {
                    var item = libraryManager.GetItemById(refreshItem.Item1);
                    if (item == null)
                    {
                        continue;
                    }

                    var task = item is MusicArtist artist
                        ? RefreshArtist(artist, refreshItem.Item2, cancellationToken)
                        : RefreshItem(item, refreshItem.Item2, cancellationToken);

                    await task.ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error refreshing item");
                }
            }

            lock (_refreshQueueLock)
            {
                _isProcessingRefreshQueue = false;
            }
        }

        private async Task RefreshItem(BaseItem item, MetadataRefreshOptions options, CancellationToken cancellationToken)
        {
            await item.RefreshMetadata(options, cancellationToken).ConfigureAwait(false);

            // Collection folders don't validate their children so we'll have to simulate that here
            switch (item)
            {
                case CollectionFolder collectionFolder:
                    await RefreshCollectionFolderChildren(options, collectionFolder, cancellationToken).ConfigureAwait(false);
                    break;
                case Folder folder:
                    await folder.ValidateChildren(new SimpleProgress<double>(), options, cancellationToken: cancellationToken).ConfigureAwait(false);
                    break;
            }
        }

        private async Task RefreshCollectionFolderChildren(MetadataRefreshOptions options, CollectionFolder collectionFolder, CancellationToken cancellationToken)
        {
            foreach (var child in collectionFolder.GetPhysicalFolders())
            {
                await child.RefreshMetadata(options, cancellationToken).ConfigureAwait(false);

                await child.ValidateChildren(new SimpleProgress<double>(), options, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task RefreshArtist(MusicArtist item, MetadataRefreshOptions options, CancellationToken cancellationToken)
        {
            var albums = _libraryManager
                .GetItemList(new InternalItemsQuery
                {
                    IncludeItemTypes = new[] { BaseItemKind.MusicAlbum },
                    ArtistIds = new[] { item.Id },
                    DtoOptions = new DtoOptions(false)
                    {
                        EnableImages = false
                    }
                })
                .OfType<MusicAlbum>();

            var musicArtists = albums
                .Select(i => i.MusicArtist)
                .Where(i => i != null);

            var musicArtistRefreshTasks = musicArtists.Select(i => i.ValidateChildren(new SimpleProgress<double>(), options, true, cancellationToken));

            await Task.WhenAll(musicArtistRefreshTasks).ConfigureAwait(false);

            try
            {
                await item.RefreshMetadata(options, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing library");
            }
        }

        /// <inheritdoc/>
        public Task RefreshFullItem(BaseItem item, MetadataRefreshOptions options, CancellationToken cancellationToken)
        {
            return RefreshItem(item, options, cancellationToken);
        }

        /// <summary>
        /// Runs multiple metadata refreshes concurrently.
        /// </summary>
        /// <param name="action">The action to run.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        public async Task RunMetadataRefresh(Func<Task> action, CancellationToken cancellationToken)
        {
            // create a variable for this since it is possible MetadataRefreshThrottler could change due to a config update during a scan
            var metadataRefreshThrottler = _baseItemManager.MetadataRefreshThrottler;

            await metadataRefreshThrottler.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                await action().ConfigureAwait(false);
            }
            finally
            {
                metadataRefreshThrottler.Release();
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and optionally managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (!_disposeCancellationTokenSource.IsCancellationRequested)
            {
                _disposeCancellationTokenSource.Cancel();
            }

            if (disposing)
            {
                _disposeCancellationTokenSource.Dispose();
            }

            _disposed = true;
        }
    }
}
