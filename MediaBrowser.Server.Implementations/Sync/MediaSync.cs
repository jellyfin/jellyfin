using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Progress;
using MediaBrowser.Controller;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Sync;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Sync;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommonIO;
using Interfaces.IO;

namespace MediaBrowser.Server.Implementations.Sync
{
    public class MediaSync
    {
        private readonly ISyncManager _syncManager;
        private readonly IServerApplicationHost _appHost;
        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;
        private readonly IConfigurationManager _config;

        public const string PathSeparatorString = "/";
        public const char PathSeparatorChar = '/';

        public MediaSync(ILogger logger, ISyncManager syncManager, IServerApplicationHost appHost, IFileSystem fileSystem, IConfigurationManager config)
        {
            _logger = logger;
            _syncManager = syncManager;
            _appHost = appHost;
            _fileSystem = fileSystem;
            _config = config;
        }

        public async Task Sync(IServerSyncProvider provider,
            ISyncDataProvider dataProvider,
            SyncTarget target,
            IProgress<double> progress,
            CancellationToken cancellationToken)
        {
            var serverId = _appHost.SystemId;
            var serverName = _appHost.FriendlyName;

            await SyncData(provider, dataProvider, serverId, target, cancellationToken).ConfigureAwait(false);
            progress.Report(3);

            var innerProgress = new ActionableProgress<double>();
            innerProgress.RegisterAction(pct =>
            {
                var totalProgress = pct * .97;
                totalProgress += 1;
                progress.Report(totalProgress);
            });
            await GetNewMedia(provider, dataProvider, target, serverId, serverName, innerProgress, cancellationToken);

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
            var localItems = await dataProvider.GetLocalItems(target, serverId).ConfigureAwait(false);
            var remoteFiles = await provider.GetFiles(new FileQuery(), target, cancellationToken).ConfigureAwait(false);
            var remoteIds = remoteFiles.Items.Select(i => i.Id).ToList();

            var jobItemIds = new List<string>();

            foreach (var localItem in localItems)
            {
                if (remoteIds.Contains(localItem.FileId, StringComparer.OrdinalIgnoreCase))
                {
                    jobItemIds.Add(localItem.SyncJobItemId);
                }
            }

            var result = await _syncManager.SyncData(new SyncDataRequest
            {
                TargetId = target.Id,
                SyncJobItemIds = jobItemIds

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
            string serverName,
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

                try
                {
                    await GetItem(provider, dataProvider, target, serverId, serverName, jobItem, innerProgress, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error syncing item", ex);
                }

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
            string serverName,
            SyncedItem jobItem,
            IProgress<double> progress,
            CancellationToken cancellationToken)
        {
            var libraryItem = jobItem.Item;
            var internalSyncJobItem = _syncManager.GetJobItem(jobItem.SyncJobItemId);
            var internalSyncJob = _syncManager.GetJob(jobItem.SyncJobId);

            var localItem = CreateLocalItem(provider, jobItem, internalSyncJob, target, libraryItem, serverId, serverName, jobItem.OriginalFileName);

            await _syncManager.ReportSyncJobItemTransferBeginning(internalSyncJobItem.Id);

            var transferSuccess = false;
            Exception transferException = null;

            var options = _config.GetSyncOptions();

            try
            {
                var fileTransferProgress = new ActionableProgress<double>();
                fileTransferProgress.RegisterAction(pct => progress.Report(pct * .92));

                var sendFileResult = await SendFile(provider, internalSyncJobItem.OutputPath, localItem.LocalPath.Split(PathSeparatorChar), target, options, fileTransferProgress, cancellationToken).ConfigureAwait(false);

                if (localItem.Item.MediaSources != null)
                {
                    var mediaSource = localItem.Item.MediaSources.FirstOrDefault();
                    if (mediaSource != null)
                    {
                        mediaSource.Path = sendFileResult.Path;
                        mediaSource.Protocol = sendFileResult.Protocol;
                        mediaSource.RequiredHttpHeaders = sendFileResult.RequiredHttpHeaders;
                        mediaSource.SupportsTranscoding = false;
                    }
                }

                localItem.FileId = sendFileResult.Id;

                // Create db record
                await dataProvider.AddOrUpdate(target, localItem).ConfigureAwait(false);

                if (localItem.Item.MediaSources != null)
                {
                    var mediaSource = localItem.Item.MediaSources.FirstOrDefault();
                    if (mediaSource != null)
                    {
                        await SendSubtitles(localItem, mediaSource, provider, dataProvider, target, options, cancellationToken).ConfigureAwait(false);
                    }
                }

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

        private async Task SendSubtitles(LocalItem localItem, MediaSourceInfo mediaSource, IServerSyncProvider provider, ISyncDataProvider dataProvider, SyncTarget target, SyncOptions options, CancellationToken cancellationToken)
        {
            var failedSubtitles = new List<MediaStream>();
            var requiresSave = false;

            foreach (var mediaStream in mediaSource.MediaStreams
                .Where(i => i.Type == MediaStreamType.Subtitle && i.IsExternal)
                .ToList())
            {
                try
                {
                    var remotePath = GetRemoteSubtitlePath(localItem, mediaStream, provider, target);
                    var sendFileResult = await SendFile(provider, mediaStream.Path, remotePath, target, options, new Progress<double>(), cancellationToken).ConfigureAwait(false);

                    // This is the path that will be used when talking to the provider
                    mediaStream.ExternalId = sendFileResult.Id;

                    // Keep track of all additional files for cleanup later.
                    localItem.AdditionalFiles.Add(sendFileResult.Id);

                    // This is the public path clients will use
                    mediaStream.Path = sendFileResult.Path;
                    requiresSave = true;
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error sending subtitle stream", ex);
                    failedSubtitles.Add(mediaStream);
                }
            }

            if (failedSubtitles.Count > 0)
            {
                mediaSource.MediaStreams = mediaSource.MediaStreams.Except(failedSubtitles).ToList();
                requiresSave = true;
            }

            if (requiresSave)
            {
                await dataProvider.AddOrUpdate(target, localItem).ConfigureAwait(false);
            }
        }

        private string[] GetRemoteSubtitlePath(LocalItem item, MediaStream stream, IServerSyncProvider provider, SyncTarget target)
        {
            var filename = GetSubtitleSaveFileName(item, stream.Language, stream.IsForced) + "." + stream.Codec.ToLower();

            var pathParts = item.LocalPath.Split(PathSeparatorChar);
            var list = pathParts.Take(pathParts.Length - 1).ToList();
            list.Add(filename);

            return list.ToArray();
        }

        private string GetSubtitleSaveFileName(LocalItem item, string language, bool isForced)
        {
            var path = item.LocalPath;

            var name = Path.GetFileNameWithoutExtension(path);

            if (!string.IsNullOrWhiteSpace(language))
            {
                name += "." + language.ToLower();
            }

            if (isForced)
            {
                name += ".foreign";
            }

            return name;
        }

        private async Task RemoveItem(IServerSyncProvider provider,
            ISyncDataProvider dataProvider,
            string serverId,
            string syncJobItemId,
            SyncTarget target,
            CancellationToken cancellationToken)
        {
            var localItems = await dataProvider.GetItemsBySyncJobItemId(target, serverId, syncJobItemId);

            foreach (var localItem in localItems)
            {
                var files = localItem.AdditionalFiles.ToList();

                foreach (var file in files)
                {
                    _logger.Debug("Removing {0} from {1}.", file, target.Name);
                    await provider.DeleteFile(file, target, cancellationToken).ConfigureAwait(false);
                }

                _logger.Debug("Removing {0} from {1}.", localItem.FileId, target.Name);
                await provider.DeleteFile(localItem.FileId, target, cancellationToken).ConfigureAwait(false);

                await dataProvider.Delete(target, localItem.Id).ConfigureAwait(false);
            }
        }

        private async Task<SyncedFileInfo> SendFile(IServerSyncProvider provider, string inputPath, string[] pathParts, SyncTarget target, SyncOptions options, IProgress<double> progress, CancellationToken cancellationToken)
        {
            _logger.Debug("Sending {0} to {1}. Remote path: {2}", inputPath, provider.Name, string.Join("/", pathParts));
            var supportsDirectCopy = provider as ISupportsDirectCopy;
            if (supportsDirectCopy != null)
            {
                return await supportsDirectCopy.SendFile(inputPath, pathParts, target, progress, cancellationToken).ConfigureAwait(false);
            }

            using (var fileStream = _fileSystem.GetFileStream(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read, true))
            {
                Stream stream = fileStream;

                if (options.UploadSpeedLimitBytes > 0 && provider is IRemoteSyncProvider)
                {
                    stream = new ThrottledStream(stream, options.UploadSpeedLimitBytes);
                }

                return await provider.SendFile(stream, pathParts, target, progress, cancellationToken).ConfigureAwait(false);
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

        public LocalItem CreateLocalItem(IServerSyncProvider provider, SyncedItem syncedItem, SyncJob job, SyncTarget target, BaseItemDto libraryItem, string serverId, string serverName, string originalFileName)
        {
            var path = GetDirectoryPath(provider, job, syncedItem, libraryItem, serverName);
            path.Add(GetLocalFileName(provider, libraryItem, originalFileName));

            var localPath = string.Join(PathSeparatorString, path.ToArray());

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
                Id = GetLocalId(syncedItem.SyncJobItemId, libraryItem.Id),
                SyncJobItemId = syncedItem.SyncJobItemId
            };
        }

        private List<string> GetDirectoryPath(IServerSyncProvider provider, SyncJob job, SyncedItem syncedItem, BaseItemDto item, string serverName)
        {
            var parts = new List<string>
            {
                serverName
            };

            var profileOption = _syncManager.GetProfileOptions(job.TargetId)
                .FirstOrDefault(i => string.Equals(i.Id, job.Profile, StringComparison.OrdinalIgnoreCase));

            string name;

            if (profileOption != null && !string.IsNullOrWhiteSpace(profileOption.Name))
            {
                name = profileOption.Name;

                if (job.Bitrate.HasValue)
                {
                    name += "-" + job.Bitrate.Value.ToString(CultureInfo.InvariantCulture);
                }
                else
                {
                    var qualityOption = _syncManager.GetQualityOptions(job.TargetId)
                        .FirstOrDefault(i => string.Equals(i.Id, job.Quality, StringComparison.OrdinalIgnoreCase));

                    if (qualityOption != null && !string.IsNullOrWhiteSpace(qualityOption.Name))
                    {
                        name += "-" + qualityOption.Name;
                    }
                }
            }
            else
            {
                name = syncedItem.SyncJobName + "-" + syncedItem.SyncJobDateCreated
                   .ToLocalTime()
                   .ToString("g")
                   .Replace(" ", "-");
            }

            name = GetValidFilename(provider, name);
            parts.Add(name);

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
    }
}
