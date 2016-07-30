using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.IO;
using MediaBrowser.Common.Progress;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Sync;
using MediaBrowser.Controller.TV;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Session;
using MediaBrowser.Model.Sync;
using MoreLinq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommonIO;

namespace MediaBrowser.Server.Implementations.Sync
{
    public class SyncJobProcessor
    {
        private readonly ILibraryManager _libraryManager;
        private readonly ISyncRepository _syncRepo;
        private readonly SyncManager _syncManager;
        private readonly ILogger _logger;
        private readonly IUserManager _userManager;
        private readonly ITVSeriesManager _tvSeriesManager;
        private readonly IMediaEncoder _mediaEncoder;
        private readonly ISubtitleEncoder _subtitleEncoder;
        private readonly IConfigurationManager _config;
        private readonly IFileSystem _fileSystem;
        private readonly IMediaSourceManager _mediaSourceManager;

        public SyncJobProcessor(ILibraryManager libraryManager, ISyncRepository syncRepo, SyncManager syncManager, ILogger logger, IUserManager userManager, ITVSeriesManager tvSeriesManager, IMediaEncoder mediaEncoder, ISubtitleEncoder subtitleEncoder, IConfigurationManager config, IFileSystem fileSystem, IMediaSourceManager mediaSourceManager)
        {
            _libraryManager = libraryManager;
            _syncRepo = syncRepo;
            _syncManager = syncManager;
            _logger = logger;
            _userManager = userManager;
            _tvSeriesManager = tvSeriesManager;
            _mediaEncoder = mediaEncoder;
            _subtitleEncoder = subtitleEncoder;
            _config = config;
            _fileSystem = fileSystem;
            _mediaSourceManager = mediaSourceManager;
        }

        public async Task EnsureJobItems(SyncJob job)
        {
            var user = _userManager.GetUserById(job.UserId);

            if (user == null)
            {
                throw new InvalidOperationException("Cannot proceed with sync because user no longer exists.");
            }

            var items = (await GetItemsForSync(job.Category, job.ParentId, job.RequestedItemIds, user, job.UnwatchedOnly).ConfigureAwait(false))
                .ToList();

            var jobItems = _syncManager.GetJobItems(new SyncJobItemQuery
            {
                JobId = job.Id,
                AddMetadata = false

            }).Items.ToList();

            foreach (var item in items)
            {
                // Respect ItemLimit, if set
                if (job.ItemLimit.HasValue)
                {
                    if (jobItems.Count(j => j.Status != SyncJobItemStatus.RemovedFromDevice && j.Status != SyncJobItemStatus.Failed) >= job.ItemLimit.Value)
                    {
                        break;
                    }
                }

                var itemId = item.Id.ToString("N");

                var jobItem = jobItems.FirstOrDefault(i => string.Equals(i.ItemId, itemId, StringComparison.OrdinalIgnoreCase));

                if (jobItem != null)
                {
                    continue;
                }

                var index = jobItems.Count == 0 ?
                    0 :
                    jobItems.Select(i => i.JobItemIndex).Max() + 1;

                jobItem = new SyncJobItem
                {
                    Id = Guid.NewGuid().ToString("N"),
                    ItemId = itemId,
                    ItemName = GetSyncJobItemName(item),
                    JobId = job.Id,
                    TargetId = job.TargetId,
                    DateCreated = DateTime.UtcNow,
                    JobItemIndex = index
                };

                await _syncRepo.Create(jobItem).ConfigureAwait(false);
                _syncManager.OnSyncJobItemCreated(jobItem);

                jobItems.Add(jobItem);
            }

            jobItems = jobItems
                .OrderBy(i => i.DateCreated)
                .ToList();

            await UpdateJobStatus(job, jobItems).ConfigureAwait(false);
        }

        private string GetSyncJobItemName(BaseItem item)
        {
            var name = item.Name;
            var episode = item as Episode;

            if (episode != null)
            {
                if (episode.IndexNumber.HasValue)
                {
                    name = "E" + episode.IndexNumber.Value.ToString(CultureInfo.InvariantCulture) + " - " + name;
                }

                if (episode.ParentIndexNumber.HasValue)
                {
                    name = "S" + episode.ParentIndexNumber.Value.ToString(CultureInfo.InvariantCulture) + ", " + name;
                }
            }

            return name;
        }

