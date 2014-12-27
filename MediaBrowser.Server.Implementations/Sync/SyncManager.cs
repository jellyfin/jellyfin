using System.IO;
using MediaBrowser.Common;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Sync;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Sync;
using MediaBrowser.Model.Users;
using MoreLinq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Sync
{
    public class SyncManager : ISyncManager
    {
        private readonly ILibraryManager _libraryManager;
        private readonly ISyncRepository _repo;
        private readonly IImageProcessor _imageProcessor;
        private readonly ILogger _logger;
        private readonly IUserManager _userManager;
        private readonly IDtoService _dtoService;
        private readonly IApplicationHost _appHost;

        private ISyncProvider[] _providers = { };

        public SyncManager(ILibraryManager libraryManager, ISyncRepository repo, IImageProcessor imageProcessor, ILogger logger, IUserManager userManager, IDtoService dtoService, IApplicationHost appHost)
        {
            _libraryManager = libraryManager;
            _repo = repo;
            _imageProcessor = imageProcessor;
            _logger = logger;
            _userManager = userManager;
            _dtoService = dtoService;
            _appHost = appHost;
        }

        public void AddParts(IEnumerable<ISyncProvider> providers)
        {
            _providers = providers.ToArray();
        }

        public async Task<SyncJobCreationResult> CreateJob(SyncJobRequest request)
        {
            var processor = new SyncJobProcessor(_libraryManager, _repo, this, _logger, _userManager);

            var user = _userManager.GetUserById(request.UserId);

            var items = processor
                .GetItemsForSync(request.ItemIds, user, request.UnwatchedOnly)
                .ToList();

            if (items.Any(i => !SupportsSync(i)))
            {
                throw new ArgumentException("Item does not support sync.");
            }

            if (string.IsNullOrWhiteSpace(request.Name) && request.ItemIds.Count == 1)
            {
                request.Name = GetDefaultName(_libraryManager.GetItemById(request.ItemIds[0]));
            }

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                throw new ArgumentException("Please supply a name for the sync job.");
            }

            var target = GetSyncTargets(request.UserId)
                .FirstOrDefault(i => string.Equals(request.TargetId, i.Id));

            if (target == null)
            {
                throw new ArgumentException("Sync target not found.");
            }

            var jobId = Guid.NewGuid().ToString("N");

            var job = new SyncJob
            {
                Id = jobId,
                Name = request.Name,
                TargetId = target.Id,
                UserId = request.UserId,
                UnwatchedOnly = request.UnwatchedOnly,
                ItemLimit = request.ItemLimit,
                RequestedItemIds = request.ItemIds,
                DateCreated = DateTime.UtcNow,
                DateLastModified = DateTime.UtcNow,
                SyncNewContent = request.SyncNewContent,
                ItemCount = items.Count,
                Quality = request.Quality,
                Category = request.Category,
                ParentId = request.ParentId
            };

            // It's just a static list
            if (!items.Any(i => i.IsFolder || i is IItemByName))
            {
                job.SyncNewContent = false;
            }

            await _repo.Create(job).ConfigureAwait(false);

            await processor.EnsureJobItems(job).ConfigureAwait(false);

            return new SyncJobCreationResult
            {
                Job = GetJob(jobId)
            };
        }

        public QueryResult<SyncJob> GetJobs(SyncJobQuery query)
        {
            var result = _repo.GetJobs(query);

            result.Items.ForEach(FillMetadata);

            return result;
        }

        private void FillMetadata(SyncJob job)
        {
            var item = job.RequestedItemIds
                .Select(_libraryManager.GetItemById)
                .FirstOrDefault(i => i != null);

            if (item != null)
            {
                var hasSeries = item as IHasSeries;
                if (hasSeries != null)
                {
                    job.ParentName = hasSeries.SeriesName;
                }

                var hasAlbumArtist = item as IHasAlbumArtist;
                if (hasAlbumArtist != null)
                {
                    job.ParentName = hasAlbumArtist.AlbumArtists.FirstOrDefault();
                }

                var primaryImage = item.GetImageInfo(ImageType.Primary, 0);

                if (primaryImage != null)
                {
                    try
                    {
                        job.PrimaryImageTag = _imageProcessor.GetImageCacheTag(item, ImageType.Primary);
                        job.PrimaryImageItemId = item.Id.ToString("N");

                    }
                    catch (Exception ex)
                    {
                        _logger.ErrorException("Error getting image info", ex);
                    }
                }
            }
        }

        public Task CancelJob(string id)
        {
            return _repo.DeleteJob(id);
        }

        public SyncJob GetJob(string id)
        {
            return _repo.GetJob(id);
        }

        public IEnumerable<SyncTarget> GetSyncTargets(string userId)
        {
            return _providers
                .SelectMany(i => GetSyncTargets(i, userId))
                .OrderBy(i => i.Name);
        }

        private IEnumerable<SyncTarget> GetSyncTargets(ISyncProvider provider, string userId)
        {
            return provider.GetSyncTargets().Select(i => new SyncTarget
            {
                Name = i.Name,
                Id = GetSyncTargetId(provider, i)
            });
        }

        private string GetSyncTargetId(ISyncProvider provider, SyncTarget target)
        {
            var hasUniqueId = provider as IHasUniqueTargetIds;

            if (hasUniqueId != null)
            {
                return target.Id;
            }

            var providerId = GetSyncProviderId(provider);
            return (providerId + "-" + target.Id).GetMD5().ToString("N");
        }

        private ISyncProvider GetSyncProvider(SyncTarget target)
        {
            var providerId = target.Id.Split(new[] { '-' }, 2).First();

            return _providers.First(i => string.Equals(providerId, GetSyncProviderId(i)));
        }

        private string GetSyncProviderId(ISyncProvider provider)
        {
            return (provider.GetType().Name).GetMD5().ToString("N");
        }

        public bool SupportsSync(BaseItem item)
        {
            if (string.Equals(item.MediaType, MediaType.Video, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(item.MediaType, MediaType.Audio, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(item.MediaType, MediaType.Photo, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(item.MediaType, MediaType.Game, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(item.MediaType, MediaType.Book, StringComparison.OrdinalIgnoreCase))
            {
                if (item.LocationType == LocationType.Virtual)
                {
                    return false;
                }

                if (!item.RunTimeTicks.HasValue)
                {
                    return false;
                }

                var video = item as Video;
                if (video != null)
                {
                    if (video.VideoType == VideoType.Iso)
                    {
                        return false;
                    }

                    if (video.IsStacked)
                    {
                        return false;
                    }
                }

                var game = item as Game;
                if (game != null)
                {
                    if (game.IsMultiPart)
                    {
                        return false;
                    }
                }

                if (item is LiveTvChannel || item is IChannelItem || item is ILiveTvRecording)
                {
                    return false;
                }

                return true;
            }

            return item.LocationType == LocationType.FileSystem || item is Season || item is ILiveTvRecording;
        }

        private string GetDefaultName(BaseItem item)
        {
            return item.Name;
        }

        public DeviceProfile GetDeviceProfile(string targetId)
        {
            foreach (var provider in _providers)
            {
                foreach (var target in GetSyncTargets(provider, null))
                {
                    if (string.Equals(target.Id, targetId, StringComparison.OrdinalIgnoreCase))
                    {
                        return provider.GetDeviceProfile(target);
                    }
                }
            }

            return null;
        }

        public async Task ReportSyncJobItemTransferred(string id)
        {
            var jobItem = _repo.GetJobItem(id);

            jobItem.Status = SyncJobItemStatus.Completed;
            jobItem.Progress = 100;

            await _repo.Update(jobItem).ConfigureAwait(false);

            var processor = new SyncJobProcessor(_libraryManager, _repo, this, _logger, _userManager);

            await processor.UpdateJobStatus(jobItem.JobId).ConfigureAwait(false);
        }

        public SyncJobItem GetJobItem(string id)
        {
            return _repo.GetJobItem(id);
        }

        public QueryResult<SyncJobItem> GetJobItems(SyncJobItemQuery query)
        {
            return _repo.GetJobItems(query);
        }

        public SyncedItem GetJobItemInfo(string id)
        {
            var jobItem = GetJobItem(id);
            var job = _repo.GetJob(jobItem.JobId);

            var libraryItem = _libraryManager.GetItemById(jobItem.ItemId);

            var syncedItem = new SyncedItem
            {
                SyncJobId = jobItem.JobId,
                SyncJobItemId = jobItem.Id,
                ServerId = _appHost.SystemId,
                UserId = job.UserId
            };

            syncedItem.Item = _dtoService.GetBaseItemDto(libraryItem, new DtoOptions());

            // TODO: this should be the media source of the transcoded output
            syncedItem.Item.MediaSources = syncedItem.Item.MediaSources
                .Where(i => string.Equals(i.Id, jobItem.MediaSourceId))
                .ToList();

            var mediaSource = syncedItem.Item.MediaSources
               .FirstOrDefault(i => string.Equals(i.Id, jobItem.MediaSourceId));

            // This will be null for items that are not audio/video
            if (mediaSource == null)
            {
                syncedItem.OriginalFileName = Path.GetFileName(libraryItem.Path);
            }
            else
            {
                syncedItem.OriginalFileName = Path.GetFileName(mediaSource.Path);
            }

            return syncedItem;
        }

        public Task ReportOfflineAction(UserAction action)
        {
            return Task.FromResult(true);
        }
    }
}
