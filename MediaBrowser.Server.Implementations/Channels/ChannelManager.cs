using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Localization;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Channels
{
    public class ChannelManager : IChannelManager, IDisposable
    {
        private IChannel[] _channels;
        private IChannelFactory[] _factories;
        private List<Channel> _channelEntities = new List<Channel>();

        private readonly IUserManager _userManager;
        private readonly IUserDataManager _userDataManager;
        private readonly IDtoService _dtoService;
        private readonly ILibraryManager _libraryManager;
        private readonly ILogger _logger;
        private readonly IServerConfigurationManager _config;
        private readonly IFileSystem _fileSystem;
        private readonly IJsonSerializer _jsonSerializer;

        private readonly ILocalizationManager _localization;
        private readonly ConcurrentDictionary<Guid, bool> _refreshedItems = new ConcurrentDictionary<Guid, bool>();

        private Timer _refreshTimer;

        public ChannelManager(IUserManager userManager, IDtoService dtoService, ILibraryManager libraryManager, ILogger logger, IServerConfigurationManager config, IFileSystem fileSystem, IUserDataManager userDataManager, IJsonSerializer jsonSerializer, ILocalizationManager localization)
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

            _refreshTimer = new Timer(s => _refreshedItems.Clear(), null, TimeSpan.FromHours(3), TimeSpan.FromHours(3));
        }

        private TimeSpan CacheLength
        {
            get
            {
                return TimeSpan.FromHours(12);
            }
        }

        public void AddParts(IEnumerable<IChannel> channels, IEnumerable<IChannelFactory> factories)
        {
            _channels = channels.ToArray();
            _factories = factories.ToArray();
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
            return _factories
                .SelectMany(i =>
                {
                    try
                    {
                        return i.GetChannels().ToList();
                    }
                    catch (Exception ex)
                    {
                        _logger.ErrorException("Error getting channel list", ex);
                        return new List<IChannel>();
                    }
                })
                .Concat(_channels)
                .OrderBy(i => i.Name);
        }

        public Task<QueryResult<BaseItemDto>> GetChannels(ChannelQuery query, CancellationToken cancellationToken)
        {
            var user = string.IsNullOrWhiteSpace(query.UserId)
                ? null
                : _userManager.GetUserById(new Guid(query.UserId));

            var channels = _channelEntities.OrderBy(i => i.SortName).ToList();

            if (query.SupportsLatestItems.HasValue)
            {
                var val = query.SupportsLatestItems.Value;
                channels = channels.Where(i => (GetChannelProvider(i) is ISupportsLatestMedia) == val)
                    .ToList();
            }
            if (query.IsFavorite.HasValue)
            {
                var val = query.IsFavorite.Value;
                channels = channels.Where(i => _userDataManager.GetUserData(user.Id, i.GetUserDataKey()).IsFavorite == val)
                    .ToList();
            }

            if (user != null)
            {
                channels = channels.Where(i => GetChannelProvider(i).IsEnabledFor(user.Id.ToString("N")) && i.IsVisible(user))
                    .ToList();
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

            // Get everything
            var fields = Enum.GetNames(typeof(ItemFields))
                    .Select(i => (ItemFields)Enum.Parse(typeof(ItemFields), i, true))
                    .ToList();

            var returnItems = all.Select(i => _dtoService.GetBaseItemDto(i, fields, user))
                .ToArray();

            var result = new QueryResult<BaseItemDto>
            {
                Items = returnItems,
                TotalRecordCount = totalCount
            };

            return Task.FromResult(result);
        }

        public async Task RefreshChannels(IProgress<double> progress, CancellationToken cancellationToken)
        {
            var allChannelsList = GetAllChannels().ToList();

            var list = new List<Channel>();

            var numComplete = 0;

            foreach (var channelInfo in allChannelsList)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var item = await GetChannel(channelInfo, cancellationToken).ConfigureAwait(false);

                    list.Add(item);

                    _libraryManager.RegisterItem(item);
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

            _channelEntities = list.ToList();
            progress.Report(100);
        }

        public async Task<IEnumerable<MediaSourceInfo>> GetChannelItemMediaSources(string id, CancellationToken cancellationToken)
        {
            var item = (IChannelMediaItem)_libraryManager.GetItemById(id);

            var channelGuid = new Guid(item.ChannelId);
            var channel = _channelEntities.First(i => i.Id == channelGuid);
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
                results = item.ChannelMediaSources;
            }

            var sources = SortMediaInfoResults(results).Select(i => GetMediaSource(item, i))
                .ToList();

            var cachedVersions = GetCachedChannelItemMediaSources(item);

            sources.InsertRange(0, cachedVersions);

            return sources;
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

        public IEnumerable<MediaSourceInfo> GetCachedChannelItemMediaSources(string id)
        {
            var item = (IChannelMediaItem)_libraryManager.GetItemById(id);

            return GetCachedChannelItemMediaSources(item);
        }

        public IEnumerable<MediaSourceInfo> GetCachedChannelItemMediaSources(IChannelMediaItem item)
        {
            var filenamePrefix = item.Id.ToString("N");
            var parentPath = Path.Combine(ChannelDownloadPath, item.ChannelId);

            try
            {
                var files = new DirectoryInfo(parentPath).EnumerateFiles("*", SearchOption.TopDirectoryOnly);

                if (string.Equals(item.MediaType, MediaType.Video, StringComparison.OrdinalIgnoreCase))
                {
                    files = files.Where(i => EntityResolutionHelper.IsVideoFile(i.FullName));
                }
                else
                {
                    files = files.Where(i => EntityResolutionHelper.IsAudioFile(i.FullName));
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
                                source.Type = MediaSourceType.Cache;
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

        private MediaSourceInfo GetMediaSource(IChannelMediaItem item, ChannelMediaInfo info)
        {
            var id = info.Path.GetMD5().ToString("N");

            var source = new MediaSourceInfo
            {
                MediaStreams = GetMediaStreams(info).ToList(),

                Container = info.Container,
                Protocol = info.Protocol,
                Path = info.Path,
                RequiredHttpHeaders = info.RequiredHttpHeaders,
                RunTimeTicks = item.RunTimeTicks,
                Name = id,
                Id = id
            };

            return source;
        }

        private IEnumerable<MediaStream> GetMediaStreams(ChannelMediaInfo info)
        {
            var list = new List<MediaStream>();

            if (!string.IsNullOrWhiteSpace(info.VideoCodec) &&
                !string.IsNullOrWhiteSpace(info.AudioCodec))
            {
                list.Add(new MediaStream
                {
                    Type = MediaStreamType.Video,
                    Width = info.Width,
                    RealFrameRate = info.Framerate,
                    Profile = info.VideoProfile,
                    Level = info.VideoLevel,
                    Index = -1,
                    Height = info.Height,
                    Codec = info.VideoCodec,
                    BitRate = info.VideoBitrate,
                    AverageFrameRate = info.Framerate
                });

                list.Add(new MediaStream
                {
                    Type = MediaStreamType.Audio,
                    Index = -1,
                    Codec = info.AudioCodec,
                    BitRate = info.AudioBitrate,
                    Channels = info.AudioChannels,
                    SampleRate = info.AudioSampleRate
                });
            }

            return list;
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
                    .OrderBy(i => (i.Width.HasValue && i.Width.Value <= val ? 0 : 1))
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
            var path = Path.Combine(_config.ApplicationPaths.ItemsByNamePath, "channels", _fileSystem.GetValidFilename(channelInfo.Name));

            var fileInfo = new DirectoryInfo(path);

            var isNew = false;

            if (!fileInfo.Exists)
            {
                _logger.Debug("Creating directory {0}", path);

                Directory.CreateDirectory(path);
                fileInfo = new DirectoryInfo(path);

                if (!fileInfo.Exists)
                {
                    throw new IOException("Path not created: " + path);
                }

                isNew = true;
            }

            var id = GetInternalChannelId(channelInfo.Name);

            var item = _libraryManager.GetItemById(id) as Channel;

            if (item == null)
            {
                item = new Channel
                {
                    Name = channelInfo.Name,
                    Id = id,
                    DateCreated = _fileSystem.GetCreationTimeUtc(fileInfo),
                    DateModified = _fileSystem.GetLastWriteTimeUtc(fileInfo),
                    Path = path
                };

                isNew = true;
            }

            item.OfficialRating = GetOfficialRating(channelInfo.ParentalRating);
            item.Overview = channelInfo.Description;
            item.HomePageUrl = channelInfo.HomePageUrl;
            item.OriginalChannelName = channelInfo.Name;

            if (string.IsNullOrEmpty(item.Name))
            {
                item.Name = channelInfo.Name;
            }

            await item.RefreshMetadata(new MetadataRefreshOptions
            {
                ForceSave = isNew

            }, cancellationToken);

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
            return (Channel)_libraryManager.GetItemById(new Guid(id));
        }

        public IEnumerable<ChannelFeatures> GetAllChannelFeatures()
        {
            return _channelEntities
                .OrderBy(i => i.SortName)
                .Select(i => GetChannelFeatures(i.Id.ToString("N")));
        }

        public ChannelFeatures GetChannelFeatures(string id)
        {
            var channel = GetChannel(id);

            var channelProvider = GetChannelProvider(channel);

            return GetChannelFeaturesDto(channel, channelProvider, channelProvider.GetChannelFeatures());
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
                SupportsContentDownloading = isIndexable || supportsLatest
            };
        }

        private Guid GetInternalChannelId(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException("name");
            }

            return ("Channel " + name).GetMBId(typeof(Channel));
        }

        public async Task<QueryResult<BaseItemDto>> GetLatestChannelItems(AllChannelMediaQuery query, CancellationToken cancellationToken)
        {
            var user = string.IsNullOrWhiteSpace(query.UserId)
                ? null
                : _userManager.GetUserById(new Guid(query.UserId));

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
                    return new Tuple<IChannel, ChannelItemResult>(i, new ChannelItemResult { });
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

            // Avoid implicitly captured closure
            var token = cancellationToken;
            var itemTasks = items.Select(i =>
            {
                var channelProvider = i.Item1;
                var channel = GetChannel(GetInternalChannelId(channelProvider.Name).ToString("N"));
                return GetChannelItemEntity(i.Item2, channelProvider, channel, token);
            });

            var internalItems = await Task.WhenAll(itemTasks).ConfigureAwait(false);

            internalItems = ApplyFilters(internalItems, query.Filters, user).ToArray();
            await RefreshIfNeeded(internalItems, cancellationToken).ConfigureAwait(false);

            if (query.StartIndex.HasValue)
            {
                internalItems = internalItems.Skip(query.StartIndex.Value).ToArray();
            }
            if (query.Limit.HasValue)
            {
                internalItems = internalItems.Take(query.Limit.Value).ToArray();
            }

            var returnItemArray = internalItems.Select(i => _dtoService.GetBaseItemDto(i, query.Fields, user))
                .ToArray();

            return new QueryResult<BaseItemDto>
            {
                TotalRecordCount = totalCount,
                Items = returnItemArray
            };
        }

        private async Task<IEnumerable<ChannelItemInfo>> GetLatestItems(ISupportsLatestMedia indexable, IChannel channel, string userId, CancellationToken cancellationToken)
        {
            var cacheLength = TimeSpan.FromHours(12);
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

        public async Task<QueryResult<BaseItemDto>> GetAllMedia(AllChannelMediaQuery query, CancellationToken cancellationToken)
        {
            var user = string.IsNullOrWhiteSpace(query.UserId)
                ? null
                : _userManager.GetUserById(new Guid(query.UserId));

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
                    var indexable = i as IIndexableChannel;

                    if (indexable != null)
                    {
                        try
                        {
                            var result = await GetAllItems(indexable, i, userId, cancellationToken).ConfigureAwait(false);

                            return new Tuple<IChannel, ChannelItemResult>(i, result);
                        }
                        catch (Exception ex)
                        {
                            _logger.ErrorException("Error getting all media from {0}", ex, i.Name);
                        }
                    }
                    return new Tuple<IChannel, ChannelItemResult>(i, new ChannelItemResult { });
                });

            var results = await Task.WhenAll(tasks).ConfigureAwait(false);

            var totalCount = results.Length;

            IEnumerable<Tuple<IChannel, ChannelItemInfo>> items = results
                .SelectMany(i => i.Item2.Items.Select(m => new Tuple<IChannel, ChannelItemInfo>(i.Item1, m)))
                .OrderBy(i => i.Item2.Name);

            if (query.ContentTypes.Length > 0)
            {
                // Avoid implicitly captured closure
                var contentTypes = query.ContentTypes;

                items = items.Where(i => contentTypes.Contains(i.Item2.ContentType));
            }

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
                var channel = GetChannel(GetInternalChannelId(channelProvider.Name).ToString("N"));
                return GetChannelItemEntity(i.Item2, channelProvider, channel, token);
            });

            var internalItems = await Task.WhenAll(itemTasks).ConfigureAwait(false);
            await RefreshIfNeeded(internalItems, cancellationToken).ConfigureAwait(false);

            var returnItemArray = internalItems.Select(i => _dtoService.GetBaseItemDto(i, query.Fields, user))
                .ToArray();

            return new QueryResult<BaseItemDto>
            {
                TotalRecordCount = totalCount,
                Items = returnItemArray
            };
        }

        private async Task<ChannelItemResult> GetAllItems(IIndexableChannel indexable, IChannel channel, string userId, CancellationToken cancellationToken)
        {
            var cacheLength = TimeSpan.FromHours(12);
            var cachePath = GetChannelDataCachePath(channel, userId, "channelmanager-allitems", null, false);

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

                var result = await indexable.GetAllMedia(new InternalAllChannelMediaQuery
                {
                    UserId = userId

                }, cancellationToken).ConfigureAwait(false);

                CacheResponse(result, cachePath);

                return result;
            }
            finally
            {
                _resourcePool.Release();
            }
        }

        public async Task<QueryResult<BaseItemDto>> GetChannelItems(ChannelItemQuery query, CancellationToken cancellationToken)
        {
            var queryChannelId = query.ChannelId;
            // Get the internal channel entity
            var channel = _channelEntities.First(i => i.Id == new Guid(queryChannelId));

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
                    throw new ArgumentException(string.Format("{0} channel only supports a maximum of {1} records at a time.", channel.Name, channelInfo.MaxPageSize.Value));
                }
                providerLimit = query.Limit;
            }

            var user = string.IsNullOrWhiteSpace(query.UserId)
                ? null
                : _userManager.GetUserById(new Guid(query.UserId));

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

            var tasks = itemsResult.Items.Select(i => GetChannelItemEntity(i, channelProvider, channel, cancellationToken));

            var internalItems = await Task.WhenAll(tasks).ConfigureAwait(false);

            if (user != null)
            {
                internalItems = internalItems.Where(i => i.IsVisible(user)).ToArray();

                if (providerTotalRecordCount.HasValue)
                {
                    providerTotalRecordCount = providerTotalRecordCount.Value;
                }
            }

            return await GetReturnItems(internalItems, providerTotalRecordCount, user, query, cancellationToken).ConfigureAwait(false);
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
                    var categoryItem = (IChannelItem)_libraryManager.GetItemById(new Guid(folderId));

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
                Directory.CreateDirectory(Path.GetDirectoryName(path));

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

        private async Task<QueryResult<BaseItemDto>> GetReturnItems(IEnumerable<BaseItem> items, int? totalCountFromProvider, User user, ChannelItemQuery query, CancellationToken cancellationToken)
        {
            items = ApplyFilters(items, query.Filters, user);

            var sortBy = query.SortBy.Length == 0 ? new[] { ItemSortBy.SortName } : query.SortBy;
            items = _libraryManager.Sort(items, user, sortBy, query.SortOrder ?? SortOrder.Ascending);

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

            await RefreshIfNeeded(all, cancellationToken).ConfigureAwait(false);

            var returnItemArray = all.Select(i => _dtoService.GetBaseItemDto(i, query.Fields, user))
                .ToArray();

            return new QueryResult<BaseItemDto>
            {
                Items = returnItemArray,
                TotalRecordCount = totalCount
            };
        }

        private string GetIdToHash(string externalId, IChannel channelProvider)
        {
            // Increment this as needed to force new downloads
            // Incorporate Name because it's being used to convert channel entity to provider
            return externalId + (channelProvider.DataVersion ?? string.Empty) +
                (channelProvider.Name ?? string.Empty) + "16";
        }

        private async Task<BaseItem> GetChannelItemEntity(ChannelItemInfo info, IChannel channelProvider, Channel internalChannel, CancellationToken cancellationToken)
        {
            BaseItem item;
            Guid id;
            var isNew = false;

            var idToHash = GetIdToHash(info.Id, channelProvider);

            if (info.Type == ChannelItemType.Folder)
            {
                id = idToHash.GetMBId(typeof(ChannelFolderItem));

                item = _libraryManager.GetItemById(id) as ChannelFolderItem;

                if (item == null)
                {
                    isNew = true;
                    item = new ChannelFolderItem();
                }
            }
            else if (info.MediaType == ChannelMediaType.Audio)
            {
                id = idToHash.GetMBId(typeof(ChannelAudioItem));

                item = _libraryManager.GetItemById(id) as ChannelAudioItem;

                if (item == null)
                {
                    isNew = true;
                    item = new ChannelAudioItem();
                }
            }
            else
            {
                id = idToHash.GetMBId(typeof(ChannelVideoItem));

                item = _libraryManager.GetItemById(id) as ChannelVideoItem;

                if (item == null)
                {
                    isNew = true;
                    item = new ChannelVideoItem();
                }
            }

            item.Id = id;
            item.RunTimeTicks = info.RunTimeTicks;

            if (isNew)
            {
                item.Name = info.Name;
                item.Genres = info.Genres;
                item.Studios = info.Studios;
                item.CommunityRating = info.CommunityRating;
                item.OfficialRating = info.OfficialRating;
                item.Overview = info.Overview;
                item.IndexNumber = info.IndexNumber;
                item.ParentIndexNumber = info.ParentIndexNumber;
                item.People = info.People;
                item.PremiereDate = info.PremiereDate;
                item.ProductionYear = info.ProductionYear;
                item.ProviderIds = info.ProviderIds;

                item.DateCreated = info.DateCreated.HasValue ?
                    info.DateCreated.Value :
                    DateTime.UtcNow;
            }

            var channelItem = (IChannelItem)item;

            channelItem.OriginalImageUrl = info.ImageUrl;
            channelItem.ExternalId = info.Id;
            channelItem.ChannelId = internalChannel.Id.ToString("N");
            channelItem.ChannelItemType = info.Type;

            if (isNew)
            {
                channelItem.Tags = info.Tags;
            }

            var channelMediaItem = item as IChannelMediaItem;

            if (channelMediaItem != null)
            {
                channelMediaItem.ContentType = info.ContentType;
                channelMediaItem.ChannelMediaSources = info.MediaSources;

                var mediaSource = info.MediaSources.FirstOrDefault();

                item.Path = mediaSource == null ? null : mediaSource.Path;

                item.DisplayMediaType = channelMediaItem.ContentType.ToString();
            }

            if (isNew)
            {
                await _libraryManager.CreateItem(item, cancellationToken).ConfigureAwait(false);
                _libraryManager.RegisterItem(item);
            }

            return item;
        }

        private async Task RefreshIfNeeded(IEnumerable<BaseItem> programs, CancellationToken cancellationToken)
        {
            foreach (var program in programs)
            {
                await RefreshIfNeeded(program, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task RefreshIfNeeded(BaseItem program, CancellationToken cancellationToken)
        {
            if (_refreshedItems.ContainsKey(program.Id))
            {
                return;
            }

            await program.RefreshMetadata(cancellationToken).ConfigureAwait(false);

            _refreshedItems.TryAdd(program.Id, true);
        }

        internal IChannel GetChannelProvider(Channel channel)
        {
            return GetAllChannels().First(i => string.Equals(i.Name, channel.OriginalChannelName, StringComparison.OrdinalIgnoreCase));
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
                        var userdata = _userDataManager.GetUserData(user.Id, item.GetUserDataKey());

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
                        var userdata = _userDataManager.GetUserData(user.Id, item.GetUserDataKey());

                        return userdata != null && userdata.Likes.HasValue && userdata.Likes.Value;
                    });

                case ItemFilter.Dislikes:
                    return items.Where(item =>
                    {
                        var userdata = _userDataManager.GetUserData(user.Id, item.GetUserDataKey());

                        return userdata != null && userdata.Likes.HasValue && !userdata.Likes.Value;
                    });

                case ItemFilter.IsFavorite:
                    return items.Where(item =>
                    {
                        var userdata = _userDataManager.GetUserData(user.Id, item.GetUserDataKey());

                        return userdata != null && userdata.IsFavorite;
                    });

                case ItemFilter.IsResumable:
                    return items.Where(item =>
                    {
                        var userdata = _userDataManager.GetUserData(user.Id, item.GetUserDataKey());

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
            var user = string.IsNullOrEmpty(userId) ? null : _userManager.GetUserById(new Guid(userId));

            // Get everything
            var fields = Enum.GetNames(typeof(ItemFields)).Select(i => (ItemFields)Enum.Parse(typeof(ItemFields), i, true)).ToList();

            var folder = await GetInternalChannelFolder(userId, cancellationToken).ConfigureAwait(false);

            return _dtoService.GetBaseItemDto(folder, fields, user);
        }

        public async Task<Folder> GetInternalChannelFolder(string userId, CancellationToken cancellationToken)
        {
            var name = _localization.GetLocalizedString("ViewTypeChannels");
            return await _libraryManager.GetNamedView(name, "channels", "zz_" + name, cancellationToken).ConfigureAwait(false);
        }

        public void Dispose()
        {
            if (_refreshTimer != null)
            {
                _refreshTimer.Dispose();
                _refreshTimer = null;
            }
        }
    }
}