        public Task UpdateJobStatus(string id)
        {
            var job = _syncRepo.GetJob(id);

            if (job == null)
            {
                return Task.FromResult(true);
            }

            var result = _syncManager.GetJobItems(new SyncJobItemQuery
            {
                JobId = job.Id,
                AddMetadata = false
            });

            return UpdateJobStatus(job, result.Items.ToList());
        }

        private async Task UpdateJobStatus(SyncJob job, List<SyncJobItem> jobItems)
        {
            job.ItemCount = jobItems.Count;

            double pct = 0;

            foreach (var item in jobItems)
            {
                if (item.Status == SyncJobItemStatus.Failed || item.Status == SyncJobItemStatus.Synced || item.Status == SyncJobItemStatus.RemovedFromDevice || item.Status == SyncJobItemStatus.Cancelled)
                {
                    pct += 100;
                }
                else
                {
                    pct += item.Progress ?? 0;
                }
            }

            if (job.ItemCount > 0)
            {
                pct /= job.ItemCount;
                job.Progress = pct;
            }
            else
            {
                job.Progress = null;
            }

            if (jobItems.Any(i => i.Status == SyncJobItemStatus.Transferring))
            {
                job.Status = SyncJobStatus.Transferring;
            }
            else if (jobItems.Any(i => i.Status == SyncJobItemStatus.Converting))
            {
                job.Status = SyncJobStatus.Converting;
            }
            else if (jobItems.All(i => i.Status == SyncJobItemStatus.Failed))
            {
                job.Status = SyncJobStatus.Failed;
            }
            else if (jobItems.All(i => i.Status == SyncJobItemStatus.Cancelled))
            {
                job.Status = SyncJobStatus.Cancelled;
            }
            else if (jobItems.All(i => i.Status == SyncJobItemStatus.ReadyToTransfer))
            {
                job.Status = SyncJobStatus.ReadyToTransfer;
            }
            else if (jobItems.All(i => i.Status == SyncJobItemStatus.Cancelled || i.Status == SyncJobItemStatus.Failed || i.Status == SyncJobItemStatus.Synced || i.Status == SyncJobItemStatus.RemovedFromDevice))
            {
                if (jobItems.Any(i => i.Status == SyncJobItemStatus.Failed))
                {
                    job.Status = SyncJobStatus.CompletedWithError;
                }
                else
                {
                    job.Status = SyncJobStatus.Completed;
                }
            }
            else
            {
                job.Status = SyncJobStatus.Queued;
            }

            await _syncRepo.Update(job).ConfigureAwait(false);

            _syncManager.OnSyncJobUpdated(job);
        }

        public async Task<IEnumerable<BaseItem>> GetItemsForSync(SyncCategory? category, string parentId, IEnumerable<string> itemIds, User user, bool unwatchedOnly)
        {
            var list = new List<BaseItem>();

            if (category.HasValue)
            {
                list = (await GetItemsForSync(category.Value, parentId, user).ConfigureAwait(false)).ToList();
            }
            else
            {
                foreach (var itemId in itemIds)
                {
                    var subList = await GetItemsForSync(itemId, user).ConfigureAwait(false);
                    list.AddRange(subList);
                }
            }

            IEnumerable<BaseItem> items = list;
            items = items.Where(_syncManager.SupportsSync);

            if (unwatchedOnly)
            {
                // Avoid implicitly captured closure
                var currentUser = user;

                items = items.Where(i =>
                {
                    var video = i as Video;

                    if (video != null)
                    {
                        return !video.IsPlayed(currentUser);
                    }

                    return true;
                });
            }

            return items.DistinctBy(i => i.Id);
        }

