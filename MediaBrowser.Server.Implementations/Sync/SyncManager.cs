using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Sync;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Sync;
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

        private ISyncProvider[] _providers = { };

        public SyncManager(ILibraryManager libraryManager, ISyncRepository repo, IImageProcessor imageProcessor, ILogger logger)
        {
            _libraryManager = libraryManager;
            _repo = repo;
            _imageProcessor = imageProcessor;
            _logger = logger;
        }

        public void AddParts(IEnumerable<ISyncProvider> providers)
        {
            _providers = providers.ToArray();
        }

        public async Task<SyncJobCreationResult> CreateJob(SyncJobRequest request)
        {
            var items = GetItemsForSync(request.ItemIds).ToList();

            if (items.Count == 1)
            {
                request.Name = GetDefaultName(items[0]);
            }

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                throw new ArgumentException("Please supply a name for the sync job.");
            }

            var target = GetSyncTargets(request.UserId)
                .First(i => string.Equals(request.TargetId, i.Id));

            var jobId = Guid.NewGuid().ToString("N");

            var job = new SyncJob
            {
                Id = jobId,
                Name = request.Name,
                TargetId = target.Id,
                UserId = request.UserId,
                UnwatchedOnly = request.UnwatchedOnly,
                Limit = request.Limit,
                LimitType = request.LimitType,
                RequestedItemIds = request.ItemIds,
                DateCreated = DateTime.UtcNow,
                DateLastModified = DateTime.UtcNow,
                ItemCount = 1
            };

            await _repo.Create(job).ConfigureAwait(false);

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
            var item = GetItemsForSync(job.RequestedItemIds)
                .FirstOrDefault();

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
            throw new NotImplementedException();
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
            var providerId = GetSyncProviderId(provider);

            return provider.GetSyncTargets().Select(i => new SyncTarget
            {
                Name = i.Name,
                Id = providerId + "-" + i.Id
            });
        }

        private ISyncProvider GetSyncProvider(SyncTarget target)
        {
            var providerId = target.Id.Split(new[] { '-' }, 2).First();

            return _providers.First(i => string.Equals(providerId, GetSyncProviderId(i)));
        }

        private string GetSyncProviderId(ISyncProvider provider)
        {
            return (provider.GetType().Name + provider.Name).GetMD5().ToString("N");
        }

        public bool SupportsSync(BaseItem item)
        {
            if (item.LocationType == LocationType.Virtual)
            {
                return false;
            }

            if (string.Equals(item.MediaType, MediaType.Audio, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(item.MediaType, MediaType.Video, StringComparison.OrdinalIgnoreCase))
            {
                if (item.RunTimeTicks.HasValue)
                {
                    var video = item as Video;

                    if (video != null)
                    {
                        if (video.VideoType != VideoType.VideoFile)
                        {
                            return false;
                        }

                        if (video.IsMultiPart)
                        {
                            return false;
                        }
                    }

                    return true;
                }

                return false;
            }

            return false;
        }

        private IEnumerable<BaseItem> GetItemsForSync(IEnumerable<string> itemIds)
        {
            return itemIds.SelectMany(GetItemsForSync).DistinctBy(i => i.Id);
        }

        private IEnumerable<BaseItem> GetItemsForSync(string id)
        {
            var item = _libraryManager.GetItemById(id);

            if (item == null)
            {
                throw new ArgumentException("Item with Id " + id + " not found.");
            }

            if (!SupportsSync(item))
            {
                throw new ArgumentException("Item with Id " + id + " does not support sync.");
            }

            return GetItemsForSync(item);
        }

        private IEnumerable<BaseItem> GetItemsForSync(BaseItem item)
        {
            return new[] { item };
        }

        private string GetDefaultName(BaseItem item)
        {
            return item.Name;
        }
    }
}
