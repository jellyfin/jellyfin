using MediaBrowser.Common.IO;
using MediaBrowser.Common.Progress;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Sync;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Sync;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Sync
{
    public class MediaSync
    {
        private readonly ISyncManager _syncManager;
        private readonly IServerApplicationHost _appHost;
        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;

        public MediaSync(ILogger logger, ISyncManager syncManager, IServerApplicationHost appHost, IFileSystem fileSystem)
        {
            _logger = logger;
            _syncManager = syncManager;
            _appHost = appHost;
            _fileSystem = fileSystem;
        }

        public async Task Sync(IServerSyncProvider provider,
            ISyncDataProvider dataProvider,
            SyncTarget target,
            IProgress<double> progress,
            CancellationToken cancellationToken)
        {
            var serverId = _appHost.SystemId;

            await SyncData(provider, dataProvider, serverId, target, cancellationToken).ConfigureAwait(false);
            progress.Report(3);

            var innerProgress = new ActionableProgress<double>();
            innerProgress.RegisterAction(pct =>
            {
                var totalProgress = pct * .97;
                totalProgress += 1;
                progress.Report(totalProgress);
            });
            await GetNewMedia(provider, dataProvider, target, serverId, innerProgress, cancellationToken);

            // Do the data sync twice so the server knows what was removed from the device
            await SyncData(provider, dataProvider, serverId, target, cancellationToken).ConfigureAwait(false);

            progress.Report(100);
        }

        private async Task SyncData(IServerSyncProvider provider,
            ISyncDataProvider dataProvider,
            string serverId,
            SyncTarget target,
            CancellationToken cancellationToken)
        {
            var localIds = await dataProvider.GetServerItemIds(target, serverId).ConfigureAwait(false);

            var result = await _syncManager.SyncData(new SyncDataRequest
            {
                TargetId = target.Id,
                LocalItemIds = localIds

            }).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            foreach (var itemIdToRemove in result.ItemIdsToRemove)
            {
                try
                {
                    await RemoveItem(provider, dataProvider, serverId, itemIdToRemove, target, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error deleting item from device. Id: {0}", ex, itemIdToRemove);
                }
            }
        }

        private async Task GetNewMedia(IServerSyncProvider provider,
            ISyncDataProvider dataProvider,
            SyncTarget target,
            string serverId,
            IProgress<double> progress,
            CancellationToken cancellationToken)
        {
            var jobItems = await _syncManager.GetReadySyncItems(target.Id).ConfigureAwait(false);

            var numComplete = 0;
            double startingPercent = 0;
            double percentPerItem = 1;
            if (jobItems.Count > 0)
            {
                percentPerItem /= jobItems.Count;
            }

            foreach (var jobItem in jobItems)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var currentPercent = startingPercent;
                var innerProgress = new ActionableProgress<double>();
                innerProgress.RegisterAction(pct =>
                {
                    var totalProgress = pct * percentPerItem;
                    totalProgress += currentPercent;
                    progress.Report(totalProgress);
                });

                await GetItem(provider, dataProvider, target, serverId, jobItem, innerProgress, cancellationToken).ConfigureAwait(false);

                numComplete++;
                startingPercent = numComplete;
                startingPercent /= jobItems.Count;
                startingPercent *= 100;
                progress.Report(startingPercent);
            }
        }

        private async Task GetItem(IServerSyncProvider provider,
            ISyncDataProvider dataProvider,
            SyncTarget target,
            string serverId,
            SyncedItem jobItem,
            IProgress<double> progress,
            CancellationToken cancellationToken)
        {
            var libraryItem = jobItem.Item;
            var internalSyncJobItem = _syncManager.GetJobItem(jobItem.SyncJobItemId);

            var fileTransferProgress = new ActionableProgress<double>();
            fileTransferProgress.RegisterAction(pct => progress.Report(pct * .92));

            var localItem = CreateLocalItem(provider, jobItem.SyncJobId, jobItem.SyncJobItemId, target, libraryItem, serverId, jobItem.OriginalFileName);

            await _syncManager.ReportSyncJobItemTransferBeginning(internalSyncJobItem.Id);

            var transferSuccess = false;
            Exception transferException = null;

            try
            {
                await SendFile(provider, internalSyncJobItem.OutputPath, localItem, target, cancellationToken).ConfigureAwait(false);

                // Create db record
                await dataProvider.AddOrUpdate(target, localItem).ConfigureAwait(false);

                progress.Report(92);

                transferSuccess = true;

                progress.Report(99);
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error transferring sync job file", ex);
                transferException = ex;
            }

            if (transferSuccess)
            {
                await _syncManager.ReportSyncJobItemTransferred(jobItem.SyncJobItemId).ConfigureAwait(false);
            }
            else
            {
                await _syncManager.ReportSyncJobItemTransferFailed(jobItem.SyncJobItemId).ConfigureAwait(false);

                throw transferException;
            }
        }

        private async Task RemoveItem(IServerSyncProvider provider,
            ISyncDataProvider dataProvider,
            string serverId,
            string itemId,
            SyncTarget target,
            CancellationToken cancellationToken)
        {
            var localItems = await dataProvider.GetCachedItems(target, serverId, itemId);

            foreach (var localItem in localItems)
            {
                var files = await GetFiles(provider, localItem, target);

                foreach (var file in files)
                {
                    await provider.DeleteFile(file.Path, target, cancellationToken).ConfigureAwait(false);
                }

                await dataProvider.Delete(target, localItem.Id).ConfigureAwait(false);
            }
        }

        private async Task SendFile(IServerSyncProvider provider, string inputPath, LocalItem item, SyncTarget target, CancellationToken cancellationToken)
        {
            using (var stream = _fileSystem.GetFileStream(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read, true))
            {
                await provider.SendFile(stream, item.LocalPath, target, new Progress<double>(), cancellationToken).ConfigureAwait(false);
            }
        }

        private static string GetLocalId(string jobItemId, string itemId)
        {
            var bytes = Encoding.UTF8.GetBytes(jobItemId + itemId);
            bytes = CreateMd5(bytes);
            return BitConverter.ToString(bytes, 0, bytes.Length).Replace("-", string.Empty);
        }

        private static byte[] CreateMd5(byte[] value)
        {
            using (var provider = MD5.Create())
            {
                return provider.ComputeHash(value);
            }
        }

        public LocalItem CreateLocalItem(IServerSyncProvider provider, string syncJobId, string syncJobItemId, SyncTarget target, BaseItemDto libraryItem, string serverId, string originalFileName)
        {
            var path = GetDirectoryPath(provider, syncJobId, libraryItem, serverId);
            path.Add(GetLocalFileName(provider, libraryItem, originalFileName));

            var localPath = provider.GetFullPath(path, target);

            foreach (var mediaSource in libraryItem.MediaSources)
            {
                mediaSource.Path = localPath;
                mediaSource.Protocol = MediaProtocol.File;
            }

            return new LocalItem
            {
                Item = libraryItem,
                ItemId = libraryItem.Id,
                ServerId = serverId,
                LocalPath = localPath,
                Id = GetLocalId(syncJobItemId, libraryItem.Id)
            };
        }

        private List<string> GetDirectoryPath(IServerSyncProvider provider, string syncJobId, BaseItemDto item, string serverId)
        {
            var parts = new List<string>
            {
                serverId,
                syncJobId
            };

            if (item.IsType("episode"))
            {
                parts.Add("TV");
                if (!string.IsNullOrWhiteSpace(item.SeriesName))
                {
                    parts.Add(item.SeriesName);
                }
            }
            else if (item.IsVideo)
            {
                parts.Add("Videos");
                parts.Add(item.Name);
            }
            else if (item.IsAudio)
            {
                parts.Add("Music");

                if (!string.IsNullOrWhiteSpace(item.AlbumArtist))
                {
                    parts.Add(item.AlbumArtist);
                }

                if (!string.IsNullOrWhiteSpace(item.Album))
                {
                    parts.Add(item.Album);
                }
            }
            else if (string.Equals(item.MediaType, MediaType.Photo, StringComparison.OrdinalIgnoreCase))
            {
                parts.Add("Photos");

                if (!string.IsNullOrWhiteSpace(item.Album))
                {
                    parts.Add(item.Album);
                }
            }

            return parts.Select(i => GetValidFilename(provider, i)).ToList();
        }

        private string GetLocalFileName(IServerSyncProvider provider, BaseItemDto item, string originalFileName)
        {
            var filename = originalFileName;

            if (string.IsNullOrWhiteSpace(filename))
            {
                filename = item.Name;
            }

            return GetValidFilename(provider, filename);
        }

        private string GetValidFilename(IServerSyncProvider provider, string filename)
        {
            // We can always add this method to the sync provider if it's really needed
            return _fileSystem.GetValidFilename(filename);
        }

        private async Task<List<ItemFileInfo>> GetFiles(IServerSyncProvider provider, LocalItem item, SyncTarget target)
        {
            var path = item.LocalPath;
            path = provider.GetParentDirectoryPath(path, target);

            var list = await provider.GetFileSystemEntries(path, target).ConfigureAwait(false);

            var itemFiles = new List<ItemFileInfo>();

            var name = Path.GetFileNameWithoutExtension(item.LocalPath);

            foreach (var file in list.Where(f => f.Name.Contains(name)))
            {
                var itemFile = new ItemFileInfo
                {
                    Path = file.Path,
                    Name = file.Name
                };

                if (IsSubtitleFile(file.Name))
                {
                    itemFile.Type = ItemFileType.Subtitles;
                }

                itemFiles.Add(itemFile);
            }

            return itemFiles;
        }

        private static readonly string[] SupportedSubtitleExtensions = { ".srt", ".vtt" };
        private bool IsSubtitleFile(string path)
        {
            var ext = Path.GetExtension(path) ?? string.Empty;

            return SupportedSubtitleExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase);
        }
    }
}