        private async Task<IEnumerable<BaseItem>> GetItemsForSync(SyncCategory category, string parentId, User user)
        {
            var parent = string.IsNullOrWhiteSpace(parentId)
                ? user.RootFolder
                : (Folder)_libraryManager.GetItemById(parentId);

            InternalItemsQuery query;

            switch (category)
            {
                case SyncCategory.Latest:
                    query = new InternalItemsQuery
                    {
                        IsFolder = false,
                        SortBy = new[] { ItemSortBy.DateCreated, ItemSortBy.SortName },
                        SortOrder = SortOrder.Descending,
                        Recursive = true
                    };
                    break;
                case SyncCategory.Resume:
                    query = new InternalItemsQuery
                    {
                        IsFolder = false,
                        SortBy = new[] { ItemSortBy.DatePlayed, ItemSortBy.SortName },
                        SortOrder = SortOrder.Descending,
                        Recursive = true,
                        IsResumable = true,
                        MediaTypes = new[] { MediaType.Video }
                    };
                    break;

                case SyncCategory.NextUp:
                    return _tvSeriesManager.GetNextUp(new NextUpQuery
                    {
                        ParentId = parentId,
                        UserId = user.Id.ToString("N")
                    }).Items;

                default:
                    throw new ArgumentException("Unrecognized category: " + category);
            }

            if (parent == null)
            {
                return new List<BaseItem>();
            }

            query.User = user;

            var result = await parent.GetItems(query).ConfigureAwait(false);
            return result.Items;
        }

        private async Task<List<BaseItem>> GetItemsForSync(string id, User user)
        {
            var item = _libraryManager.GetItemById(id);

            if (item == null)
            {
                return new List<BaseItem>();
            }

            var itemByName = item as IItemByName;
            if (itemByName != null)
            {
                return itemByName.GetTaggedItems(new InternalItemsQuery(user)
                {
                    IsFolder = false,
                    Recursive = true
                }).ToList();
            }

            if (item.IsFolder)
            {
                var folder = (Folder)item;
                var itemsResult = await folder.GetItems(new InternalItemsQuery(user)
                {
                    Recursive = true,
                    IsFolder = false

                }).ConfigureAwait(false);

                var items = itemsResult.Items;

                if (!folder.IsPreSorted)
                {
                    items = _libraryManager.Sort(items, user, new[] { ItemSortBy.SortName }, SortOrder.Ascending)
                        .ToArray();
                }

                return items.ToList();
            }

            return new List<BaseItem> { item };
        }

        private async Task EnsureSyncJobItems(string targetId, CancellationToken cancellationToken)
        {
            var jobResult = _syncRepo.GetJobs(new SyncJobQuery
            {
                SyncNewContent = true,
                TargetId = targetId
            });

            foreach (var job in jobResult.Items)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (job.SyncNewContent)
                {
                    await EnsureJobItems(job).ConfigureAwait(false);
                }
            }
        }

        public async Task Sync(IProgress<double> progress, CancellationToken cancellationToken)
        {
            await EnsureSyncJobItems(null, cancellationToken).ConfigureAwait(false);

            // Look job items that are supposedly transfering, but need to be requeued because the synced files have been deleted somehow
            await HandleDeletedSyncFiles(cancellationToken).ConfigureAwait(false);

            // If it already has a converting status then is must have been aborted during conversion
            var result = _syncManager.GetJobItems(new SyncJobItemQuery
            {
                Statuses = new[] { SyncJobItemStatus.Queued, SyncJobItemStatus.Converting },
                AddMetadata = false
            });

            await SyncJobItems(result.Items, true, progress, cancellationToken).ConfigureAwait(false);

            CleanDeadSyncFiles();
        }

