using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Sync;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Session;
using MediaBrowser.Model.Sync;
using MoreLinq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Sync
{
    public class SyncJobProcessor
    {
        private readonly ILibraryManager _libraryManager;
        private readonly ISyncRepository _syncRepo;
        private readonly ISyncManager _syncManager;
        private readonly ILogger _logger;
        private readonly IUserManager _userManager;

        public SyncJobProcessor(ILibraryManager libraryManager, ISyncRepository syncRepo, ISyncManager syncManager, ILogger logger, IUserManager userManager)
        {
            _libraryManager = libraryManager;
            _syncRepo = syncRepo;
            _syncManager = syncManager;
            _logger = logger;
            _userManager = userManager;
        }

        public void ProcessJobItem(SyncJob job, SyncJobItem jobItem, SyncTarget target)
        {

        }

        public async Task EnsureJobItems(SyncJob job)
        {
            var user = _userManager.GetUserById(job.UserId);

            if (user == null)
            {
                throw new InvalidOperationException("Cannot proceed with sync because user no longer exists.");
            }

            var items = GetItemsForSync(job.RequestedItemIds, user)
                .ToList();

            var jobItems = _syncRepo.GetJobItems(new SyncJobItemQuery
            {
                JobId = job.Id

            }).Items.ToList();

            foreach (var item in items)
            {
                var itemId = item.Id.ToString("N");

                var jobItem = jobItems.FirstOrDefault(i => string.Equals(i.ItemId, itemId, StringComparison.OrdinalIgnoreCase));

                if (jobItem != null)
                {
                    continue;
                }

                jobItem = new SyncJobItem
                {
                    Id = Guid.NewGuid().ToString("N"),
                    ItemId = itemId,
                    JobId = job.Id,
                    TargetId = job.TargetId,
                    DateCreated = DateTime.UtcNow
                };

                await _syncRepo.Create(jobItem).ConfigureAwait(false);

                jobItems.Add(jobItem);
            }

            jobItems = jobItems
                .OrderBy(i => i.DateCreated)
                .ToList();

            await UpdateJobStatus(job, jobItems).ConfigureAwait(false);
        }

        private Task UpdateJobStatus(SyncJob job)
        {
            if (job == null)
            {
                throw new ArgumentNullException("job");
            }

            var result = _syncRepo.GetJobItems(new SyncJobItemQuery
            {
                JobId = job.Id
            });

            return UpdateJobStatus(job, result.Items.ToList());
        }

        private Task UpdateJobStatus(SyncJob job, List<SyncJobItem> jobItems)
        {
            job.ItemCount = jobItems.Count;

            double pct = 0;

            foreach (var item in jobItems)
            {
                if (item.Status == SyncJobItemStatus.Failed || item.Status == SyncJobItemStatus.Completed)
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

            if (pct >= 100)
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
            else if (pct.Equals(0))
            {
                job.Status = SyncJobStatus.Queued;
            }
            else
            {
                job.Status = SyncJobStatus.InProgress;
            }

            return _syncRepo.Update(job);
        }

        public IEnumerable<BaseItem> GetItemsForSync(IEnumerable<string> itemIds, User user)
        {
            return itemIds
                .SelectMany(i => GetItemsForSync(i, user))
                .Where(_syncManager.SupportsSync)
                .DistinctBy(i => i.Id);
        }

        private IEnumerable<BaseItem> GetItemsForSync(string id, User user)
        {
            var item = _libraryManager.GetItemById(id);

            if (item == null)
            {
                return new List<BaseItem>();
            }

            return GetItemsForSync(item, user);
        }

        private IEnumerable<BaseItem> GetItemsForSync(BaseItem item, User user)
        {
            var itemByName = item as IItemByName;
            if (itemByName != null)
            {
                var items = user.RootFolder
                    .GetRecursiveChildren(user);

                return itemByName.GetTaggedItems(items);
            } 
            
            if (item.IsFolder)
            {
                var folder = (Folder)item;
                var items = folder.GetRecursiveChildren(user);

                items = items.Where(i => !i.IsFolder);

                if (!folder.IsPreSorted)
                {
                    items = items.OrderBy(i => i.SortName);
                }

                return items;
            }

            return new[] { item };
        }

        public async Task EnsureSyncJobs(CancellationToken cancellationToken)
        {
            var jobResult = _syncRepo.GetJobs(new SyncJobQuery
            {
                IsCompleted = false
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
            await EnsureSyncJobs(cancellationToken).ConfigureAwait(false);

            var result = _syncRepo.GetJobItems(new SyncJobItemQuery
            {
                IsCompleted = false
            });

            var jobItems = result.Items;
            var index = 0;

            foreach (var item in jobItems)
            {
                double percent = index;
                percent /= result.TotalRecordCount;

                progress.Report(100 * percent);

                cancellationToken.ThrowIfCancellationRequested();

                if (item.Status == SyncJobItemStatus.Queued)
                {
                    await ProcessJobItem(item, cancellationToken).ConfigureAwait(false);
                }

                var job = _syncRepo.GetJob(item.JobId);
                await UpdateJobStatus(job).ConfigureAwait(false);

                index++;
            }
        }

        private async Task ProcessJobItem(SyncJobItem jobItem, CancellationToken cancellationToken)
        {
            var item = _libraryManager.GetItemById(jobItem.ItemId);
            if (item == null)
            {
                jobItem.Status = SyncJobItemStatus.Failed;
                _logger.Error("Unable to locate library item for JobItem {0}, ItemId {1}", jobItem.Id, jobItem.ItemId);
                await _syncRepo.Update(jobItem).ConfigureAwait(false);
                return;
            }

            var deviceProfile = _syncManager.GetDeviceProfile(jobItem.TargetId);
            if (deviceProfile == null)
            {
                jobItem.Status = SyncJobItemStatus.Failed;
                _logger.Error("Unable to locate SyncTarget for JobItem {0}, SyncTargetId {1}", jobItem.Id, jobItem.TargetId);
                await _syncRepo.Update(jobItem).ConfigureAwait(false);
                return;
            }

            jobItem.Progress = 0;
            jobItem.Status = SyncJobItemStatus.Converting;

            var video = item as Video;
            if (video != null)
            {
                jobItem.OutputPath = await Sync(jobItem, video, deviceProfile, cancellationToken).ConfigureAwait(false);
            }

            else if (item is Audio)
            {
                jobItem.OutputPath = await Sync(jobItem, (Audio)item, deviceProfile, cancellationToken).ConfigureAwait(false);
            }

            else if (item is Photo)
            {
                jobItem.OutputPath = await Sync(jobItem, (Photo)item, deviceProfile, cancellationToken).ConfigureAwait(false);
            }

            else if (item is Game)
            {
                jobItem.OutputPath = await Sync(jobItem, (Game)item, deviceProfile, cancellationToken).ConfigureAwait(false);
            }

            else if (item is Book)
            {
                jobItem.OutputPath = await Sync(jobItem, (Book)item, deviceProfile, cancellationToken).ConfigureAwait(false);
            }

            jobItem.Progress = 50;
            jobItem.Status = SyncJobItemStatus.Transferring;
            await _syncRepo.Update(jobItem).ConfigureAwait(false);
        }

        private async Task<string> Sync(SyncJobItem jobItem, Video item, DeviceProfile profile, CancellationToken cancellationToken)
        {
            var options = new VideoOptions
            {
                Context = EncodingContext.Streaming,
                ItemId = item.Id.ToString("N"),
                DeviceId = jobItem.TargetId,
                Profile = profile,
                MediaSources = item.GetMediaSources(false).ToList()
            };

            var streamInfo = new StreamBuilder().BuildVideoItem(options);
            var mediaSource = streamInfo.MediaSource;

            if (streamInfo.PlayMethod != PlayMethod.Transcode)
            {
                if (mediaSource.Protocol == MediaProtocol.File)
                {
                    return mediaSource.Path;
                }
                if (mediaSource.Protocol == MediaProtocol.Http)
                {
                    return await DownloadFile(jobItem, mediaSource, cancellationToken).ConfigureAwait(false);
                }
                throw new InvalidOperationException(string.Format("Cannot direct stream {0} protocol", mediaSource.Protocol));
            }

            // TODO: Transcode
            return mediaSource.Path;
        }

        private async Task<string> Sync(SyncJobItem jobItem, Audio item, DeviceProfile profile, CancellationToken cancellationToken)
        {
            var options = new AudioOptions
            {
                Context = EncodingContext.Streaming,
                ItemId = item.Id.ToString("N"),
                DeviceId = jobItem.TargetId,
                Profile = profile,
                MediaSources = item.GetMediaSources(false).ToList()
            };

            var streamInfo = new StreamBuilder().BuildAudioItem(options);
            var mediaSource = streamInfo.MediaSource;

            if (streamInfo.PlayMethod != PlayMethod.Transcode)
            {
                if (mediaSource.Protocol == MediaProtocol.File)
                {
                    return mediaSource.Path;
                }
                if (mediaSource.Protocol == MediaProtocol.Http)
                {
                    return await DownloadFile(jobItem, mediaSource, cancellationToken).ConfigureAwait(false);
                }
                throw new InvalidOperationException(string.Format("Cannot direct stream {0} protocol", mediaSource.Protocol));
            }

            // TODO: Transcode
            return mediaSource.Path;
        }

        private async Task<string> Sync(SyncJobItem jobItem, Photo item, DeviceProfile profile, CancellationToken cancellationToken)
        {
            return item.Path;
        }

        private async Task<string> Sync(SyncJobItem jobItem, Game item, DeviceProfile profile, CancellationToken cancellationToken)
        {
            return item.Path;
        }

        private async Task<string> Sync(SyncJobItem jobItem, Book item, DeviceProfile profile, CancellationToken cancellationToken)
        {
            return item.Path;
        }

        private async Task<string> DownloadFile(SyncJobItem jobItem, MediaSourceInfo mediaSource, CancellationToken cancellationToken)
        {
            // TODO: Download
            return mediaSource.Path;
        }
    }
}
