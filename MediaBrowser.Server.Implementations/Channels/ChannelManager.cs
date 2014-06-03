using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Channels
{
    public class ChannelManager : IChannelManager
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

        public ChannelManager(IUserManager userManager, IDtoService dtoService, ILibraryManager libraryManager, ILogger logger, IServerConfigurationManager config, IFileSystem fileSystem, IUserDataManager userDataManager, IJsonSerializer jsonSerializer)
        {
            _userManager = userManager;
            _dtoService = dtoService;
            _libraryManager = libraryManager;
            _logger = logger;
            _config = config;
            _fileSystem = fileSystem;
            _userDataManager = userDataManager;
            _jsonSerializer = jsonSerializer;
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
                if (!string.IsNullOrWhiteSpace(_config.Configuration.ChannelOptions.DownloadPath))
                {
                    return _config.Configuration.ChannelOptions.DownloadPath;
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

            if (user != null)
            {
                channels = channels.Where(i => GetChannelProvider(i).IsEnabledFor(user) && i.IsVisible(user))
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
                results = await requiresCallback.GetChannelItemMediaInfo(item.ExternalId, cancellationToken)
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
                LocationType = info.IsRemote ? LocationType.Remote : LocationType.FileSystem,
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

            var width = _config.Configuration.ChannelOptions.PreferredStreamingWidth;

            if (width.HasValue)
            {
                var val = width.Value;

                return list
                    .OrderBy(i => i.Width.HasValue && i.Width.Value <= val)
                    .ThenBy(i => Math.Abs(i.Width ?? 0 - val))
                    .ThenByDescending(i => i.Width ?? 0)
                    .ThenBy(list.IndexOf);
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

            return GetChannelFeaturesDto(channel, channelProvider.GetChannelFeatures());
        }

        public ChannelFeatures GetChannelFeaturesDto(Channel channel, InternalChannelFeatures features)
        {
            return new ChannelFeatures
            {
                CanFilter = !features.MaxPageSize.HasValue,
                CanGetAllMedia = features.CanGetAllMedia,
                CanSearch = features.CanSearch,
                ContentTypes = features.ContentTypes,
                DefaultSortFields = features.DefaultSortFields,
                MaxPageSize = features.MaxPageSize,
                MediaTypes = features.MediaTypes,
                SupportsSortOrderToggle = features.SupportsSortOrderToggle,
                Name = channel.Name,
                Id = channel.Id.ToString("N"),
                CanDownloadAllMedia = features.CanGetAllMedia
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

        public async Task<QueryResult<BaseItemDto>> GetAllMedia(AllChannelMediaQuery query, CancellationToken cancellationToken)
        {
            var user = string.IsNullOrWhiteSpace(query.UserId)
                ? null
                : _userManager.GetUserById(new Guid(query.UserId));

            var channels = _channels;

            if (query.ChannelIds.Length > 0)
            {
                channels = channels
                    .Where(i => query.ChannelIds.Contains(GetInternalChannelId(i.Name).ToString("N")))
                    .ToArray();
            }

            var tasks = channels
                .Where(i => i.GetChannelFeatures().CanGetAllMedia)
                .Select(async i =>
                {
                    try
                    {
                        var result = await i.GetAllMedia(new InternalAllChannelMediaQuery
                        {
                            User = user

                        }, cancellationToken).ConfigureAwait(false);

                        return new Tuple<IChannel, ChannelItemResult>(i, result);
                    }
                    catch (Exception ex)
                    {
                        _logger.ErrorException("Error getting all media from {0}", ex, i.Name);
                        return new Tuple<IChannel, ChannelItemResult>(i, new ChannelItemResult { });
                    }
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
                var channel = GetChannel(GetInternalChannelId(channelProvider.Name).ToString("N"));
                return GetChannelItemEntity(i.Item2, channelProvider, channel, token);
            });

            var internalItems = await Task.WhenAll(itemTasks).ConfigureAwait(false);

            // Get everything
            var fields = Enum.GetNames(typeof(ItemFields))
                    .Select(i => (ItemFields)Enum.Parse(typeof(ItemFields), i, true))
                    .ToList();

            var returnItemArray = internalItems.Select(i => _dtoService.GetBaseItemDto(i, fields, user))
                .ToArray();

            return new QueryResult<BaseItemDto>
            {
                TotalRecordCount = totalCount,
                Items = returnItemArray
            };
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
            var cachePath = GetChannelDataCachePath(channel, user, folderId, sortField, sortDescending);

            try
            {
                if (!startIndex.HasValue && !limit.HasValue)
                {
                    var channelItemResult = _jsonSerializer.DeserializeFromFile<ChannelItemResult>(cachePath);

                    if (_fileSystem.GetLastWriteTimeUtc(cachePath).Add(channelItemResult.CacheLength) > DateTime.UtcNow)
                    {
                        return channelItemResult;
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
                        var channelItemResult = _jsonSerializer.DeserializeFromFile<ChannelItemResult>(cachePath);

                        if (_fileSystem.GetLastWriteTimeUtc(cachePath).Add(channelItemResult.CacheLength) > DateTime.UtcNow)
                        {
                            return channelItemResult;
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
                    User = user,
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

        private void CacheResponse(ChannelItemResult result, string path)
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
            User user,
            string folderId,
            ChannelItemSortField? sortField,
            bool sortDescending)
        {
            var channelId = GetInternalChannelId(channel.Name).ToString("N");

            var folderKey = string.IsNullOrWhiteSpace(folderId) ? "root" : folderId.GetMD5().ToString("N");

            var version = string.IsNullOrWhiteSpace(channel.DataVersion) ? "0" : channel.DataVersion;

            var filename = user.Id.ToString("N");
            var hashfilename = false;

            if (sortField.HasValue)
            {
                filename += "-sortField-" + sortField.Value;
                hashfilename = true;
            }
            if (sortDescending)
            {
                filename += "-sortDescending";
                hashfilename = true;
            }

            if (hashfilename)
            {
                filename = filename.GetMD5().ToString("N");
            }

            return Path.Combine(_config.ApplicationPaths.CachePath, "channels", channelId, version, folderKey, filename + ".json");
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

            // Get everything
            var fields = Enum.GetNames(typeof(ItemFields))
                    .Select(i => (ItemFields)Enum.Parse(typeof(ItemFields), i, true))
                    .ToList();

            var returnItemArray = all.Select(i => _dtoService.GetBaseItemDto(i, fields, user))
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
            return externalId + (channelProvider.DataVersion ?? string.Empty) + (channelProvider.Name ?? string.Empty) + "14";
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
                item.People = info.People;
                item.PremiereDate = info.PremiereDate;
                item.ProductionYear = info.ProductionYear;
                item.ProviderIds = info.ProviderIds;

                if (info.DateCreated.HasValue)
                {
                    item.DateCreated = info.DateCreated.Value;
                }
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
                channelMediaItem.IsInfiniteStream = info.IsInfiniteStream;
                channelMediaItem.ContentType = info.ContentType;
                channelMediaItem.ChannelMediaSources = info.MediaSources;

                var mediaSource = info.MediaSources.FirstOrDefault();

                item.Path = mediaSource == null ? null : mediaSource.Path;
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
            //if (_refreshedPrograms.ContainsKey(program.Id))
            {
                //return;
            }

            await program.RefreshMetadata(cancellationToken).ConfigureAwait(false);

            //_refreshedPrograms.TryAdd(program.Id, true);
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
    }
}
