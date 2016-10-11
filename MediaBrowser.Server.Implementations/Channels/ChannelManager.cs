using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Localization;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Extensions;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using CommonIO;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;

namespace MediaBrowser.Server.Implementations.Channels
{
    public class ChannelManager : IChannelManager
    {
        private IChannel[] _channels;

        private readonly IUserManager _userManager;
        private readonly IUserDataManager _userDataManager;
        private readonly IDtoService _dtoService;
        private readonly ILibraryManager _libraryManager;
        private readonly ILogger _logger;
        private readonly IServerConfigurationManager _config;
        private readonly IFileSystem _fileSystem;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IHttpClient _httpClient;
        private readonly IProviderManager _providerManager;

        private readonly ILocalizationManager _localization;
        private readonly ConcurrentDictionary<Guid, bool> _refreshedItems = new ConcurrentDictionary<Guid, bool>();

        public ChannelManager(IUserManager userManager, IDtoService dtoService, ILibraryManager libraryManager, ILogger logger, IServerConfigurationManager config, IFileSystem fileSystem, IUserDataManager userDataManager, IJsonSerializer jsonSerializer, ILocalizationManager localization, IHttpClient httpClient, IProviderManager providerManager)
        {
            _userManager = userManager;
            _dtoService = dtoService;
            _libraryManager = libraryManager;
            _logger = logger;
            _config = config;
            _fileSystem = fileSystem;
            _userDataManager = userDataManager;
            _jsonSerializer = jsonSerializer;
            _localization = localization;
            _httpClient = httpClient;
            _providerManager = providerManager;
        }

        private TimeSpan CacheLength
        {
            get
            {
                return TimeSpan.FromHours(6);
            }
        }

        public void AddParts(IEnumerable<IChannel> channels)
        {
            _channels = channels.ToArray();
        }

        public string ChannelDownloadPath
        {
            get
            {
                var options = _config.GetChannelsConfiguration();

                if (!string.IsNullOrWhiteSpace(options.DownloadPath))
                {
                    return options.DownloadPath;
                }

                return Path.Combine(_config.ApplicationPaths.ProgramDataPath, "channels");
            }
        }

        private IEnumerable<IChannel> GetAllChannels()
        {
            return _channels
                .OrderBy(i => i.Name);
        }

        public IEnumerable<Guid> GetInstalledChannelIds()
        {
            return GetAllChannels().Select(i => GetInternalChannelId(i.Name));
        }

        public Task<QueryResult<Channel>> GetChannelsInternal(ChannelQuery query, CancellationToken cancellationToken)
        {
            var user = string.IsNullOrWhiteSpace(query.UserId)
                ? null
                : _userManager.GetUserById(query.UserId);

            var channels = GetAllChannels()
                .Select(GetChannelEntity)
                .OrderBy(i => i.SortName)
                .ToList();

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
            if (query.IsFavorite.HasValue)
            {
                var val = query.IsFavorite.Value;
                channels = channels.Where(i => _userDataManager.GetUserData(user,  i).IsFavorite == val)
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
                        return GetChannelProvider(i).IsEnabledFor(user.Id.ToString("N"));
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

            var result = new QueryResult<Channel>
            {
                Items = returnItems,
                TotalRecordCount = totalCount
            };

            return Task.FromResult(result);
        }

        public async Task<QueryResult<BaseItemDto>> GetChannels(ChannelQuery query, CancellationToken cancellationToken)
        {
            var user = string.IsNullOrWhiteSpace(query.UserId)
                ? null
                : _userManager.GetUserById(query.UserId);

            var internalResult = await GetChannelsInternal(query, cancellationToken).ConfigureAwait(false);

            var dtoOptions = new DtoOptions();

            var returnItems = (await _dtoService.GetBaseItemDtos(internalResult.Items, dtoOptions, user).ConfigureAwait(false))
                .ToArray();

            var result = new QueryResult<BaseItemDto>
            {
                Items = returnItems,
                TotalRecordCount = internalResult.TotalRecordCount
            };

            return result;
        }

        public async Task RefreshChannels(IProgress<double> progress, CancellationToken cancellationToken)
        {
            _refreshedItems.Clear();

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
                    _logger.ErrorException("Error getting channel information for {0}", ex, channelInfo.Name);
                }

                numComplete++;
                double percent = numComplete;
                percent /= allChannelsList.Count;

                progress.Report(100 * percent);
            }

            progress.Report(100);
        }

        private Channel GetChannelEntity(IChannel channel)
        {
            var item = GetChannel(GetInternalChannelId(channel.Name).ToString("N"));

            if (item == null)
            {
                item = GetChannel(channel, CancellationToken.None).Result;
            }

            return item;
        }

        public async Task<IEnumerable<MediaSourceInfo>> GetStaticMediaSources(BaseItem item, bool includeCachedVersions, CancellationToken cancellationToken)
        {
            IEnumerable<ChannelMediaInfo> results = new List<ChannelMediaInfo>();
            var video = item as Video;
            if (video != null)
            {
                results = video.ChannelMediaSources;
            }
            var audio = item as Audio;
            if (audio != null)
            {
                results = audio.ChannelMediaSources ?? new List<ChannelMediaInfo>();
            }

            var sources = SortMediaInfoResults(results)
                .Select(i => GetMediaSource(item, i))
                .ToList();

            if (includeCachedVersions)
            {
                var cachedVersions = GetCachedChannelItemMediaSources(item);
                sources.InsertRange(0, cachedVersions);
            }

            return sources;
        }