        private async Task HandleDeletedSyncFiles(CancellationToken cancellationToken)
        {
            // Look job items that are supposedly transfering, but need to be requeued because the synced files have been deleted somehow
            var result = _syncManager.GetJobItems(new SyncJobItemQuery
            {
                Statuses = new[] { SyncJobItemStatus.ReadyToTransfer, SyncJobItemStatus.Transferring },
                AddMetadata = false
            });

            foreach (var item in result.Items)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (string.IsNullOrWhiteSpace(item.OutputPath) || !_fileSystem.FileExists(item.OutputPath))
                {
                    item.Status = SyncJobItemStatus.Queued;
                    await _syncManager.UpdateSyncJobItemInternal(item).ConfigureAwait(false);
                    await UpdateJobStatus(item.JobId).ConfigureAwait(false);
                }
            }
        }

        private void CleanDeadSyncFiles()
        {
            // TODO
            // Clean files in sync temp folder that are not linked to any sync jobs
        }

        public async Task SyncJobItems(string targetId, bool enableConversion, IProgress<double> progress,
            CancellationToken cancellationToken)
        {
            await EnsureSyncJobItems(targetId, cancellationToken).ConfigureAwait(false);

            // If it already has a converting status then is must have been aborted during conversion
            var result = _syncManager.GetJobItems(new SyncJobItemQuery
            {
                Statuses = new[] { SyncJobItemStatus.Queued, SyncJobItemStatus.Converting },
                TargetId = targetId,
                AddMetadata = false
            });

            await SyncJobItems(result.Items, enableConversion, progress, cancellationToken).ConfigureAwait(false);
        }

        public async Task SyncJobItems(SyncJobItem[] items, bool enableConversion, IProgress<double> progress, CancellationToken cancellationToken)
        {
            if (items.Length > 0)
            {
                if (!SyncRegistrationInfo.Instance.IsRegistered)
                {
                    _logger.Debug("Cancelling sync job processing. Please obtain a supporter membership.");
                    return;
                }
            }

            var numComplete = 0;

            foreach (var item in items)
            {
                cancellationToken.ThrowIfCancellationRequested();

                double percentPerItem = 1;
                percentPerItem /= items.Length;
                var startingPercent = numComplete * percentPerItem * 100;

                var innerProgress = new ActionableProgress<double>();
                innerProgress.RegisterAction(p => progress.Report(startingPercent + percentPerItem * p));

                // Pull it fresh from the db just to make sure it wasn't deleted or cancelled while another item was converting
                var jobItem = enableConversion ? _syncRepo.GetJobItem(item.Id) : item;

                if (jobItem != null)
                {
                    if (jobItem.Status != SyncJobItemStatus.Cancelled)
                    {
                        await ProcessJobItem(jobItem, enableConversion, innerProgress, cancellationToken).ConfigureAwait(false);
                    }

                    await UpdateJobStatus(jobItem.JobId).ConfigureAwait(false);
                }

                numComplete++;
                double percent = numComplete;
                percent /= items.Length;
                progress.Report(100 * percent);
            }
        }

        private async Task ProcessJobItem(SyncJobItem jobItem, bool enableConversion, IProgress<double> progress, CancellationToken cancellationToken)
        {
            if (jobItem == null)
            {
                throw new ArgumentNullException("jobItem");
            }

            var item = _libraryManager.GetItemById(jobItem.ItemId);
            if (item == null)
            {
                jobItem.Status = SyncJobItemStatus.Failed;
                _logger.Error("Unable to locate library item for JobItem {0}, ItemId {1}", jobItem.Id, jobItem.ItemId);
                await _syncManager.UpdateSyncJobItemInternal(jobItem).ConfigureAwait(false);
                return;
            }

            jobItem.Progress = 0;

            var syncOptions = _config.GetSyncOptions();
            var job = _syncManager.GetJob(jobItem.JobId);
            var user = _userManager.GetUserById(job.UserId);
            if (user == null)
            {
                jobItem.Status = SyncJobItemStatus.Failed;
                _logger.Error("User not found. Cannot complete the sync job.");
                await _syncManager.UpdateSyncJobItemInternal(jobItem).ConfigureAwait(false);
                return;
            }

            // See if there's already another active job item for the same target
            var existingJobItems = _syncManager.GetJobItems(new SyncJobItemQuery
            {
                AddMetadata = false,
                ItemId = jobItem.ItemId,
                TargetId = jobItem.TargetId,
                Statuses = new[] { SyncJobItemStatus.Converting, SyncJobItemStatus.Queued, SyncJobItemStatus.ReadyToTransfer, SyncJobItemStatus.Synced, SyncJobItemStatus.Transferring }
            });

            var duplicateJobItems = existingJobItems.Items
                .Where(i => !string.Equals(i.Id, jobItem.Id, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (duplicateJobItems.Count > 0)
            {
                var syncProvider = _syncManager.GetSyncProvider(jobItem) as IHasDuplicateCheck;

                if (!duplicateJobItems.Any(i => AllowDuplicateJobItem(syncProvider, i, jobItem)))
                {
                    _logger.Debug("Cancelling sync job item because there is already another active job for the same target.");
                    jobItem.Status = SyncJobItemStatus.Cancelled;
                    await _syncManager.UpdateSyncJobItemInternal(jobItem).ConfigureAwait(false);
                    return;
                }
            }

            var video = item as Video;
            if (video != null)
            {
                await Sync(jobItem, video, user, enableConversion, syncOptions, progress, cancellationToken).ConfigureAwait(false);
            }

            else if (item is Audio)
            {
                await Sync(jobItem, (Audio)item, user, enableConversion, syncOptions, progress, cancellationToken).ConfigureAwait(false);
            }

            else if (item is Photo)
            {
                await Sync(jobItem, (Photo)item, cancellationToken).ConfigureAwait(false);
            }

            else
            {
                await SyncGeneric(jobItem, item, cancellationToken).ConfigureAwait(false);
            }
        }

        private bool AllowDuplicateJobItem(IHasDuplicateCheck provider, SyncJobItem original, SyncJobItem duplicate)
        {
            if (provider != null)
            {
                return provider.AllowDuplicateJobItem(original, duplicate);
            }

            return true;
        }

        private async Task Sync(SyncJobItem jobItem, Video item, User user, bool enableConversion, SyncOptions syncOptions, IProgress<double> progress, CancellationToken cancellationToken)
        {
            var job = _syncManager.GetJob(jobItem.JobId);
            var jobOptions = _syncManager.GetVideoOptions(jobItem, job);
            var conversionOptions = new VideoOptions
            {
                Profile = jobOptions.DeviceProfile
            };

            conversionOptions.DeviceId = jobItem.TargetId;
            conversionOptions.Context = EncodingContext.Static;
            conversionOptions.ItemId = item.Id.ToString("N");
            conversionOptions.MediaSources = _mediaSourceManager.GetStaticMediaSources(item, false, user).ToList();

            var streamInfo = new StreamBuilder(_mediaEncoder, _logger).BuildVideoItem(conversionOptions);
            var mediaSource = streamInfo.MediaSource;

            // No sense creating external subs if we're already burning one into the video
            var externalSubs = streamInfo.SubtitleDeliveryMethod == SubtitleDeliveryMethod.Encode ?
                new List<SubtitleStreamInfo>() :
                streamInfo.GetExternalSubtitles(false, true, null, null);

            // Mark as requiring conversion if transcoding the video, or if any subtitles need to be extracted
            var requiresVideoTranscoding = streamInfo.PlayMethod == PlayMethod.Transcode && jobOptions.IsConverting;
            var requiresConversion = requiresVideoTranscoding || externalSubs.Any(i => RequiresExtraction(i, mediaSource));

            if (requiresConversion && !enableConversion)
            {
                return;
            }

            jobItem.MediaSourceId = streamInfo.MediaSourceId;
            jobItem.TemporaryPath = GetTemporaryPath(jobItem);

            if (requiresConversion)
            {
                jobItem.Status = SyncJobItemStatus.Converting;
            }

            if (requiresVideoTranscoding)
            {
                // Save the job item now since conversion could take a while
                await _syncManager.UpdateSyncJobItemInternal(jobItem).ConfigureAwait(false);
                await UpdateJobStatus(jobItem.JobId).ConfigureAwait(false);

                try
                {
                    var lastJobUpdate = DateTime.MinValue;
                    var innerProgress = new ActionableProgress<double>();
                    innerProgress.RegisterAction(async pct =>
                    {
                        progress.Report(pct);

                        if ((DateTime.UtcNow - lastJobUpdate).TotalSeconds >= DatabaseProgressUpdateIntervalSeconds)
                        {
                            jobItem.Progress = pct / 2;
                            await _syncManager.UpdateSyncJobItemInternal(jobItem).ConfigureAwait(false);
                            await UpdateJobStatus(jobItem.JobId).ConfigureAwait(false);
                        }
                    });

                    jobItem.OutputPath = await _mediaEncoder.EncodeVideo(new EncodingJobOptions(streamInfo, conversionOptions.Profile)
                    {
                        OutputDirectory = jobItem.TemporaryPath,
                        CpuCoreLimit = syncOptions.TranscodingCpuCoreLimit,
                        ReadInputAtNativeFramerate = !syncOptions.EnableFullSpeedTranscoding

                    }, innerProgress, cancellationToken);

                    jobItem.ItemDateModifiedTicks = item.DateModified.Ticks;
                    _syncManager.OnConversionComplete(jobItem);
                }
                catch (OperationCanceledException)
                {
                    jobItem.Status = SyncJobItemStatus.Queued;
                    jobItem.Progress = 0;
                }
                catch (Exception ex)
                {
                    jobItem.Status = SyncJobItemStatus.Failed;
                    _logger.ErrorException("Error during sync transcoding", ex);
                }

                if (jobItem.Status == SyncJobItemStatus.Failed || jobItem.Status == SyncJobItemStatus.Queued)
                {
                    await _syncManager.UpdateSyncJobItemInternal(jobItem).ConfigureAwait(false);
                    return;
                }

                jobItem.MediaSource = await GetEncodedMediaSource(jobItem.OutputPath, user, true).ConfigureAwait(false);
            }
            else
            {
                if (mediaSource.Protocol == MediaProtocol.File)
                {
                    jobItem.OutputPath = mediaSource.Path;
                }
                else if (mediaSource.Protocol == MediaProtocol.Http)
                {
                    jobItem.OutputPath = await DownloadFile(jobItem, mediaSource, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    throw new InvalidOperationException(string.Format("Cannot direct stream {0} protocol", mediaSource.Protocol));
                }

                jobItem.ItemDateModifiedTicks = item.DateModified.Ticks;
                jobItem.MediaSource = mediaSource;
            }

            jobItem.MediaSource.SupportsTranscoding = false;

            if (externalSubs.Count > 0)
            {
                // Save the job item now since conversion could take a while
                await _syncManager.UpdateSyncJobItemInternal(jobItem).ConfigureAwait(false);

                await ConvertSubtitles(jobItem, externalSubs, streamInfo, cancellationToken).ConfigureAwait(false);
            }

            jobItem.Progress = 50;
            jobItem.Status = SyncJobItemStatus.ReadyToTransfer;
            await _syncManager.UpdateSyncJobItemInternal(jobItem).ConfigureAwait(false);
        }

        private bool RequiresExtraction(SubtitleStreamInfo stream, MediaSourceInfo mediaSource)
        {
            var originalStream = mediaSource.MediaStreams.FirstOrDefault(i => i.Type == MediaStreamType.Subtitle && i.Index == stream.Index);

            return originalStream != null && !originalStream.IsExternal;
        }

        private async Task ConvertSubtitles(SyncJobItem jobItem,
            IEnumerable<SubtitleStreamInfo> subtitles,
            StreamInfo streamInfo,
            CancellationToken cancellationToken)
        {
            var files = new List<ItemFileInfo>();

            var mediaStreams = jobItem.MediaSource.MediaStreams
                .Where(i => i.Type != MediaStreamType.Subtitle || !i.IsExternal)
                .ToList();

            var startingIndex = mediaStreams.Count == 0 ?
                0 :
                mediaStreams.Select(i => i.Index).Max() + 1;

            foreach (var subtitle in subtitles)
            {
                var fileInfo = await ConvertSubtitles(jobItem.TemporaryPath, streamInfo, subtitle, cancellationToken).ConfigureAwait(false);

                // Reset this to a value that will be based on the output media
                fileInfo.Index = startingIndex;
                files.Add(fileInfo);

                mediaStreams.Add(new MediaStream
                {
                    Index = startingIndex,
                    Codec = subtitle.Format,
                    IsForced = subtitle.IsForced,
                    IsExternal = true,
                    Language = subtitle.Language,
                    Path = fileInfo.Path,
                    SupportsExternalStream = true,
                    Type = MediaStreamType.Subtitle
                });

                startingIndex++;
            }

            jobItem.AdditionalFiles.AddRange(files);

            jobItem.MediaSource.MediaStreams = mediaStreams;
        }

        private async Task<ItemFileInfo> ConvertSubtitles(string temporaryPath, StreamInfo streamInfo, SubtitleStreamInfo subtitleStreamInfo, CancellationToken cancellationToken)
        {
            var subtitleStreamIndex = subtitleStreamInfo.Index;

            var filename = Guid.NewGuid() + "." + subtitleStreamInfo.Format.ToLower();

            var path = Path.Combine(temporaryPath, filename);

            _fileSystem.CreateDirectory(Path.GetDirectoryName(path));

            using (var stream = await _subtitleEncoder.GetSubtitles(streamInfo.ItemId, streamInfo.MediaSourceId, subtitleStreamIndex, subtitleStreamInfo.Format, 0, null, false, cancellationToken).ConfigureAwait(false))
            {
                using (var fs = _fileSystem.GetFileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read, true))
                {
                    await stream.CopyToAsync(fs, StreamDefaults.DefaultCopyToBufferSize, cancellationToken).ConfigureAwait(false);
                }
            }

            return new ItemFileInfo
            {
                Name = Path.GetFileName(path),
                Path = path,
                Type = ItemFileType.Subtitles,
                Index = subtitleStreamIndex
            };
        }

        private const int DatabaseProgressUpdateIntervalSeconds = 2;

        private async Task Sync(SyncJobItem jobItem, Audio item, User user, bool enableConversion, SyncOptions syncOptions, IProgress<double> progress, CancellationToken cancellationToken)
        {
            var job = _syncManager.GetJob(jobItem.JobId);
            var jobOptions = _syncManager.GetAudioOptions(jobItem, job);
            var conversionOptions = new AudioOptions
            {
                Profile = jobOptions.DeviceProfile
            };

            conversionOptions.DeviceId = jobItem.TargetId;
            conversionOptions.Context = EncodingContext.Static;
            conversionOptions.ItemId = item.Id.ToString("N");
            conversionOptions.MediaSources = _mediaSourceManager.GetStaticMediaSources(item, false, user).ToList();

            var streamInfo = new StreamBuilder(_mediaEncoder, _logger).BuildAudioItem(conversionOptions);
            var mediaSource = streamInfo.MediaSource;

            jobItem.MediaSourceId = streamInfo.MediaSourceId;
            jobItem.TemporaryPath = GetTemporaryPath(jobItem);

            if (streamInfo.PlayMethod == PlayMethod.Transcode && jobOptions.IsConverting)
            {
                if (!enableConversion)
                {
                    return;
                }

                jobItem.Status = SyncJobItemStatus.Converting;
                await _syncManager.UpdateSyncJobItemInternal(jobItem).ConfigureAwait(false);
                await UpdateJobStatus(jobItem.JobId).ConfigureAwait(false);

                try
                {
                    var lastJobUpdate = DateTime.MinValue;
                    var innerProgress = new ActionableProgress<double>();
                    innerProgress.RegisterAction(async pct =>
                    {
                        progress.Report(pct);

                        if ((DateTime.UtcNow - lastJobUpdate).TotalSeconds >= DatabaseProgressUpdateIntervalSeconds)
                        {
                            jobItem.Progress = pct / 2;
                            await _syncManager.UpdateSyncJobItemInternal(jobItem).ConfigureAwait(false);
                            await UpdateJobStatus(jobItem.JobId).ConfigureAwait(false);
                        }
                    });

                    jobItem.OutputPath = await _mediaEncoder.EncodeAudio(new EncodingJobOptions(streamInfo, conversionOptions.Profile)
                    {
                        OutputDirectory = jobItem.TemporaryPath,
                        CpuCoreLimit = syncOptions.TranscodingCpuCoreLimit

                    }, innerProgress, cancellationToken);

                    jobItem.ItemDateModifiedTicks = item.DateModified.Ticks;
                    _syncManager.OnConversionComplete(jobItem);
                }
                catch (OperationCanceledException)
                {
                    jobItem.Status = SyncJobItemStatus.Queued;
                    jobItem.Progress = 0;
                }
                catch (Exception ex)
                {
                    jobItem.Status = SyncJobItemStatus.Failed;
                    _logger.ErrorException("Error during sync transcoding", ex);
                }

                if (jobItem.Status == SyncJobItemStatus.Failed || jobItem.Status == SyncJobItemStatus.Queued)
                {
                    await _syncManager.UpdateSyncJobItemInternal(jobItem).ConfigureAwait(false);
                    return;
                }

                jobItem.MediaSource = await GetEncodedMediaSource(jobItem.OutputPath, user, false).ConfigureAwait(false);
            }
            else
            {
                if (mediaSource.Protocol == MediaProtocol.File)
                {
                    jobItem.OutputPath = mediaSource.Path;
                }
                else if (mediaSource.Protocol == MediaProtocol.Http)
                {
                    jobItem.OutputPath = await DownloadFile(jobItem, mediaSource, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    throw new InvalidOperationException(string.Format("Cannot direct stream {0} protocol", mediaSource.Protocol));
                }

                jobItem.ItemDateModifiedTicks = item.DateModified.Ticks;
                jobItem.MediaSource = mediaSource;
            }

            jobItem.MediaSource.SupportsTranscoding = false;

            jobItem.Progress = 50;
            jobItem.Status = SyncJobItemStatus.ReadyToTransfer;
            await _syncManager.UpdateSyncJobItemInternal(jobItem).ConfigureAwait(false);
        }

        private async Task Sync(SyncJobItem jobItem, Photo item, CancellationToken cancellationToken)
        {
            jobItem.OutputPath = item.Path;

            jobItem.Progress = 50;
            jobItem.Status = SyncJobItemStatus.ReadyToTransfer;
            jobItem.ItemDateModifiedTicks = item.DateModified.Ticks;
            await _syncManager.UpdateSyncJobItemInternal(jobItem).ConfigureAwait(false);
        }

        private async Task SyncGeneric(SyncJobItem jobItem, BaseItem item, CancellationToken cancellationToken)
        {
            jobItem.OutputPath = item.Path;

            jobItem.Progress = 50;
            jobItem.Status = SyncJobItemStatus.ReadyToTransfer;
            jobItem.ItemDateModifiedTicks = item.DateModified.Ticks;
            await _syncManager.UpdateSyncJobItemInternal(jobItem).ConfigureAwait(false);
        }

        private async Task<string> DownloadFile(SyncJobItem jobItem, MediaSourceInfo mediaSource, CancellationToken cancellationToken)
        {
            // TODO: Download
            return mediaSource.Path;
        }

        public string GetTemporaryPath(SyncJob job)
        {
            return GetTemporaryPath(job.Id);
        }

        public string GetTemporaryPath(string jobId)
        {
            var basePath = _config.GetSyncOptions().TemporaryPath;

            if (string.IsNullOrWhiteSpace(basePath))
            {
                basePath = Path.Combine(_config.CommonApplicationPaths.ProgramDataPath, "sync");
            }

            return Path.Combine(basePath, jobId);
        }

        public string GetTemporaryPath(SyncJobItem jobItem)
        {
            return Path.Combine(GetTemporaryPath(jobItem.JobId), jobItem.Id);
        }

        private async Task<MediaSourceInfo> GetEncodedMediaSource(string path, User user, bool isVideo)
        {
            var item = _libraryManager.ResolvePath(_fileSystem.GetFileSystemInfo(path));

            await item.RefreshMetadata(CancellationToken.None).ConfigureAwait(false);

            var hasMediaSources = item as IHasMediaSources;

            var mediaSources = _mediaSourceManager.GetStaticMediaSources(hasMediaSources, false).ToList();

            var preferredAudio = string.IsNullOrEmpty(user.Configuration.AudioLanguagePreference)
                ? new string[] { }
                : new[] { user.Configuration.AudioLanguagePreference };

            var preferredSubs = string.IsNullOrEmpty(user.Configuration.SubtitleLanguagePreference)
                ? new List<string>() : new List<string> { user.Configuration.SubtitleLanguagePreference };

            foreach (var source in mediaSources)
            {
                if (isVideo)
                {
                    source.DefaultAudioStreamIndex =
                        MediaStreamSelector.GetDefaultAudioStreamIndex(source.MediaStreams, preferredAudio, user.Configuration.PlayDefaultAudioTrack);

                    var defaultAudioIndex = source.DefaultAudioStreamIndex;
                    var audioLangage = defaultAudioIndex == null
                        ? null
                        : source.MediaStreams.Where(i => i.Type == MediaStreamType.Audio && i.Index == defaultAudioIndex).Select(i => i.Language).FirstOrDefault();

                    source.DefaultAudioStreamIndex =
                        MediaStreamSelector.GetDefaultSubtitleStreamIndex(source.MediaStreams, preferredSubs, user.Configuration.SubtitleMode, audioLangage);
                }
                else
                {
                    var audio = source.MediaStreams.FirstOrDefault(i => i.Type == MediaStreamType.Audio);

                    if (audio != null)
                    {
                        source.DefaultAudioStreamIndex = audio.Index;
                    }

                }
            }

            return mediaSources.FirstOrDefault();
        }
    }
}
