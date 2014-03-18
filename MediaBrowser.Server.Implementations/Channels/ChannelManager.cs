using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Querying;
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
        private List<Channel> _channelEntities = new List<Channel>();

        private readonly IUserManager _userManager;
        private readonly IDtoService _dtoService;
        private readonly ILibraryManager _libraryManager;
        private readonly ILogger _logger;
        private readonly IServerConfigurationManager _config;
        private readonly IFileSystem _fileSystem;

        public ChannelManager(IUserManager userManager, IDtoService dtoService, ILibraryManager libraryManager, ILogger logger, IServerConfigurationManager config, IFileSystem fileSystem)
        {
            _userManager = userManager;
            _dtoService = dtoService;
            _libraryManager = libraryManager;
            _logger = logger;
            _config = config;
            _fileSystem = fileSystem;
        }

        public void AddParts(IEnumerable<IChannel> channels)
        {
            _channels = channels.ToArray();
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

            // Get everything
            var fields = Enum.GetNames(typeof(ItemFields))
                    .Select(i => (ItemFields)Enum.Parse(typeof(ItemFields), i, true))
                    .ToList();

            var returnItems = channels.Select(i => _dtoService.GetBaseItemDto(i, fields, user))
                .ToArray();

            var result = new QueryResult<BaseItemDto>
            {
                Items = returnItems,
                TotalRecordCount = returnItems.Length
            };

            return Task.FromResult(result);
        }

        public async Task RefreshChannels(IProgress<double> progress, CancellationToken cancellationToken)
        {
            var allChannelsList = _channels.ToList();

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

        private Guid GetInternalChannelId(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException("name");
            }

            return ("Channel " + name).GetMBId(typeof(Channel));
        }

        public async Task<QueryResult<BaseItemDto>> GetChannelItems(ChannelItemQuery query, CancellationToken cancellationToken)
        {
            var user = string.IsNullOrWhiteSpace(query.UserId)
                ? null
                : _userManager.GetUserById(new Guid(query.UserId));

            var id = new Guid(query.ChannelId);
            var channel = _channelEntities.First(i => i.Id == id);
            var channelProvider = GetChannelProvider(channel);

            var items = await GetChannelItems(channelProvider, user, query.CategoryId, cancellationToken)
                        .ConfigureAwait(false);


            return await GetReturnItems(items, user, query.StartIndex, query.Limit, cancellationToken).ConfigureAwait(false);
        }

        private Task<IEnumerable<ChannelItemInfo>> GetChannelItems(IChannel channel, User user, string categoryId, CancellationToken cancellationToken)
        {
            // TODO: Put some caching in here

            if (string.IsNullOrWhiteSpace(categoryId))
            {
                return channel.GetChannelItems(user, cancellationToken);
            }

            return channel.GetChannelItems(categoryId, user, cancellationToken);
        }

        private async Task<QueryResult<BaseItemDto>> GetReturnItems(IEnumerable<ChannelItemInfo> items, User user, int? startIndex, int? limit, CancellationToken cancellationToken)
        {
            if (startIndex.HasValue)
            {
                items = items.Skip(startIndex.Value);
            }
            if (limit.HasValue)
            {
                items = items.Take(limit.Value);
            }

            // Get everything
            var fields = Enum.GetNames(typeof(ItemFields))
                    .Select(i => (ItemFields)Enum.Parse(typeof(ItemFields), i, true))
                    .ToList();

            var tasks = items.Select(GetChannelItemEntity);

            var returnItems = await Task.WhenAll(tasks).ConfigureAwait(false);

            var returnItemArray = returnItems.Select(i => _dtoService.GetBaseItemDto(i, fields, user))
                .ToArray();

            return new QueryResult<BaseItemDto>
            {
                Items = returnItemArray,
                TotalRecordCount = returnItems.Length
            };
        }

        private async Task<BaseItem> GetChannelItemEntity(ChannelItemInfo info)
        {
            return null;
        }

        private IChannel GetChannelProvider(Channel channel)
        {
            return _channels.First(i => string.Equals(i.Name, channel.OriginalChannelName, StringComparison.OrdinalIgnoreCase));
        }
    }
}