        public async Task<IEnumerable<MediaSourceInfo>> GetDynamicMediaSources(BaseItem item, CancellationToken cancellationToken)
        {
            var channel = GetChannel(item.ChannelId);
            var channelPlugin = GetChannelProvider(channel);

            var requiresCallback = channelPlugin as IRequiresMediaInfoCallback;

            IEnumerable<ChannelMediaInfo> results;

            if (requiresCallback != null)
            {
                results = await GetChannelItemMediaSourcesInternal(requiresCallback, item.ExternalId, cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                results = new List<ChannelMediaInfo>();
            }

            var list = SortMediaInfoResults(results)
                .Select(i => GetMediaSource(item, i))
                .ToList();

            var cachedVersions = GetCachedChannelItemMediaSources(item);
            list.InsertRange(0, cachedVersions);

            return list;
        }

        private readonly ConcurrentDictionary<string, Tuple<DateTime, List<ChannelMediaInfo>>> _channelItemMediaInfo =
            new ConcurrentDictionary<string, Tuple<DateTime, List<ChannelMediaInfo>>>();

        private async Task<IEnumerable<ChannelMediaInfo>> GetChannelItemMediaSourcesInternal(IRequiresMediaInfoCallback channel, string id, CancellationToken cancellationToken)
        {
            Tuple<DateTime, List<ChannelMediaInfo>> cachedInfo;

            if (_channelItemMediaInfo.TryGetValue(id, out cachedInfo))
            {
                if ((DateTime.UtcNow - cachedInfo.Item1).TotalMinutes < 5)
                {
                    return cachedInfo.Item2;
                }
            }

            var mediaInfo = await channel.GetChannelItemMediaInfo(id, cancellationToken)
                   .ConfigureAwait(false);
            var list = mediaInfo.ToList();

            var item2 = new Tuple<DateTime, List<ChannelMediaInfo>>(DateTime.UtcNow, list);
            _channelItemMediaInfo.AddOrUpdate(id, item2, (key, oldValue) => item2);

            return list;
        }

        private IEnumerable<MediaSourceInfo> GetCachedChannelItemMediaSources(BaseItem item)
        {
            var filenamePrefix = item.Id.ToString("N");
            var parentPath = Path.Combine(ChannelDownloadPath, item.ChannelId);

            try
            {
                var files = _fileSystem.GetFiles(parentPath);

                if (string.Equals(item.MediaType, MediaType.Video, StringComparison.OrdinalIgnoreCase))
                {
                    files = files.Where(i => _libraryManager.IsVideoFile(i.FullName));
                }
                else
                {
                    files = files.Where(i => _libraryManager.IsAudioFile(i.FullName));
                }

                var file = files
                    .FirstOrDefault(i => i.Name.StartsWith(filenamePrefix, StringComparison.OrdinalIgnoreCase));

                if (file != null)
                {
                    var cachedItem = _libraryManager.ResolvePath(file);

                    if (cachedItem != null)
                    {
                        var hasMediaSources = _libraryManager.GetItemById(cachedItem.Id) as IHasMediaSources;

                        if (hasMediaSources != null)
                        {
                            var source = hasMediaSources.GetMediaSources(true).FirstOrDefault();

                            if (source != null)
                            {
                                return new[] { source };
                            }
                        }
                    }
                }
            }
            catch (DirectoryNotFoundException)
            {

            }

            return new List<MediaSourceInfo>();
        }

        private MediaSourceInfo GetMediaSource(BaseItem item, ChannelMediaInfo info)
        {
            var source = info.ToMediaSource();

            source.RunTimeTicks = source.RunTimeTicks ?? item.RunTimeTicks;

            return source;
        }

        private IEnumerable<ChannelMediaInfo> SortMediaInfoResults(IEnumerable<ChannelMediaInfo> channelMediaSources)
        {
            var list = channelMediaSources.ToList();

            var options = _config.GetChannelsConfiguration();

            var width = options.PreferredStreamingWidth;

            if (width.HasValue)
            {
                var val = width.Value;

                var res = list
                    .OrderBy(i => i.Width.HasValue && i.Width.Value <= val ? 0 : 1)
                    .ThenBy(i => Math.Abs((i.Width ?? 0) - val))
                    .ThenByDescending(i => i.Width ?? 0)
                    .ThenBy(list.IndexOf)
                    .ToList();


                return res;
            }

            return list
                .OrderByDescending(i => i.Width ?? 0)
                .ThenBy(list.IndexOf);
        }

        private async Task<Channel> GetChannel(IChannel channelInfo, CancellationToken cancellationToken)
        {
            var parentFolder = await GetInternalChannelFolder(cancellationToken).ConfigureAwait(false);
            var parentFolderId = parentFolder.Id;

            var id = GetInternalChannelId(channelInfo.Name);
            var idString = id.ToString("N");

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

            if (!string.Equals(item.ChannelId, idString, StringComparison.OrdinalIgnoreCase))
            {
                forceUpdate = true;
            }
            item.ChannelId = idString;

            if (item.ParentId != parentFolderId)
            {
                forceUpdate = true;
            }
            item.ParentId = parentFolderId;

            item.OfficialRating = GetOfficialRating(channelInfo.ParentalRating);
            item.Overview = channelInfo.Description;
            item.HomePageUrl = channelInfo.HomePageUrl;

            if (string.IsNullOrWhiteSpace(item.Name))
            {
                item.Name = channelInfo.Name;
            }

            if (isNew)
            {
                await _libraryManager.CreateItem(item, cancellationToken).ConfigureAwait(false);
            }
            else if (forceUpdate)
            {
                await item.UpdateToRepository(ItemUpdateType.None, cancellationToken).ConfigureAwait(false);
            }

            await item.RefreshMetadata(new MetadataRefreshOptions(_fileSystem), cancellationToken);
            return item;
        }

        private string GetOfficialRating(ChannelParentalRating rating)
        {
            switch (rating)
            {
                case ChannelParentalRating.Adult:
                    return "XXX";
                case ChannelParentalRating.UsR:
                    return "R";
                case ChannelParentalRating.UsPG13:
                    return "PG-13";
                case ChannelParentalRating.UsPG:
                    return "PG";
                default:
                    return null;
            }
        }

        public Channel GetChannel(string id)
        {
            return _libraryManager.GetItemById(id) as Channel;
        }

        public IEnumerable<ChannelFeatures> GetAllChannelFeatures()
        {
            return _libraryManager.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = new[] { typeof(Channel).Name },
                SortBy = new[] { ItemSortBy.SortName }

            }).Select(i => GetChannelFeatures(i.Id.ToString("N")));
        }

        public ChannelFeatures GetChannelFeatures(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentNullException("id");
            }

            var channel = GetChannel(id);
            var channelProvider = GetChannelProvider(channel);

            return GetChannelFeaturesDto(channel, channelProvider, channelProvider.GetChannelFeatures());
        }

        public bool SupportsSync(string channelId)
        {
            if (string.IsNullOrWhiteSpace(channelId))
            {
                throw new ArgumentNullException("channelId");
            }

            //var channel = GetChannel(channelId);
            var channelProvider = GetChannelProvider(channelId);

            return channelProvider.GetChannelFeatures().SupportsContentDownloading;
        }

        public ChannelFeatures GetChannelFeaturesDto(Channel channel,
            IChannel provider,
            InternalChannelFeatures features)
        {
            var isIndexable = provider is IIndexableChannel;
            var supportsLatest = provider is ISupportsLatestMedia;

            return new ChannelFeatures
            {
                CanFilter = !features.MaxPageSize.HasValue,
                CanSearch = provider is ISearchableChannel,
                ContentTypes = features.ContentTypes,
                DefaultSortFields = features.DefaultSortFields,
                MaxPageSize = features.MaxPageSize,
                MediaTypes = features.MediaTypes,
                SupportsSortOrderToggle = features.SupportsSortOrderToggle,
                SupportsLatestMedia = supportsLatest,
                Name = channel.Name,
                Id = channel.Id.ToString("N"),
                SupportsContentDownloading = features.SupportsContentDownloading && (isIndexable || supportsLatest),
                AutoRefreshLevels = features.AutoRefreshLevels
            };
        }

        private Guid GetInternalChannelId(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException("name");
            }
            return _libraryManager.GetNewItemId("Channel " + name, typeof(Channel));
        }

        public async Task<QueryResult<BaseItemDto>> GetLatestChannelItems(AllChannelMediaQuery query, CancellationToken cancellationToken)
        {
            var user = string.IsNullOrWhiteSpace(query.UserId)
                ? null
                : _userManager.GetUserById(query.UserId);

            var limit = query.Limit;

            // See below about parental control
            if (user != null)
            {
                query.StartIndex = null;
                query.Limit = null;
            }

            var internalResult = await GetLatestChannelItemsInternal(query, cancellationToken).ConfigureAwait(false);

            var items = internalResult.Items;
            var totalRecordCount = internalResult.TotalRecordCount;

            // Supporting parental control is a hack because it has to be done after querying the remote data source
            // This will get screwy if apps try to page, so limit to 10 results in an attempt to always keep them on the first page
            if (user != null)
            {
                items = items.Where(i => i.IsVisible(user))
                    .Take(limit ?? 10)
                    .ToArray();

                totalRecordCount = items.Length;
            }

            var dtoOptions = new DtoOptions();

            var returnItems = (await _dtoService.GetBaseItemDtos(items, dtoOptions, user).ConfigureAwait(false))
                .ToArray();

            var result = new QueryResult<BaseItemDto>
            {
                Items = returnItems,
                TotalRecordCount = totalRecordCount
            };

            return result;
        }

        public async Task<QueryResult<BaseItem>> GetLatestChannelItemsInternal(AllChannelMediaQuery query, CancellationToken cancellationToken)
        {
            var user = string.IsNullOrWhiteSpace(query.UserId)
                ? null
                : _userManager.GetUserById(query.UserId);

            if (!string.IsNullOrWhiteSpace(query.UserId) && user == null)
            {
                throw new ArgumentException("User not found.");
            }

            var channels = GetAllChannels();

            if (query.ChannelIds.Length > 0)
            {
                // Avoid implicitly captured closure
                var ids = query.ChannelIds;
                channels = channels
                    .Where(i => ids.Contains(GetInternalChannelId(i.Name).ToString("N")))
                    .ToArray();
            }

            // Avoid implicitly captured closure
            var userId = query.UserId;

            var tasks = channels
                .Select(async i =>
                {
                    var indexable = i as ISupportsLatestMedia;

                    if (indexable != null)
                    {
                        try
                        {
                            var result = await GetLatestItems(indexable, i, userId, cancellationToken).ConfigureAwait(false);

                            var resultItems = result.ToList();

                            return new Tuple<IChannel, ChannelItemResult>(i, new ChannelItemResult
                            {
                                Items = resultItems,
                                TotalRecordCount = resultItems.Count
                            });
                        }
                        catch (Exception ex)
                        {
                            _logger.ErrorException("Error getting all media from {0}", ex, i.Name);
                        }
                    }
                    return new Tuple<IChannel, ChannelItemResult>(i, new ChannelItemResult());
                });

            var results = await Task.WhenAll(tasks).ConfigureAwait(false);

            var totalCount = results.Length;

            IEnumerable<Tuple<IChannel, ChannelItemInfo>> items = results
                .SelectMany(i => i.Item2.Items.Select(m => new Tuple<IChannel, ChannelItemInfo>(i.Item1, m)));

            if (query.ContentTypes.Length > 0)
            {
                // Avoid implicitly captured closure
                var contentTypes = query.ContentTypes;

                items = items.Where(i => contentTypes.Contains(i.Item2.ContentType));
            }
            if (query.ExtraTypes.Length > 0)
            {
                // Avoid implicitly captured closure
                var contentTypes = query.ExtraTypes;

                items = items.Where(i => contentTypes.Contains(i.Item2.ExtraType));
            }

            // Avoid implicitly captured closure
            var token = cancellationToken;
            var itemTasks = items.Select(i =>
            {
                var channelProvider = i.Item1;
                var internalChannelId = GetInternalChannelId(channelProvider.Name);
                return GetChannelItemEntity(i.Item2, channelProvider, internalChannelId, token);
            });

            var internalItems = await Task.WhenAll(itemTasks).ConfigureAwait(false);

            internalItems = ApplyFilters(internalItems, query.Filters, user).ToArray();
            RefreshIfNeeded(internalItems);

            if (query.StartIndex.HasValue)
            {
                internalItems = internalItems.Skip(query.StartIndex.Value).ToArray();
            }
            if (query.Limit.HasValue)
            {
                internalItems = internalItems.Take(query.Limit.Value).ToArray();
            }

            var returnItemArray = internalItems.ToArray();

            return new QueryResult<BaseItem>
            {
                TotalRecordCount = totalCount,
                Items = returnItemArray
            };
        }

        private async Task<IEnumerable<ChannelItemInfo>> GetLatestItems(ISupportsLatestMedia indexable, IChannel channel, string userId, CancellationToken cancellationToken)
        {
            var cacheLength = CacheLength;
            var cachePath = GetChannelDataCachePath(channel, userId, "channelmanager-latest", null, false);

            try
            {
                if (_fileSystem.GetLastWriteTimeUtc(cachePath).Add(cacheLength) > DateTime.UtcNow)
                {
                    return _jsonSerializer.DeserializeFromFile<List<ChannelItemInfo>>(cachePath);
                }
            }
            catch (FileNotFoundException)
            {

            }
            catch (DirectoryNotFoundException)
            {

            }

            await _resourcePool.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                try
                {
                    if (_fileSystem.GetLastWriteTimeUtc(cachePath).Add(cacheLength) > DateTime.UtcNow)
                    {
                        return _jsonSerializer.DeserializeFromFile<List<ChannelItemInfo>>(cachePath);
                    }
                }
                catch (FileNotFoundException)
                {

                }
                catch (DirectoryNotFoundException)
                {

                }

                var result = await indexable.GetLatestMedia(new ChannelLatestMediaSearch
                {
                    UserId = userId

                }, cancellationToken).ConfigureAwait(false);

                var resultItems = result.ToList();

                CacheResponse(resultItems, cachePath);

                return resultItems;
            }
            finally
            {
                _resourcePool.Release();
            }
        }

        public async Task<QueryResult<BaseItem>> GetAllMediaInternal(AllChannelMediaQuery query, CancellationToken cancellationToken)
        {
            var channels = GetAllChannels();

            if (query.ChannelIds.Length > 0)
            {
                // Avoid implicitly captured closure
                var ids = query.ChannelIds;
                channels = channels
                    .Where(i => ids.Contains(GetInternalChannelId(i.Name).ToString("N")))
                    .ToArray();
            }

            var tasks = channels
                .Select(async i =>
                {
                    var indexable = i as IIndexableChannel;

                    if (indexable != null)
                    {
                        try
                        {
                            var result = await GetAllItems(indexable, i, new InternalAllChannelMediaQuery
                            {
                                UserId = query.UserId,
                                ContentTypes = query.ContentTypes,
                                ExtraTypes = query.ExtraTypes,
                                TrailerTypes = query.TrailerTypes

                            }, cancellationToken).ConfigureAwait(false);

                            return new Tuple<IChannel, ChannelItemResult>(i, result);
                        }
                        catch (Exception ex)
                        {
                            _logger.ErrorException("Error getting all media from {0}", ex, i.Name);
                        }
                    }
                    return new Tuple<IChannel, ChannelItemResult>(i, new ChannelItemResult());
                });

            var results = await Task.WhenAll(tasks).ConfigureAwait(false);

            var totalCount = results.Length;

            IEnumerable<Tuple<IChannel, ChannelItemInfo>> items = results
                .SelectMany(i => i.Item2.Items.Select(m => new Tuple<IChannel, ChannelItemInfo>(i.Item1, m)))
                .OrderBy(i => i.Item2.Name);

            if (query.StartIndex.HasValue)
            {
                items = items.Skip(query.StartIndex.Value);
            }
            if (query.Limit.HasValue)
            {
                items = items.Take(query.Limit.Value);
            }

            // Avoid implicitly captured closure
            var token = cancellationToken;
            var itemTasks = items.Select(i =>
            {
                var channelProvider = i.Item1;
                var internalChannelId = GetInternalChannelId(channelProvider.Name);
                return GetChannelItemEntity(i.Item2, channelProvider, internalChannelId, token);
            });

            var internalItems = await Task.WhenAll(itemTasks).ConfigureAwait(false);

            var returnItemArray = internalItems.ToArray();

            return new QueryResult<BaseItem>
            {
                TotalRecordCount = totalCount,
                Items = returnItemArray
            };
        }

        public async Task<QueryResult<BaseItemDto>> GetAllMedia(AllChannelMediaQuery query, CancellationToken cancellationToken)
        {
            var user = string.IsNullOrWhiteSpace(query.UserId)
                ? null
                : _userManager.GetUserById(query.UserId);

            var internalResult = await GetAllMediaInternal(query, cancellationToken).ConfigureAwait(false);

            RefreshIfNeeded(internalResult.Items);

            var dtoOptions = new DtoOptions();

            var returnItems = (await _dtoService.GetBaseItemDtos(internalResult.Items, dtoOptions, user).ConfigureAwait(false))
                .ToArray();

            var result = new QueryResult<BaseItemDto>
            {
                Items = returnItems,
                TotalRecordCount = internalResult.TotalRecordCount
            };

            return result;
        }

        private async Task<ChannelItemResult> GetAllItems(IIndexableChannel indexable, IChannel channel, InternalAllChannelMediaQuery query, CancellationToken cancellationToken)
        {
            var cacheLength = CacheLength;
            var folderId = _jsonSerializer.SerializeToString(query).GetMD5().ToString("N");
            var cachePath = GetChannelDataCachePath(channel, query.UserId, folderId, null, false);

            try
            {
                if (_fileSystem.GetLastWriteTimeUtc(cachePath).Add(cacheLength) > DateTime.UtcNow)
                {
                    return _jsonSerializer.DeserializeFromFile<ChannelItemResult>(cachePath);
                }
            }
            catch (FileNotFoundException)
            {

            }
            catch (DirectoryNotFoundException)
            {

            }

            await _resourcePool.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                try
                {
                    if (_fileSystem.GetLastWriteTimeUtc(cachePath).Add(cacheLength) > DateTime.UtcNow)
                    {
                        return _jsonSerializer.DeserializeFromFile<ChannelItemResult>(cachePath);
                    }
                }
                catch (FileNotFoundException)
                {

                }
                catch (DirectoryNotFoundException)
                {

                }

                var result = await indexable.GetAllMedia(query, cancellationToken).ConfigureAwait(false);

                CacheResponse(result, cachePath);

                return result;
            }
            finally
            {
                _resourcePool.Release();
            }
        }

        public async Task<QueryResult<BaseItem>> GetChannelItemsInternal(ChannelItemQuery query, IProgress<double> progress, CancellationToken cancellationToken)
        {
            // Get the internal channel entity
            var channel = GetChannel(query.ChannelId);

            // Find the corresponding channel provider plugin
            var channelProvider = GetChannelProvider(channel);

            var channelInfo = channelProvider.GetChannelFeatures();

            int? providerStartIndex = null;
            int? providerLimit = null;

            if (channelInfo.MaxPageSize.HasValue)
            {
                providerStartIndex = query.StartIndex;

                if (query.Limit.HasValue && query.Limit.Value > channelInfo.MaxPageSize.Value)
                {
                    query.Limit = Math.Min(query.Limit.Value, channelInfo.MaxPageSize.Value);
                }
                providerLimit = query.Limit;

                // This will cause some providers to fail
                if (providerLimit == 0)
                {
                    providerLimit = 1;
                }
            }

            var user = string.IsNullOrWhiteSpace(query.UserId)
                ? null
                : _userManager.GetUserById(query.UserId);

            ChannelItemSortField? sortField = null;
            ChannelItemSortField parsedField;
            if (query.SortBy.Length == 1 &&
                Enum.TryParse(query.SortBy[0], true, out parsedField))
            {
                sortField = parsedField;
            }

            var sortDescending = query.SortOrder.HasValue && query.SortOrder.Value == SortOrder.Descending;

            var itemsResult = await GetChannelItems(channelProvider,
                user,
                query.FolderId,
                providerStartIndex,
                providerLimit,
                sortField,
                sortDescending,
                cancellationToken)
                .ConfigureAwait(false);

            var providerTotalRecordCount = providerLimit.HasValue ? itemsResult.TotalRecordCount : null;

            var tasks = itemsResult.Items.Select(i => GetChannelItemEntity(i, channelProvider, channel.Id, cancellationToken));

            var internalItems = await Task.WhenAll(tasks).ConfigureAwait(false);

            if (user != null)
            {
                internalItems = internalItems.Where(i => i.IsVisible(user)).ToArray();

                if (providerTotalRecordCount.HasValue)
                {
                    providerTotalRecordCount = providerTotalRecordCount.Value;
                }
            }

            return await GetReturnItems(internalItems, providerTotalRecordCount, user, query).ConfigureAwait(false);
        }

        public async Task<QueryResult<BaseItemDto>> GetChannelItems(ChannelItemQuery query, CancellationToken cancellationToken)
        {
            var user = string.IsNullOrWhiteSpace(query.UserId)
                ? null
                : _userManager.GetUserById(query.UserId);

            var internalResult = await GetChannelItemsInternal(query, new Progress<double>(), cancellationToken).ConfigureAwait(false);

            var dtoOptions = new DtoOptions();

            var returnItems = (await _dtoService.GetBaseItemDtos(internalResult.Items, dtoOptions, user).ConfigureAwait(false))
                .ToArray();

            var result = new QueryResult<BaseItemDto>
            {
                Items = returnItems,
                TotalRecordCount = internalResult.TotalRecordCount
            };

            return result;
        }

        private readonly SemaphoreSlim _resourcePool = new SemaphoreSlim(1, 1);
        private async Task<ChannelItemResult> GetChannelItems(IChannel channel,
            User user,
            string folderId,
            int? startIndex,
            int? limit,
            ChannelItemSortField? sortField,
            bool sortDescending,
            CancellationToken cancellationToken)
        {
            var userId = user.Id.ToString("N");

            var cacheLength = CacheLength;
            var cachePath = GetChannelDataCachePath(channel, userId, folderId, sortField, sortDescending);

            try
            {
                if (!startIndex.HasValue && !limit.HasValue)
                {
                    if (_fileSystem.GetLastWriteTimeUtc(cachePath).Add(cacheLength) > DateTime.UtcNow)
                    {
                        return _jsonSerializer.DeserializeFromFile<ChannelItemResult>(cachePath);
                    }
                }
            }
            catch (FileNotFoundException)
            {

            }
            catch (DirectoryNotFoundException)
            {

            }

            await _resourcePool.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                try
                {
                    if (!startIndex.HasValue && !limit.HasValue)
                    {
                        if (_fileSystem.GetLastWriteTimeUtc(cachePath).Add(cacheLength) > DateTime.UtcNow)
                        {
                            return _jsonSerializer.DeserializeFromFile<ChannelItemResult>(cachePath);
                        }
                    }
                }
                catch (FileNotFoundException)
                {

                }
                catch (DirectoryNotFoundException)
                {

                }

                var query = new InternalChannelItemQuery
                {
                    UserId = userId,
                    StartIndex = startIndex,
                    Limit = limit,
                    SortBy = sortField,
                    SortDescending = sortDescending
                };

                if (!string.IsNullOrWhiteSpace(folderId))
                {
                    var categoryItem = _libraryManager.GetItemById(new Guid(folderId));

                    query.FolderId = categoryItem.ExternalId;
                }

                var result = await channel.GetChannelItems(query, cancellationToken).ConfigureAwait(false);

                if (!startIndex.HasValue && !limit.HasValue)
                {
                    CacheResponse(result, cachePath);
                }

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
                _fileSystem.CreateDirectory(Path.GetDirectoryName(path));

                _jsonSerializer.SerializeToFile(result, path);
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error writing to channel cache file: {0}", ex, path);
            }
        }

        private string GetChannelDataCachePath(IChannel channel,
            string userId,
            string folderId,
            ChannelItemSortField? sortField,
            bool sortDescending)
        {
            var channelId = GetInternalChannelId(channel.Name).ToString("N");

            var userCacheKey = string.Empty;

            var hasCacheKey = channel as IHasCacheKey;
            if (hasCacheKey != null)
            {
                userCacheKey = hasCacheKey.GetCacheKey(userId) ?? string.Empty;
            }

            var filename = string.IsNullOrWhiteSpace(folderId) ? "root" : folderId;
            filename += userCacheKey;

            var version = (channel.DataVersion ?? string.Empty).GetMD5().ToString("N");

            if (sortField.HasValue)
            {
                filename += "-sortField-" + sortField.Value;
            }
            if (sortDescending)
            {
                filename += "-sortDescending";
            }

            filename = filename.GetMD5().ToString("N");

            return Path.Combine(_config.ApplicationPaths.CachePath,
                "channels",
                channelId,
                version,
                filename + ".json");
        }

        private async Task<QueryResult<BaseItem>> GetReturnItems(IEnumerable<BaseItem> items,
            int? totalCountFromProvider,
            User user,
            ChannelItemQuery query)
        {
            items = ApplyFilters(items, query.Filters, user);

            items = _libraryManager.Sort(items, user, query.SortBy, query.SortOrder ?? SortOrder.Ascending);

            var all = items.ToList();
            var totalCount = totalCountFromProvider ?? all.Count;

            if (!totalCountFromProvider.HasValue)
            {
                if (query.StartIndex.HasValue)
                {
                    all = all.Skip(query.StartIndex.Value).ToList();
                }
                if (query.Limit.HasValue)
                {
                    all = all.Take(query.Limit.Value).ToList();
                }
            }

            var returnItemArray = all.ToArray();
            RefreshIfNeeded(returnItemArray);

            return new QueryResult<BaseItem>
            {
                Items = returnItemArray,
                TotalRecordCount = totalCount
            };
        }

        private string GetIdToHash(string externalId, string channelName)
        {
            // Increment this as needed to force new downloads
            // Incorporate Name because it's being used to convert channel entity to provider
            return externalId + (channelName ?? string.Empty) + "16";
        }

        private T GetItemById<T>(string idString, string channelName, string channnelDataVersion, out bool isNew)
            where T : BaseItem, new()
        {
            var id = GetIdToHash(idString, channelName).GetMBId(typeof(T));

            T item = null;

            try
            {
                item = _libraryManager.GetItemById(id) as T;
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error retrieving channel item from database", ex);
            }

            if (item == null || !string.Equals(item.ExternalEtag, channnelDataVersion, StringComparison.Ordinal))
            {
                item = new T();
                isNew = true;
            }
            else
            {
                isNew = false;
            }

            item.ExternalEtag = channnelDataVersion;
            item.Id = id;
            return item;
        }

        private async Task<BaseItem> GetChannelItemEntity(ChannelItemInfo info, IChannel channelProvider, Guid internalChannelId, CancellationToken cancellationToken)
        {
            BaseItem item;
            bool isNew;
            bool forceUpdate = false;

            if (info.Type == ChannelItemType.Folder)
            {
                if (info.FolderType == ChannelFolderType.MusicAlbum)
                {
                    item = GetItemById<MusicAlbum>(info.Id, channelProvider.Name, channelProvider.DataVersion, out isNew);
                }
                else if (info.FolderType == ChannelFolderType.MusicArtist)
                {
                    item = GetItemById<MusicArtist>(info.Id, channelProvider.Name, channelProvider.DataVersion, out isNew);
                }
                else if (info.FolderType == ChannelFolderType.PhotoAlbum)
                {
                    item = GetItemById<PhotoAlbum>(info.Id, channelProvider.Name, channelProvider.DataVersion, out isNew);
                }
                else if (info.FolderType == ChannelFolderType.Series)
                {
                    item = GetItemById<Series>(info.Id, channelProvider.Name, channelProvider.DataVersion, out isNew);
                }
                else if (info.FolderType == ChannelFolderType.Season)
                {
                    item = GetItemById<Season>(info.Id, channelProvider.Name, channelProvider.DataVersion, out isNew);
                }
                else
                {
                    item = GetItemById<Folder>(info.Id, channelProvider.Name, channelProvider.DataVersion, out isNew);
                }
            }
            else if (info.MediaType == ChannelMediaType.Audio)
            {
                if (info.ContentType == ChannelMediaContentType.Podcast)
                {
                    item = GetItemById<AudioPodcast>(info.Id, channelProvider.Name, channelProvider.DataVersion, out isNew);
                }
                else
                {
                    item = GetItemById<Audio>(info.Id, channelProvider.Name, channelProvider.DataVersion, out isNew);
                }
            }
            else
            {
                if (info.ContentType == ChannelMediaContentType.Episode)
                {
                    item = GetItemById<Episode>(info.Id, channelProvider.Name, channelProvider.DataVersion, out isNew);
                }
                else if (info.ContentType == ChannelMediaContentType.Movie)
                {
                    item = GetItemById<Movie>(info.Id, channelProvider.Name, channelProvider.DataVersion, out isNew);
                }
                else if (info.ContentType == ChannelMediaContentType.Trailer || info.ExtraType == ExtraType.Trailer)
                {
                    item = GetItemById<Trailer>(info.Id, channelProvider.Name, channelProvider.DataVersion, out isNew);
                }
                else
                {
                    item = GetItemById<Video>(info.Id, channelProvider.Name, channelProvider.DataVersion, out isNew);
                }
            }

            item.RunTimeTicks = info.RunTimeTicks;

            if (isNew)
            {
                item.Name = info.Name;
                item.Genres = info.Genres;
                item.Studios = info.Studios;
                item.CommunityRating = info.CommunityRating;
                item.Overview = info.Overview;
                item.IndexNumber = info.IndexNumber;
                item.ParentIndexNumber = info.ParentIndexNumber;
                item.PremiereDate = info.PremiereDate;
                item.ProductionYear = info.ProductionYear;
                item.ProviderIds = info.ProviderIds;
                item.OfficialRating = info.OfficialRating;
                item.DateCreated = info.DateCreated ?? DateTime.UtcNow;
                item.Tags = info.Tags;
                item.HomePageUrl = info.HomePageUrl;
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

            var hasArtists = item as IHasArtist;
            if (hasArtists != null)
            {
                hasArtists.Artists = info.Artists;
            }

            var hasAlbumArtists = item as IHasAlbumArtist;
            if (hasAlbumArtists != null)
            {
                hasAlbumArtists.AlbumArtists = info.AlbumArtists;
            }

            var trailer = item as Trailer;
            if (trailer != null)
            {
                if (!info.TrailerTypes.SequenceEqual(trailer.TrailerTypes))
                {
                    forceUpdate = true;
                }
                trailer.TrailerTypes = info.TrailerTypes;
            }

            item.ChannelId = internalChannelId.ToString("N");

            if (item.ParentId != internalChannelId)
            {
                forceUpdate = true;
            }
            item.ParentId = internalChannelId;

            if (!string.Equals(item.ExternalId, info.Id, StringComparison.OrdinalIgnoreCase))
            {
                forceUpdate = true;
            }
            item.ExternalId = info.Id;

            var channelAudioItem = item as Audio;
            if (channelAudioItem != null)
            {
                channelAudioItem.ExtraType = info.ExtraType;
                channelAudioItem.ChannelMediaSources = info.MediaSources;

                var mediaSource = info.MediaSources.FirstOrDefault();
                item.Path = mediaSource == null ? null : mediaSource.Path;
            }

            var channelVideoItem = item as Video;
            if (channelVideoItem != null)
            {
                channelVideoItem.ExtraType = info.ExtraType;
                channelVideoItem.ChannelMediaSources = info.MediaSources;

                var mediaSource = info.MediaSources.FirstOrDefault();
                item.Path = mediaSource == null ? null : mediaSource.Path;
            }

            if (!string.IsNullOrWhiteSpace(info.ImageUrl) && !item.HasImage(ImageType.Primary))
            {
                item.SetImagePath(ImageType.Primary, info.ImageUrl);
            }

            if (item.SourceType != SourceType.Channel)
            {
                item.SourceType = SourceType.Channel;
                forceUpdate = true;
            }

            if (isNew)
            {
                await _libraryManager.CreateItem(item, cancellationToken).ConfigureAwait(false);

                if (info.People != null && info.People.Count > 0)
                {
                    await _libraryManager.UpdatePeople(item, info.People ?? new List<PersonInfo>()).ConfigureAwait(false);
                }
            }
            else if (forceUpdate)
            {
                await item.UpdateToRepository(ItemUpdateType.None, cancellationToken).ConfigureAwait(false);
            }

            return item;
        }

        private void RefreshIfNeeded(BaseItem[] programs)
        {
            foreach (var program in programs)
            {
                RefreshIfNeeded(program);
            }
        }

        private void RefreshIfNeeded(BaseItem program)
        {
            if (!_refreshedItems.ContainsKey(program.Id))
            {
                _refreshedItems.TryAdd(program.Id, true);
                _providerManager.QueueRefresh(program.Id, new MetadataRefreshOptions(_fileSystem));
            }

        }

        internal IChannel GetChannelProvider(Channel channel)
        {
            if (channel == null)
            {
                throw new ArgumentNullException("channel");
            }

            var result = GetAllChannels()
                .FirstOrDefault(i => string.Equals(GetInternalChannelId(i.Name).ToString("N"), channel.ChannelId, StringComparison.OrdinalIgnoreCase) || string.Equals(i.Name, channel.Name, StringComparison.OrdinalIgnoreCase));

            if (result == null)
            {
                throw new ResourceNotFoundException("No channel provider found for channel " + channel.Name);
            }

            return result;
        }

        internal IChannel GetChannelProvider(string internalChannelId)
        {
            if (internalChannelId == null)
            {
                throw new ArgumentNullException("internalChannelId");
            }

            var result = GetAllChannels()
                .FirstOrDefault(i => string.Equals(GetInternalChannelId(i.Name).ToString("N"), internalChannelId, StringComparison.OrdinalIgnoreCase));

            if (result == null)
            {
                throw new ResourceNotFoundException("No channel provider found for channel id " + internalChannelId);
            }

            return result;
        }

        private IEnumerable<BaseItem> ApplyFilters(IEnumerable<BaseItem> items, IEnumerable<ItemFilter> filters, User user)
        {
            foreach (var filter in filters.OrderByDescending(f => (int)f))
            {
                items = ApplyFilter(items, filter, user);
            }

            return items;
        }

        private IEnumerable<BaseItem> ApplyFilter(IEnumerable<BaseItem> items, ItemFilter filter, User user)
        {
            // Avoid implicitly captured closure
            var currentUser = user;

            switch (filter)
            {
                case ItemFilter.IsFavoriteOrLikes:
                    return items.Where(item =>
                    {
                        var userdata = _userDataManager.GetUserData(user, item);

                        if (userdata == null)
                        {
                            return false;
                        }

                        var likes = userdata.Likes ?? false;
                        var favorite = userdata.IsFavorite;

                        return likes || favorite;
                    });

                case ItemFilter.Likes:
                    return items.Where(item =>
                    {
                        var userdata = _userDataManager.GetUserData(user, item);

                        return userdata != null && userdata.Likes.HasValue && userdata.Likes.Value;
                    });

                case ItemFilter.Dislikes:
                    return items.Where(item =>
                    {
                        var userdata = _userDataManager.GetUserData(user, item);

                        return userdata != null && userdata.Likes.HasValue && !userdata.Likes.Value;
                    });

                case ItemFilter.IsFavorite:
                    return items.Where(item =>
                    {
                        var userdata = _userDataManager.GetUserData(user, item);

                        return userdata != null && userdata.IsFavorite;
                    });

                case ItemFilter.IsResumable:
                    return items.Where(item =>
                    {
                        var userdata = _userDataManager.GetUserData(user, item);

                        return userdata != null && userdata.PlaybackPositionTicks > 0;
                    });

                case ItemFilter.IsPlayed:
                    return items.Where(item => item.IsPlayed(currentUser));

                case ItemFilter.IsUnplayed:
                    return items.Where(item => item.IsUnplayed(currentUser));

                case ItemFilter.IsFolder:
                    return items.Where(item => item.IsFolder);

                case ItemFilter.IsNotFolder:
                    return items.Where(item => !item.IsFolder);
            }

            return items;
        }

        public async Task<BaseItemDto> GetChannelFolder(string userId, CancellationToken cancellationToken)
        {
            var user = string.IsNullOrEmpty(userId) ? null : _userManager.GetUserById(userId);

            var folder = await GetInternalChannelFolder(cancellationToken).ConfigureAwait(false);

            return _dtoService.GetBaseItemDto(folder, new DtoOptions(), user);
        }

        public async Task<Folder> GetInternalChannelFolder(CancellationToken cancellationToken)
        {
            var name = _localization.GetLocalizedString("ViewTypeChannels");

            return await _libraryManager.GetNamedView(name, "channels", "zz_" + name, cancellationToken).ConfigureAwait(false);
        }
    }
}