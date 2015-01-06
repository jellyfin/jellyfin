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
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Sync;
using MediaBrowser.Controller.TV;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Sync;
using MediaBrowser.Model.Users;
using MoreLinq;
using System;
using System.Collections.Generic;
using System.IO;
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
        private readonly Func<IDtoService> _dtoService;
        private readonly IApplicationHost _appHost;
        private readonly ITVSeriesManager _tvSeriesManager;
        private readonly Func<IMediaEncoder> _mediaEncoder;

        private ISyncProvider[] _providers = { };

        public SyncManager(ILibraryManager libraryManager, ISyncRepository repo, IImageProcessor imageProcessor, ILogger logger, IUserManager userManager, Func<IDtoService> dtoService, IApplicationHost appHost, ITVSeriesManager tvSeriesManager, Func<IMediaEncoder> mediaEncoder)
        {
            _libraryManager = libraryManager;
            _repo = repo;
            _imageProcessor = imageProcessor;
            _logger = logger;
            _userManager = userManager;
            _dtoService = dtoService;
            _appHost = appHost;
            _tvSeriesManager = tvSeriesManager;
            _mediaEncoder = mediaEncoder;
        }

        public void AddParts(IEnumerable<ISyncProvider> providers)
        {
            _providers = providers.ToArray();
        }

        public async Task<SyncJobCreationResult> CreateJob(SyncJobRequest request)
        {
            var processor = new SyncJobProcessor(_libraryManager, _repo, this, _logger, _userManager, _tvSeriesManager, _mediaEncoder());

            var user = _userManager.GetUserById(request.UserId);

            var items = (await processor
                .GetItemsForSync(request.Category, request.ParentId, request.ItemIds, user, request.UnwatchedOnly).ConfigureAwait(false))
                .ToList();

            if (items.Any(i => !SupportsSync(i)))
            {
                throw new ArgumentException("Item does not support sync.");
            }

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                if (request.ItemIds.Count == 1)
                {
                    request.Name = GetDefaultName(_libraryManager.GetItemById(request.ItemIds[0]));
                }
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
                RequestedItemIds = request.ItemIds ?? new List<string> { },
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

        public Task UpdateJob(SyncJob job)
        {
            // Get fresh from the db and only update the fields that are supported to be changed.
            var instance = _repo.GetJob(job.Id);

            instance.Name = job.Name;
            instance.Quality = job.Quality;
            instance.UnwatchedOnly = job.UnwatchedOnly;
            instance.SyncNewContent = job.SyncNewContent;
            instance.ItemLimit = job.ItemLimit;

            return _repo.Update(instance);
        }

        public async Task<QueryResult<SyncJob>> GetJobs(SyncJobQuery query)
        {
            var result = _repo.GetJobs(query);

            foreach (var item in result.Items)
            {
                await FillMetadata(item).ConfigureAwait(false);
            }

            return result;
        }

        private async Task FillMetadata(SyncJob job)
        {
            var item = job.RequestedItemIds
                .Select(_libraryManager.GetItemById)
                .FirstOrDefault(i => i != null);

            if (item == null)
            {
                var processor = new SyncJobProcessor(_libraryManager, _repo, this, _logger, _userManager, _tvSeriesManager, _mediaEncoder());

                var user = _userManager.GetUserById(job.UserId);

                item = (await processor
                    .GetItemsForSync(job.Category, job.ParentId, job.RequestedItemIds, user, job.UnwatchedOnly).ConfigureAwait(false))
                    .FirstOrDefault();
            }

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
                var itemWithImage = item;

                if (primaryImage == null)
                {
                    var parentWithImage = item.Parents.FirstOrDefault(i => i.HasImage(ImageType.Primary));

                    if (parentWithImage != null)
                    {
                        itemWithImage = parentWithImage;
                        primaryImage = parentWithImage.GetImageInfo(ImageType.Primary, 0);
                    }
                }

                if (primaryImage != null)
                {
                    try
                    {
                        job.PrimaryImageTag = _imageProcessor.GetImageCacheTag(itemWithImage, ImageType.Primary);
                        job.PrimaryImageItemId = itemWithImage.Id.ToString("N");

                    }
                    catch (Exception ex)
                    {
                        _logger.ErrorException("Error getting image info", ex);
                    }
                }
            }
        }

        private void FillMetadata(SyncJobItem jobItem)
        {
            var item = _libraryManager.GetItemById(jobItem.ItemId);

            if (item == null)
            {
                return;
            }

            var primaryImage = item.GetImageInfo(ImageType.Primary, 0);
            var itemWithImage = item;

            if (primaryImage == null)
            {
                var parentWithImage = item.Parents.FirstOrDefault(i => i.HasImage(ImageType.Primary));

                if (parentWithImage != null)
                {
                    itemWithImage = parentWithImage;
                    primaryImage = parentWithImage.GetImageInfo(ImageType.Primary, 0);
                }
            }

            if (primaryImage != null)
            {
                try
                {
                    jobItem.PrimaryImageTag = _imageProcessor.GetImageCacheTag(itemWithImage, ImageType.Primary);
                    jobItem.PrimaryImageItemId = itemWithImage.Id.ToString("N");

                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error getting image info", ex);
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
            return provider.GetSyncTargets(userId).Select(i => new SyncTarget
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

                // It would be nice to support these later
                if (item is Game || item is Book)
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

            jobItem.Status = SyncJobItemStatus.Synced;
            jobItem.Progress = 100;

            if (jobItem.RequiresConversion)
            {
                try
                {
                    File.Delete(jobItem.OutputPath);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error deleting temporary job file: {0}", ex, jobItem.OutputPath);
                }
            }

            await _repo.Update(jobItem).ConfigureAwait(false);

            var processor = new SyncJobProcessor(_libraryManager, _repo, this, _logger, _userManager, _tvSeriesManager, _mediaEncoder());

            await processor.UpdateJobStatus(jobItem.JobId).ConfigureAwait(false);
        }

        public SyncJobItem GetJobItem(string id)
        {
            return _repo.GetJobItem(id);
        }

        public QueryResult<SyncJobItem> GetJobItems(SyncJobItemQuery query)
        {
            var result = _repo.GetJobItems(query);

            if (query.AddMetadata)
            {
                result.Items.ForEach(FillMetadata);
            }

            return result;
        }

        private SyncedItem GetJobItemInfo(SyncJobItem jobItem)
        {
            var job = _repo.GetJob(jobItem.JobId);

            var libraryItem = _libraryManager.GetItemById(jobItem.ItemId);

            var syncedItem = new SyncedItem
            {
                SyncJobId = jobItem.JobId,
                SyncJobItemId = jobItem.Id,
                ServerId = _appHost.SystemId,
                UserId = job.UserId
            };

            var dtoOptions = new DtoOptions();

            // Remove some bloat
            dtoOptions.Fields.Remove(ItemFields.MediaStreams);
            dtoOptions.Fields.Remove(ItemFields.IndexOptions);
            dtoOptions.Fields.Remove(ItemFields.MediaSourceCount);
            dtoOptions.Fields.Remove(ItemFields.OriginalPrimaryImageAspectRatio);
            dtoOptions.Fields.Remove(ItemFields.Path);
            dtoOptions.Fields.Remove(ItemFields.SeriesGenres);
            dtoOptions.Fields.Remove(ItemFields.Settings);
            dtoOptions.Fields.Remove(ItemFields.SyncInfo);

            syncedItem.Item = _dtoService().GetBaseItemDto(libraryItem, dtoOptions);

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

        public List<SyncedItem> GetReadySyncItems(string targetId)
        {
            var jobItemResult = GetJobItems(new SyncJobItemQuery
            {
                TargetId = targetId,
                Statuses = new List<SyncJobItemStatus> { SyncJobItemStatus.Transferring }
            });

            return jobItemResult.Items.Select(GetJobItemInfo)
                .ToList();
        }

        public async Task<SyncDataResponse> SyncData(SyncDataRequest request)
        {
            var jobItemResult = GetJobItems(new SyncJobItemQuery
            {
                TargetId = request.TargetId,
                Statuses = new List<SyncJobItemStatus> { SyncJobItemStatus.Synced }
            });

            var response = new SyncDataResponse();

            foreach (var jobItem in jobItemResult.Items)
            {
                if (request.LocalItemIds.Contains(jobItem.ItemId, StringComparer.OrdinalIgnoreCase))
                {
                    var job = _repo.GetJob(jobItem.JobId);
                    var user = _userManager.GetUserById(job.UserId);

                    if (user == null)
                    {
                        // Tell the device to remove it since the user is gone now
                        response.ItemIdsToRemove.Add(jobItem.ItemId);
                    }
                    else if (job.UnwatchedOnly)
                    {
                        var libraryItem = _libraryManager.GetItemById(jobItem.ItemId);

                        if (IsLibraryItemAvailable(libraryItem))
                        {
                            if (libraryItem.IsPlayed(user) && libraryItem is Video)
                            {
                                // Tell the device to remove it since it has been played
                                response.ItemIdsToRemove.Add(jobItem.ItemId);
                            }
                        }
                        else
                        {
                            // Tell the device to remove it since it's no longer available
                            response.ItemIdsToRemove.Add(jobItem.ItemId);
                        }
                    }
                }
                else
                {
                    // Content is no longer on the device
                    jobItem.Status = SyncJobItemStatus.RemovedFromDevice;
                    await _repo.Update(jobItem).ConfigureAwait(false);
                }
            }

            // Now check each item that's on the device
            foreach (var itemId in request.LocalItemIds)
            {
                // See if it's already marked for removal
                if (response.ItemIdsToRemove.Contains(itemId, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                // If there isn't a sync job for this item, mark it for removal
                if (!jobItemResult.Items.Any(i => string.Equals(itemId, i.ItemId, StringComparison.OrdinalIgnoreCase)))
                {
                    response.ItemIdsToRemove.Add(itemId);
                }
            }

            response.ItemIdsToRemove = response.ItemIdsToRemove.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            return response;
        }

        private bool IsLibraryItemAvailable(BaseItem item)
        {
            if (item == null)
            {
                return false;
            }

            // TODO: Make sure it hasn't been deleted

            return true;
        }
    }
}
