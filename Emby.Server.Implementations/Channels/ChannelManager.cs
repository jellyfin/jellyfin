using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Progress;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.Channels
{
    /// <summary>
    /// The LiveTV channel manager.
    /// </summary>
    public class ChannelManager : IChannelManager
    {
        private readonly IUserManager _userManager;
        private readonly IUserDataManager _userDataManager;
        private readonly IDtoService _dtoService;
        private readonly ILibraryManager _libraryManager;
        private readonly ILogger _logger;
        private readonly IServerConfigurationManager _config;
        private readonly IFileSystem _fileSystem;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IProviderManager _providerManager;

        private readonly ConcurrentDictionary<string, Tuple<DateTime, List<MediaSourceInfo>>> _channelItemMediaInfo =
            new ConcurrentDictionary<string, Tuple<DateTime, List<MediaSourceInfo>>>();

        private readonly SemaphoreSlim _resourcePool = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Initializes a new instance of the <see cref="ChannelManager"/> class.
        /// </summary>
        /// <param name="userManager">The user manager.</param>
        /// <param name="dtoService">The dto service.</param>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="config">The server configuration manager.</param>
        /// <param name="fileSystem">The filesystem.</param>
        /// <param name="userDataManager">The user data manager.</param>
        /// <param name="jsonSerializer">The JSON serializer.</param>
        /// <param name="providerManager">The provider manager.</param>
        public ChannelManager(
            IUserManager userManager,
            IDtoService dtoService,
            ILibraryManager libraryManager,
            ILogger<ChannelManager> logger,
            IServerConfigurationManager config,
            IFileSystem fileSystem,
            IUserDataManager userDataManager,
            IJsonSerializer jsonSerializer,
            IProviderManager providerManager)
        {
            _userManager = userManager;
            _dtoService = dtoService;
            _libraryManager = libraryManager;
            _logger = logger;
            _config = config;
            _fileSystem = fileSystem;
            _userDataManager = userDataManager;
            _jsonSerializer = jsonSerializer;
            _providerManager = providerManager;
        }

        internal IChannel[] Channels { get; private set; }

        private static TimeSpan CacheLength => TimeSpan.FromHours(3);

        /// <inheritdoc />
        public void AddParts(IEnumerable<IChannel> channels)
        {
            Channels = channels.ToArray();
        }

        /// <inheritdoc />
        public bool EnableMediaSourceDisplay(BaseItem item)
        {
            var internalChannel = _libraryManager.GetItemById(item.ChannelId);
            var channel = Channels.FirstOrDefault(i => GetInternalChannelId(i.Name).Equals(internalChannel.Id));

            return !(channel is IDisableMediaSourceDisplay);
        }

        /// <inheritdoc />
        public bool CanDelete(BaseItem item)
        {
            var internalChannel = _libraryManager.GetItemById(item.ChannelId);
            var channel = Channels.FirstOrDefault(i => GetInternalChannelId(i.Name).Equals(internalChannel.Id));

            return channel is ISupportsDelete supportsDelete && supportsDelete.CanDelete(item);
        }

        /// <inheritdoc />
        public bool EnableMediaProbe(BaseItem item)
        {
            var internalChannel = _libraryManager.GetItemById(item.ChannelId);
            var channel = Channels.FirstOrDefault(i => GetInternalChannelId(i.Name).Equals(internalChannel.Id));

            return channel is ISupportsMediaProbe;
        }

        /// <inheritdoc />
        public Task DeleteItem(BaseItem item)
        {
            var internalChannel = _libraryManager.GetItemById(item.ChannelId);
            if (internalChannel == null)
            {
                throw new ArgumentException();
            }

            var channel = Channels.FirstOrDefault(i => GetInternalChannelId(i.Name).Equals(internalChannel.Id));

            var supportsDelete = channel as ISupportsDelete;

            if (supportsDelete == null)
            {
                throw new ArgumentException();
            }

            return supportsDelete.DeleteItem(item.ExternalId, CancellationToken.None);
        }

        private IEnumerable<IChannel> GetAllChannels()
        {
            return Channels
                .OrderBy(i => i.Name);
        }

        /// <summary>
        /// Get the installed channel IDs.
        /// </summary>
        /// <returns>An <see cref="IEnumerable{T}"/> containing installed channel IDs.</returns>
        public IEnumerable<Guid> GetInstalledChannelIds()
        {
            return GetAllChannels().Select(i => GetInternalChannelId(i.Name));
        }

        /// <inheritdoc />
        public QueryResult<Channel> GetChannelsInternal(ChannelQuery query)
        {
            var user = query.UserId.Equals(Guid.Empty)
                ? null
                : _userManager.GetUserById(query.UserId);

            var channels = GetAllChannels()
                .Select(GetChannelEntity)
                .OrderBy(i => i.SortName)
                .ToList();

            if (query.IsRecordingsFolder.HasValue)
            {
                var val = query.IsRecordingsFolder.Value;
                channels = channels.Where(i =>
                {
                    try
                    {
                        return (GetChannelProvider(i) is IHasFolderAttributes hasAttributes
                            && hasAttributes.Attributes.Contains("Recordings", StringComparer.OrdinalIgnoreCase)) == val;
                    }
                    catch
                    {
                        return false;
                    }
                }).ToList();
            }

            if (query.SupportsLatestItems.HasValue)
            {
                var val = query.SupportsLatestItems.Value;
                channels = channels.Where(i =>
                {
                    try
                    {
                        return GetChannelProvider(i) is ISupportsLatestMedia == val;
                    }
                    catch
                    {
                        return false;
                    }
                }).ToList();
            }

            if (query.SupportsMediaDeletion.HasValue)
            {
                var val = query.SupportsMediaDeletion.Value;
                channels = channels.Where(i =>
                {
                    try
                    {
                        return GetChannelProvider(i) is ISupportsDelete == val;
                    }
                    catch
                    {
                        return false;
                    }
                }).ToList();
            }

            if (query.IsFavorite.HasValue)
            {
                var val = query.IsFavorite.Value;
                channels = channels.Where(i => _userDataManager.GetUserData(user, i).IsFavorite == val)
                    .ToList();
            }

            if (user != null)
            {
                channels = channels.Where(i =>
                {
                    if (!i.IsVisible(user))
                    {
                        return false;
                    }

                    try
                    {
                        return GetChannelProvider(i).IsEnabledFor(user.Id.ToString("N", CultureInfo.InvariantCulture));
                    }
                    catch
                    {
                        return false;
                    }
                }).ToList();
            }

            var all = channels;
            var totalCount = all.Count;

            if (query.StartIndex.HasValue)
            {
                all = all.Skip(query.StartIndex.Value).ToList();
            }

            if (query.Limit.HasValue)
            {
                all = all.Take(query.Limit.Value).ToList();
            }

            var returnItems = all.ToArray();

            if (query.RefreshLatestChannelItems)
            {
                foreach (var item in returnItems)
                {
                    RefreshLatestChannelItems(GetChannelProvider(item), CancellationToken.None).GetAwaiter().GetResult();
                }
            }

            return new QueryResult<Channel>
            {
                Items = returnItems,
                TotalRecordCount = totalCount
            };
        }

        /// <inheritdoc />
        public QueryResult<BaseItemDto> GetChannels(ChannelQuery query)
        {
            var user = query.UserId.Equals(Guid.Empty)
                ? null
                : _userManager.GetUserById(query.UserId);

            var internalResult = GetChannelsInternal(query);

            var dtoOptions = new DtoOptions();

            // TODO Fix The co-variant conversion (internalResult.Items) between Folder[] and BaseItem[], this can generate runtime issues.
            var returnItems = _dtoService.GetBaseItemDtos(internalResult.Items, dtoOptions, user);

            var result = new QueryResult<BaseItemDto>
            {
                Items = returnItems,
                TotalRecordCount = internalResult.TotalRecordCount
            };

            return result;
        }

        /// <summary>
        /// Refreshes the associated channels.
        /// </summary>
        /// <param name="progress">The progress.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>The completed task.</returns>
        public async Task RefreshChannels(IProgress<double> progress, CancellationToken cancellationToken)
        {
            var allChannelsList = GetAllChannels().ToList();

            var numComplete = 0;

            foreach (var channelInfo in allChannelsList)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    await GetChannel(channelInfo, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting channel information for {0}", channelInfo.Name);
                }

                numComplete++;
                double percent = (double)numComplete / allChannelsList.Count;
                progress.Report(100 * percent);
            }

            progress.Report(100);
        }

        private Channel GetChannelEntity(IChannel channel)
        {
            return GetChannel(GetInternalChannelId(channel.Name)) ?? GetChannel(channel, CancellationToken.None).Result;
        }

        private List<MediaSourceInfo> GetSavedMediaSources(BaseItem item)
        {
            var path = Path.Combine(item.GetInternalMetadataPath(), "channelmediasourceinfos.json");

            try
            {
                return _jsonSerializer.DeserializeFromFile<List<MediaSourceInfo>>(path) ?? new List<MediaSourceInfo>();
            }
            catch
            {
                return new List<MediaSourceInfo>();
            }
        }

        private void SaveMediaSources(BaseItem item, List<MediaSourceInfo> mediaSources)
        {
            var path = Path.Combine(item.GetInternalMetadataPath(), "channelmediasourceinfos.json");

            if (mediaSources == null || mediaSources.Count == 0)
            {
                try
                {
                    _fileSystem.DeleteFile(path);
                }
                catch
                {
                }

                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path));

            _jsonSerializer.SerializeToFile(mediaSources, path);
        }

        /// <inheritdoc />
        public IEnumerable<MediaSourceInfo> GetStaticMediaSources(BaseItem item, CancellationToken cancellationToken)
        {
            IEnumerable<MediaSourceInfo> results = GetSavedMediaSources(item);

            return results
                .Select(i => NormalizeMediaSource(item, i))
                .ToList();
        }

        /// <summary>
        /// Gets the dynamic media sources based on the provided item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>The task representing the operation to get the media sources.</returns>
        public async Task<IEnumerable<MediaSourceInfo>> GetDynamicMediaSources(BaseItem item, CancellationToken cancellationToken)
        {
            var channel = GetChannel(item.ChannelId);
            var channelPlugin = GetChannelProvider(channel);

            IEnumerable<MediaSourceInfo> results;

            if (channelPlugin is IRequiresMediaInfoCallback requiresCallback)
            {
                results = await GetChannelItemMediaSourcesInternal(requiresCallback, item.ExternalId, cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                results = new List<MediaSourceInfo>();
            }

            return results
                .Select(i => NormalizeMediaSource(item, i))
                .ToList();
        }

        private async Task<IEnumerable<MediaSourceInfo>> GetChannelItemMediaSourcesInternal(IRequiresMediaInfoCallback channel, string id, CancellationToken cancellationToken)
        {
            if (_channelItemMediaInfo.TryGetValue(id, out Tuple<DateTime, List<MediaSourceInfo>> cachedInfo))
            {
                if ((DateTime.UtcNow - cachedInfo.Item1).TotalMinutes < 5)
                {
                    return cachedInfo.Item2;
                }
            }

            var mediaInfo = await channel.GetChannelItemMediaInfo(id, cancellationToken)
                   .ConfigureAwait(false);
            var list = mediaInfo.ToList();

            var item2 = new Tuple<DateTime, List<MediaSourceInfo>>(DateTime.UtcNow, list);
            _channelItemMediaInfo.AddOrUpdate(id, item2, (key, oldValue) => item2);

            return list;
        }

        private static MediaSourceInfo NormalizeMediaSource(BaseItem item, MediaSourceInfo info)
        {
            info.RunTimeTicks ??= item.RunTimeTicks;

            return info;
        }

        private async Task<Channel> GetChannel(IChannel channelInfo, CancellationToken cancellationToken)
        {
            var parentFolderId = Guid.Empty;

            var id = GetInternalChannelId(channelInfo.Name);

            var path = Channel.GetInternalMetadataPath(_config.ApplicationPaths.InternalMetadataPath, id);

            var isNew = false;
            var forceUpdate = false;

            var item = _libraryManager.GetItemById(id) as Channel;

            if (item == null)
            {
                item = new Channel
                {
                    Name = channelInfo.Name,
                    Id = id,
                    DateCreated = _fileSystem.GetCreationTimeUtc(path),
                    DateModified = _fileSystem.GetLastWriteTimeUtc(path)
                };

                isNew = true;
            }

            if (!string.Equals(item.Path, path, StringComparison.OrdinalIgnoreCase))
            {
                isNew = true;
            }

            item.Path = path;

            if (!item.ChannelId.Equals(id))
            {
                forceUpdate = true;
            }

            item.ChannelId = id;

            if (item.ParentId != parentFolderId)
            {
                forceUpdate = true;
            }

            item.ParentId = parentFolderId;

            item.OfficialRating = GetOfficialRating(channelInfo.ParentalRating);
            item.Overview = channelInfo.Description;

            if (string.IsNullOrWhiteSpace(item.Name))
            {
                item.Name = channelInfo.Name;
            }

            if (isNew)
            {
                item.OnMetadataChanged();
                _libraryManager.CreateItem(item, null);
            }

            await item.RefreshMetadata(
                new MetadataRefreshOptions(new DirectoryService(_fileSystem))
                {
                    ForceSave = !isNew && forceUpdate
                },
                cancellationToken).ConfigureAwait(false);

            return item;
        }

        private static string GetOfficialRating(ChannelParentalRating rating)
        {
            return rating switch
            {
                ChannelParentalRating.Adult => "XXX",
                ChannelParentalRating.UsR => "R",
                ChannelParentalRating.UsPG13 => "PG-13",
                ChannelParentalRating.UsPG => "PG",
                _ => null
            };
        }

        /// <summary>
        /// Gets a channel with the provided Guid.
        /// </summary>
        /// <param name="id">The Guid.</param>
        /// <returns>The corresponding channel.</returns>
        public Channel GetChannel(Guid id)
        {
            return _libraryManager.GetItemById(id) as Channel;
        }

        /// <inheritdoc />
        public Channel GetChannel(string id)
        {
            return _libraryManager.GetItemById(id) as Channel;
        }

        /// <inheritdoc />
        public ChannelFeatures[] GetAllChannelFeatures()
        {
            return _libraryManager.GetItemIds(
                new InternalItemsQuery
                {
                    IncludeItemTypes = new[] { typeof(Channel).Name },
                    OrderBy = new[] { (ItemSortBy.SortName, SortOrder.Ascending) }
                }).Select(i => GetChannelFeatures(i.ToString("N", CultureInfo.InvariantCulture))).ToArray();
        }

        /// <inheritdoc />
        public ChannelFeatures GetChannelFeatures(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException(nameof(id));
            }

            var channel = GetChannel(id);
            var channelProvider = GetChannelProvider(channel);

            return GetChannelFeaturesDto(channel, channelProvider, channelProvider.GetChannelFeatures());
        }

        /// <summary>
        /// Checks whether the provided Guid supports external transfer.
        /// </summary>
        /// <param name="channelId">The Guid.</param>
        /// <returns>Whether or not the provided Guid supports external transfer.</returns>
        public bool SupportsExternalTransfer(Guid channelId)
        {
            var channelProvider = GetChannelProvider(channelId);

            return channelProvider.GetChannelFeatures().SupportsContentDownloading;
        }

        /// <summary>
        /// Gets the provided channel's supported features.
        /// </summary>
        /// <param name="channel">The channel.</param>
        /// <param name="provider">The provider.</param>
        /// <param name="features">The features.</param>
        /// <returns>The supported features.</returns>
        public ChannelFeatures GetChannelFeaturesDto(
            Channel channel,
            IChannel provider,
            InternalChannelFeatures features)
        {
            var supportsLatest = provider is ISupportsLatestMedia;

            return new ChannelFeatures
            {
                CanFilter = !features.MaxPageSize.HasValue,
                CanSearch = provider is ISearchableChannel,
                ContentTypes = features.ContentTypes.ToArray(),
                DefaultSortFields = features.DefaultSortFields.ToArray(),
                MaxPageSize = features.MaxPageSize,
                MediaTypes = features.MediaTypes.ToArray(),
                SupportsSortOrderToggle = features.SupportsSortOrderToggle,
                SupportsLatestMedia = supportsLatest,
                Name = channel.Name,
                Id = channel.Id.ToString("N", CultureInfo.InvariantCulture),
                SupportsContentDownloading = features.SupportsContentDownloading,
                AutoRefreshLevels = features.AutoRefreshLevels
            };
        }

        private Guid GetInternalChannelId(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            return _libraryManager.GetNewItemId("Channel " + name, typeof(Channel));
        }

        /// <inheritdoc />
        public async Task<QueryResult<BaseItemDto>> GetLatestChannelItems(InternalItemsQuery query, CancellationToken cancellationToken)
        {
            var internalResult = await GetLatestChannelItemsInternal(query, cancellationToken).ConfigureAwait(false);

            var items = internalResult.Items;
            var totalRecordCount = internalResult.TotalRecordCount;

            var returnItems = _dtoService.GetBaseItemDtos(items, query.DtoOptions, query.User);

            var result = new QueryResult<BaseItemDto>
            {
                Items = returnItems,
                TotalRecordCount = totalRecordCount
            };

            return result;
        }

        /// <inheritdoc />
        public async Task<QueryResult<BaseItem>> GetLatestChannelItemsInternal(InternalItemsQuery query, CancellationToken cancellationToken)
        {
            var channels = GetAllChannels().Where(i => i is ISupportsLatestMedia).ToArray();

            if (query.ChannelIds.Length > 0)
            {
                // Avoid implicitly captured closure
                var ids = query.ChannelIds;
                channels = channels
                    .Where(i => ids.Contains(GetInternalChannelId(i.Name)))
                    .ToArray();
            }

            if (channels.Length == 0)
            {
                return new QueryResult<BaseItem>();
            }

            foreach (var channel in channels)
            {
                await RefreshLatestChannelItems(channel, cancellationToken).ConfigureAwait(false);
            }

            query.IsFolder = false;

            // hack for trailers, figure out a better way later
            var sortByPremiereDate = channels.Length == 1 && channels[0].GetType().Name.Contains("Trailer", StringComparison.Ordinal);

            if (sortByPremiereDate)
            {
                query.OrderBy = new[]
                {
                    (ItemSortBy.PremiereDate, SortOrder.Descending),
                    (ItemSortBy.ProductionYear, SortOrder.Descending),
                    (ItemSortBy.DateCreated, SortOrder.Descending)
                };
            }
            else
            {
                query.OrderBy = new[]
                {
                    (ItemSortBy.DateCreated, SortOrder.Descending)
                };
            }

            return _libraryManager.GetItemsResult(query);
        }

        private async Task RefreshLatestChannelItems(IChannel channel, CancellationToken cancellationToken)
        {
            var internalChannel = await GetChannel(channel, cancellationToken).ConfigureAwait(false);

            var query = new InternalItemsQuery
            {
                Parent = internalChannel,
                EnableTotalRecordCount = false,
                ChannelIds = new Guid[] { internalChannel.Id }
            };

            var result = await GetChannelItemsInternal(query, new SimpleProgress<double>(), cancellationToken).ConfigureAwait(false);

            foreach (var item in result.Items)
            {
                if (item is Folder folder)
                {
                    await GetChannelItemsInternal(
                        new InternalItemsQuery
                        {
                            Parent = folder,
                            EnableTotalRecordCount = false,
                            ChannelIds = new Guid[] { internalChannel.Id }
                        },
                        new SimpleProgress<double>(),
                        cancellationToken).ConfigureAwait(false);
                }
            }
        }

        /// <inheritdoc />
        public async Task<QueryResult<BaseItem>> GetChannelItemsInternal(InternalItemsQuery query, IProgress<double> progress, CancellationToken cancellationToken)
        {
            // Get the internal channel entity
            var channel = GetChannel(query.ChannelIds[0]);

            // Find the corresponding channel provider plugin
            var channelProvider = GetChannelProvider(channel);

            var parentItem = query.ParentId == Guid.Empty ? channel : _libraryManager.GetItemById(query.ParentId);

            var itemsResult = await GetChannelItems(
                channelProvider,
                query.User,
                parentItem is Channel ? null : parentItem.ExternalId,
                null,
                false,
                cancellationToken)
                .ConfigureAwait(false);

            if (query.ParentId == Guid.Empty)
            {
                query.Parent = channel;
            }

            query.ChannelIds = Array.Empty<Guid>();

            // Not yet sure why this is causing a problem
            query.GroupByPresentationUniqueKey = false;

            // null if came from cache
            if (itemsResult != null)
            {
                var internalItems = itemsResult.Items
                    .Select(i => GetChannelItemEntity(i, channelProvider, channel.Id, parentItem, cancellationToken))
                    .ToArray();

                var existingIds = _libraryManager.GetItemIds(query);
                var deadIds = existingIds.Except(internalItems.Select(i => i.Id))
                    .ToArray();

                foreach (var deadId in deadIds)
                {
                    var deadItem = _libraryManager.GetItemById(deadId);
                    if (deadItem != null)
                    {
                        _libraryManager.DeleteItem(
                            deadItem,
                            new DeleteOptions
                            {
                                DeleteFileLocation = false,
                                DeleteFromExternalProvider = false
                            },
                            parentItem,
                            false);
                    }
                }
            }

            return _libraryManager.GetItemsResult(query);
        }

        /// <inheritdoc />
        public async Task<QueryResult<BaseItemDto>> GetChannelItems(InternalItemsQuery query, CancellationToken cancellationToken)
        {
            var internalResult = await GetChannelItemsInternal(query, new SimpleProgress<double>(), cancellationToken).ConfigureAwait(false);

            var returnItems = _dtoService.GetBaseItemDtos(internalResult.Items, query.DtoOptions, query.User);

            var result = new QueryResult<BaseItemDto>
            {
                Items = returnItems,
                TotalRecordCount = internalResult.TotalRecordCount
            };

            return result;
        }

        private async Task<ChannelItemResult> GetChannelItems(IChannel channel,
            User user,
            string externalFolderId,
            ChannelItemSortField? sortField,
            bool sortDescending,
            CancellationToken cancellationToken)
        {
            var userId = user?.Id.ToString("N", CultureInfo.InvariantCulture);

            var cacheLength = CacheLength;
            var cachePath = GetChannelDataCachePath(channel, userId, externalFolderId, sortField, sortDescending);

            try
            {
                if (_fileSystem.GetLastWriteTimeUtc(cachePath).Add(cacheLength) > DateTime.UtcNow)
                {
                    var cachedResult = _jsonSerializer.DeserializeFromFile<ChannelItemResult>(cachePath);
                    if (cachedResult != null)
                    {
                        return null;
                    }
                }
            }
            catch (FileNotFoundException)
            {
            }
            catch (IOException)
            {
            }

            await _resourcePool.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                try
                {
                    if (_fileSystem.GetLastWriteTimeUtc(cachePath).Add(cacheLength) > DateTime.UtcNow)
                    {
                        var cachedResult = _jsonSerializer.DeserializeFromFile<ChannelItemResult>(cachePath);
                        if (cachedResult != null)
                        {
                            return null;
                        }
                    }
                }
                catch (FileNotFoundException)
                {
                }
                catch (IOException)
                {
                }

                var query = new InternalChannelItemQuery
                {
                    UserId = user?.Id ?? Guid.Empty,
                    SortBy = sortField,
                    SortDescending = sortDescending,
                    FolderId = externalFolderId
                };

                query.FolderId = externalFolderId;

                var result = await channel.GetChannelItems(query, cancellationToken).ConfigureAwait(false);

                if (result == null)
                {
                    throw new InvalidOperationException("Channel returned a null result from GetChannelItems");
                }

                CacheResponse(result, cachePath);

                return result;
            }
            finally
            {
                _resourcePool.Release();
            }
        }

        private void CacheResponse(object result, string path)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));

                _jsonSerializer.SerializeToFile(result, path);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error writing to channel cache file: {path}", path);
            }
        }

        private string GetChannelDataCachePath(
            IChannel channel,
            string userId,
            string externalFolderId,
            ChannelItemSortField? sortField,
            bool sortDescending)
        {
            var channelId = GetInternalChannelId(channel.Name).ToString("N", CultureInfo.InvariantCulture);

            var userCacheKey = string.Empty;

            if (channel is IHasCacheKey hasCacheKey)
            {
                userCacheKey = hasCacheKey.GetCacheKey(userId) ?? string.Empty;
            }

            var filename = string.IsNullOrEmpty(externalFolderId) ? "root" : externalFolderId.GetMD5().ToString("N", CultureInfo.InvariantCulture);
            filename += userCacheKey;

            var version = ((channel.DataVersion ?? string.Empty) + "2").GetMD5().ToString("N", CultureInfo.InvariantCulture);

            if (sortField.HasValue)
            {
                filename += "-sortField-" + sortField.Value;
            }

            if (sortDescending)
            {
                filename += "-sortDescending";
            }

            filename = filename.GetMD5().ToString("N", CultureInfo.InvariantCulture);

            return Path.Combine(
                _config.ApplicationPaths.CachePath,
                "channels",
                channelId,
                version,
                filename + ".json");
        }

        private static string GetIdToHash(string externalId, string channelName)
        {
            // Increment this as needed to force new downloads
            // Incorporate Name because it's being used to convert channel entity to provider
            return externalId + (channelName ?? string.Empty) + "16";
        }

        private T GetItemById<T>(string idString, string channelName, out bool isNew)
            where T : BaseItem, new()
        {
            var id = _libraryManager.GetNewItemId(GetIdToHash(idString, channelName), typeof(T));

            T item = null;

            try
            {
                item = _libraryManager.GetItemById(id) as T;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving channel item from database");
            }

            if (item == null)
            {
                item = new T();
                isNew = true;
            }
            else
            {
                isNew = false;
            }

            item.Id = id;
            return item;
        }

        private BaseItem GetChannelItemEntity(ChannelItemInfo info, IChannel channelProvider, Guid internalChannelId, BaseItem parentFolder, CancellationToken cancellationToken)
        {
            var parentFolderId = parentFolder.Id;

            BaseItem item;
            bool isNew;
            bool forceUpdate = false;

            if (info.Type == ChannelItemType.Folder)
            {
                item = info.FolderType switch
                {
                    ChannelFolderType.MusicAlbum => GetItemById<MusicAlbum>(info.Id, channelProvider.Name, out isNew),
                    ChannelFolderType.MusicArtist => GetItemById<MusicArtist>(info.Id, channelProvider.Name, out isNew),
                    ChannelFolderType.PhotoAlbum => GetItemById<PhotoAlbum>(info.Id, channelProvider.Name, out isNew),
                    ChannelFolderType.Series => GetItemById<Series>(info.Id, channelProvider.Name, out isNew),
                    ChannelFolderType.Season => GetItemById<Season>(info.Id, channelProvider.Name, out isNew),
                    _ => GetItemById<Folder>(info.Id, channelProvider.Name, out isNew)
                };
            }
            else if (info.MediaType == ChannelMediaType.Audio)
            {
                item = info.ContentType == ChannelMediaContentType.Podcast
                    ? GetItemById<AudioBook>(info.Id, channelProvider.Name, out isNew)
                    : GetItemById<Audio>(info.Id, channelProvider.Name, out isNew);
            }
            else
            {
                item = info.ContentType switch
                {
                    ChannelMediaContentType.Episode => GetItemById<Episode>(info.Id, channelProvider.Name, out isNew),
                    ChannelMediaContentType.Movie => GetItemById<Movie>(info.Id, channelProvider.Name, out isNew),
                    var x when x == ChannelMediaContentType.Trailer || info.ExtraType == ExtraType.Trailer
                    => GetItemById<Trailer>(info.Id, channelProvider.Name, out isNew),
                    _ => GetItemById<Video>(info.Id, channelProvider.Name, out isNew)
                };
            }

            var enableMediaProbe = channelProvider is ISupportsMediaProbe;

            if (info.IsLiveStream)
            {
                item.RunTimeTicks = null;
            }
            else if (isNew || !enableMediaProbe)
            {
                item.RunTimeTicks = info.RunTimeTicks;
            }

            if (isNew)
            {
                item.Name = info.Name;
                item.Genres = info.Genres.ToArray();
                item.Studios = info.Studios.ToArray();
                item.CommunityRating = info.CommunityRating;
                item.Overview = info.Overview;
                item.IndexNumber = info.IndexNumber;
                item.ParentIndexNumber = info.ParentIndexNumber;
                item.PremiereDate = info.PremiereDate;
                item.ProductionYear = info.ProductionYear;
                item.ProviderIds = info.ProviderIds;
                item.OfficialRating = info.OfficialRating;
                item.DateCreated = info.DateCreated ?? DateTime.UtcNow;
                item.Tags = info.Tags.ToArray();
                item.OriginalTitle = info.OriginalTitle;
            }
            else if (info.Type == ChannelItemType.Folder && info.FolderType == ChannelFolderType.Container)
            {
                // At least update names of container folders
                if (item.Name != info.Name)
                {
                    item.Name = info.Name;
                    forceUpdate = true;
                }
            }

            if (item is IHasArtist hasArtists)
            {
                hasArtists.Artists = info.Artists.ToArray();
            }

            if (item is IHasAlbumArtist hasAlbumArtists)
            {
                hasAlbumArtists.AlbumArtists = info.AlbumArtists.ToArray();
            }

            if (item is Trailer trailer)
            {
                if (!info.TrailerTypes.SequenceEqual(trailer.TrailerTypes))
                {
                    _logger.LogDebug("Forcing update due to TrailerTypes {0}", item.Name);
                    forceUpdate = true;
                }

                trailer.TrailerTypes = info.TrailerTypes.ToArray();
            }

            if (info.DateModified > item.DateModified)
            {
                item.DateModified = info.DateModified;
                _logger.LogDebug("Forcing update due to DateModified {0}", item.Name);
                forceUpdate = true;
            }

            // was used for status
            //if (!string.Equals(item.ExternalEtag ?? string.Empty, info.Etag ?? string.Empty, StringComparison.Ordinal))
            //{
            //    item.ExternalEtag = info.Etag;
            //    forceUpdate = true;
            //    _logger.LogDebug("Forcing update due to ExternalEtag {0}", item.Name);
            //}

            if (!internalChannelId.Equals(item.ChannelId))
            {
                forceUpdate = true;
                _logger.LogDebug("Forcing update due to ChannelId {0}", item.Name);
            }

            item.ChannelId = internalChannelId;

            if (!item.ParentId.Equals(parentFolderId))
            {
                forceUpdate = true;
                _logger.LogDebug("Forcing update due to parent folder Id {0}", item.Name);
            }

            item.ParentId = parentFolderId;

            if (item is IHasSeries hasSeries)
            {
                if (!string.Equals(hasSeries.SeriesName, info.SeriesName, StringComparison.OrdinalIgnoreCase))
                {
                    forceUpdate = true;
                    _logger.LogDebug("Forcing update due to SeriesName {0}", item.Name);
                }

                hasSeries.SeriesName = info.SeriesName;
            }

            if (!string.Equals(item.ExternalId, info.Id, StringComparison.OrdinalIgnoreCase))
            {
                forceUpdate = true;
                _logger.LogDebug("Forcing update due to ExternalId {0}", item.Name);
            }

            item.ExternalId = info.Id;

            if (item is Audio channelAudioItem)
            {
                channelAudioItem.ExtraType = info.ExtraType;

                var mediaSource = info.MediaSources.FirstOrDefault();
                item.Path = mediaSource?.Path;
            }

            if (item is Video channelVideoItem)
            {
                channelVideoItem.ExtraType = info.ExtraType;

                var mediaSource = info.MediaSources.FirstOrDefault();
                item.Path = mediaSource?.Path;
            }

            if (!string.IsNullOrEmpty(info.ImageUrl) && !item.HasImage(ImageType.Primary))
            {
                item.SetImagePath(ImageType.Primary, info.ImageUrl);
                _logger.LogDebug("Forcing update due to ImageUrl {0}", item.Name);
                forceUpdate = true;
            }

            if (!info.IsLiveStream)
            {
                if (item.Tags.Contains("livestream", StringComparer.OrdinalIgnoreCase))
                {
                    item.Tags = item.Tags.Except(new[] { "livestream" }, StringComparer.OrdinalIgnoreCase).ToArray();
                    _logger.LogDebug("Forcing update due to Tags {0}", item.Name);
                    forceUpdate = true;
                }
            }
            else
            {
                if (!item.Tags.Contains("livestream", StringComparer.OrdinalIgnoreCase))
                {
                    item.Tags = item.Tags.Concat(new[] { "livestream" }).ToArray();
                    _logger.LogDebug("Forcing update due to Tags {0}", item.Name);
                    forceUpdate = true;
                }
            }

            item.OnMetadataChanged();

            if (isNew)
            {
                _libraryManager.CreateItem(item, parentFolder);

                if (info.People != null && info.People.Count > 0)
                {
                    _libraryManager.UpdatePeople(item, info.People);
                }
            }
            else if (forceUpdate)
            {
                item.UpdateToRepository(ItemUpdateType.None, cancellationToken);
            }

            if ((isNew || forceUpdate) && info.Type == ChannelItemType.Media)
            {
                if (enableMediaProbe && !info.IsLiveStream && item.HasPathProtocol)
                {
                    SaveMediaSources(item, new List<MediaSourceInfo>());
                }
                else
                {
                    SaveMediaSources(item, info.MediaSources);
                }
            }

            if (isNew || forceUpdate || item.DateLastRefreshed == default)
            {
                _providerManager.QueueRefresh(item.Id, new MetadataRefreshOptions(new DirectoryService(_fileSystem)), RefreshPriority.Normal);
            }

            return item;
        }

        internal IChannel GetChannelProvider(Channel channel)
        {
            if (channel == null)
            {
                throw new ArgumentNullException(nameof(channel));
            }

            var result = GetAllChannels()
                .FirstOrDefault(i => GetInternalChannelId(i.Name).Equals(channel.ChannelId) || string.Equals(i.Name, channel.Name, StringComparison.OrdinalIgnoreCase));

            if (result == null)
            {
                throw new ResourceNotFoundException("No channel provider found for channel " + channel.Name);
            }

            return result;
        }

        internal IChannel GetChannelProvider(Guid internalChannelId)
        {
            var result = GetAllChannels()
                .FirstOrDefault(i => internalChannelId.Equals(GetInternalChannelId(i.Name)));

            if (result == null)
            {
                throw new ResourceNotFoundException("No channel provider found for channel id " + internalChannelId);
            }

            return result;
        }
    }
}
