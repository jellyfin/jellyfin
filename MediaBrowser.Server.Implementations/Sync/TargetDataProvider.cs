using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.IO;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Sync;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Sync;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommonIO;
using Interfaces.IO;

namespace MediaBrowser.Server.Implementations.Sync
{
    public class TargetDataProvider : ISyncDataProvider
    {
        private readonly SyncTarget _target;
        private readonly IServerSyncProvider _provider;

        private readonly SemaphoreSlim _dataLock = new SemaphoreSlim(1, 1);
        private List<LocalItem> _items;

        private readonly ILogger _logger;
        private readonly IJsonSerializer _json;
        private readonly IFileSystem _fileSystem;
        private readonly IApplicationPaths _appPaths;
        private readonly IServerApplicationHost _appHost;

        public TargetDataProvider(IServerSyncProvider provider, SyncTarget target, IServerApplicationHost appHost, ILogger logger, IJsonSerializer json, IFileSystem fileSystem, IApplicationPaths appPaths)
        {
            _logger = logger;
            _json = json;
            _provider = provider;
            _target = target;
            _fileSystem = fileSystem;
            _appPaths = appPaths;
            _appHost = appHost;
        }

        private string[] GetRemotePath()
        {
            var parts = new List<string>
            {
                _appHost.FriendlyName,
                "data.json"
            };

            parts = parts.Select(i => GetValidFilename(_provider, i)).ToList();

            return parts.ToArray();
        }

        private string GetValidFilename(IServerSyncProvider provider, string filename)
        {
            // We can always add this method to the sync provider if it's really needed
            return _fileSystem.GetValidFilename(filename);
        }

        private async Task EnsureData(CancellationToken cancellationToken)
        {
            if (_items == null)
            {
                _logger.Debug("Getting {0} from {1}", string.Join(MediaSync.PathSeparatorString, GetRemotePath().ToArray()), _provider.Name);

                var fileResult = await _provider.GetFiles(new FileQuery
                {
                    FullPath = GetRemotePath().ToArray()

                }, _target, cancellationToken).ConfigureAwait(false);

                if (fileResult.Items.Length > 0)
                {
                    using (var stream = await _provider.GetFile(fileResult.Items[0].Id, _target, new Progress<double>(), cancellationToken))
                    {
                        _items = _json.DeserializeFromStream<List<LocalItem>>(stream);
                    }
                }
                else
                {
                    _items = new List<LocalItem>();
                }
            }
        }

        private async Task SaveData(CancellationToken cancellationToken)
        {
            using (var stream = new MemoryStream())
            {
                _json.SerializeToStream(_items, stream);

                // Save to sync provider
                stream.Position = 0;
                await _provider.SendFile(stream, GetRemotePath(), _target, new Progress<double>(), cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task<T> GetData<T>(Func<List<LocalItem>, T> dataFactory)
        {
            await _dataLock.WaitAsync().ConfigureAwait(false);

            try
            {
                await EnsureData(CancellationToken.None).ConfigureAwait(false);

                return dataFactory(_items);
            }
            finally
            {
                _dataLock.Release();
            }
        }

        private async Task UpdateData(Func<List<LocalItem>, List<LocalItem>> action)
        {
            await _dataLock.WaitAsync().ConfigureAwait(false);

            try
            {
                await EnsureData(CancellationToken.None).ConfigureAwait(false);

                _items = action(_items);

                await SaveData(CancellationToken.None).ConfigureAwait(false);
            }
            finally
            {
                _dataLock.Release();
            }
        }

        public Task<List<LocalItem>> GetLocalItems(SyncTarget target, string serverId)
        {
            return GetData(items => items.Where(i => string.Equals(i.ServerId, serverId, StringComparison.OrdinalIgnoreCase)).ToList());
        }

        public Task AddOrUpdate(SyncTarget target, LocalItem item)
        {
            return UpdateData(items =>
            {
                var list = items.Where(i => !string.Equals(i.Id, item.Id, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                list.Add(item);

                return list;
            });
        }

        public Task Delete(SyncTarget target, string id)
        {
            return UpdateData(items => items.Where(i => !string.Equals(i.Id, id, StringComparison.OrdinalIgnoreCase)).ToList());
        }

        public Task<LocalItem> Get(SyncTarget target, string id)
        {
            return GetData(items => items.FirstOrDefault(i => string.Equals(i.Id, id, StringComparison.OrdinalIgnoreCase)));
        }

        public Task<List<LocalItem>> GetItems(SyncTarget target, string serverId, string itemId)
        {
            return GetData(items => items.Where(i => string.Equals(i.ServerId, serverId, StringComparison.OrdinalIgnoreCase) && string.Equals(i.ItemId, itemId, StringComparison.OrdinalIgnoreCase)).ToList());
        }

        public Task<List<LocalItem>> GetItemsBySyncJobItemId(SyncTarget target, string serverId, string syncJobItemId)
        {
            return GetData(items => items.Where(i => string.Equals(i.ServerId, serverId, StringComparison.OrdinalIgnoreCase) && string.Equals(i.SyncJobItemId, syncJobItemId, StringComparison.OrdinalIgnoreCase)).ToList());
        }
    }
}
