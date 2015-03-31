using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Extensions;
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

        private readonly SemaphoreSlim _cacheFileLock = new SemaphoreSlim(1, 1);

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

        private string GetCachePath()
        {
            return Path.Combine(_appPaths.DataPath, "sync", _target.Id.GetMD5().ToString("N") + ".json");
        }

        private string GetRemotePath()
        {
            var parts = new List<string>
            {
                _appHost.FriendlyName,
                "data.json"
            };

            parts = parts.Select(i => GetValidFilename(_provider, i)).ToList();

            return _provider.GetFullPath(parts, _target);
        }

        private string GetValidFilename(IServerSyncProvider provider, string filename)
        {
            // We can always add this method to the sync provider if it's really needed
            return _fileSystem.GetValidFilename(filename);
        }

        private async Task CacheData(Stream stream)
        {
            var cachePath = GetCachePath();

            await _cacheFileLock.WaitAsync().ConfigureAwait(false);

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(cachePath));
                using (var fileStream = _fileSystem.GetFileStream(cachePath, FileMode.Create, FileAccess.Write, FileShare.Read, true))
                {
                    await stream.CopyToAsync(fileStream).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error saving sync data to {0}", ex, cachePath);
            }
            finally
            {
                _cacheFileLock.Release();
            }
        }

        private async Task EnsureData(CancellationToken cancellationToken)
        {
            if (_items == null)
            {
                try
                {
                    using (var stream = await _provider.GetFile(GetRemotePath(), _target, new Progress<double>(), cancellationToken))
                    {
                        _items = _json.DeserializeFromStream<List<LocalItem>>(stream);
                    }
                }
                catch (FileNotFoundException)
                {
                    _items = new List<LocalItem>();
                }
                catch (DirectoryNotFoundException)
                {
                    _items = new List<LocalItem>();
                }

                using (var memoryStream = new MemoryStream())
                {
                    _json.SerializeToStream(_items, memoryStream);
                    
                    // Now cache it
                    memoryStream.Position = 0;
                    await CacheData(memoryStream).ConfigureAwait(false);
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

                // Now cache it
                stream.Position = 0;
                await CacheData(stream).ConfigureAwait(false);
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

        public Task<List<string>> GetServerItemIds(SyncTarget target, string serverId)
        {
            return GetData(items => items.Where(i => string.Equals(i.ServerId, serverId, StringComparison.OrdinalIgnoreCase)).Select(i => i.ItemId).ToList());
        }

        public Task<List<string>> GetSyncJobItemIds(SyncTarget target, string serverId)
        {
            return GetData(items => items.Where(i => string.Equals(i.ServerId, serverId, StringComparison.OrdinalIgnoreCase)).Select(i => i.SyncJobItemId).Where(i => !string.IsNullOrWhiteSpace(i)).ToList());
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

        private async Task<List<LocalItem>> GetCachedData()
        {
            if (_items == null)
            {
                await _cacheFileLock.WaitAsync().ConfigureAwait(false);

                try
                {
                    if (_items == null)
                    {
                        try
                        {
                            _items = _json.DeserializeFromFile<List<LocalItem>>(GetCachePath());
                        }
                        catch (FileNotFoundException)
                        {
                            _items = new List<LocalItem>();
                        }
                        catch (DirectoryNotFoundException)
                        {
                            _items = new List<LocalItem>();
                        }
                    }
                }
                finally
                {
                    _cacheFileLock.Release();
                }
            }

            return _items.ToList();
        }

        public async Task<List<string>> GetCachedServerItemIds(SyncTarget target, string serverId)
        {
            var items = await GetCachedData().ConfigureAwait(false);

            return items.Where(i => string.Equals(i.ServerId, serverId, StringComparison.OrdinalIgnoreCase))
                    .Select(i => i.ItemId)
                    .ToList();
        }

        public async Task<List<LocalItem>> GetCachedItems(SyncTarget target, string serverId, string itemId)
        {
            var items = await GetCachedData().ConfigureAwait(false);

            return items.Where(i => string.Equals(i.ServerId, serverId, StringComparison.OrdinalIgnoreCase) && string.Equals(i.ItemId, itemId, StringComparison.OrdinalIgnoreCase))
                    .ToList();
        }

        public async Task<List<LocalItem>> GetCachedItemsBySyncJobItemId(SyncTarget target, string serverId, string syncJobItemId)
        {
            var items = await GetCachedData().ConfigureAwait(false);

            return items.Where(i => string.Equals(i.ServerId, serverId, StringComparison.OrdinalIgnoreCase) && string.Equals(i.SyncJobItemId, syncJobItemId, StringComparison.OrdinalIgnoreCase))
                    .ToList();
        }
    }
}
