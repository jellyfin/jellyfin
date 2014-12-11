using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.TV;
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
            var items = new SyncJobProcessor(_libraryManager, _repo)
                .GetItemsForSync(request.ItemIds)
                .ToList();

            if (items.Any(i => !SupportsSync(i)))
            {
                throw new ArgumentException("Item does not support sync.");
            }

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
                ItemLimit = request.ItemLimit,
                RequestedItemIds = request.ItemIds,
                DateCreated = DateTime.UtcNow,
                DateLastModified = DateTime.UtcNow,
                SyncNewContent = request.SyncNewContent,
                RemoveWhenWatched = request.RemoveWhenWatched,
                ItemCount = items.Count,
                Quality = request.Quality
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
            var item = new SyncJobProcessor(_libraryManager, _repo)
                .GetItemsForSync(job.RequestedItemIds)
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
            var job = GetJob(id);

            job.Status = SyncJobStatus.Cancelled;

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
            return (provider.GetType().Name).GetMD5().ToString("N");
        }

        public bool SupportsSync(BaseItem item)
        {
            if (string.Equals(item.MediaType, MediaType.Audio, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(item.MediaType, MediaType.Video, StringComparison.OrdinalIgnoreCase))
            {
                if (item.LocationType == LocationType.Virtual)
                {
                    return false;
                }

                if (item.RunTimeTicks.HasValue)
                {
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

                    return true;
                }

                return false;
            }

            return item.LocationType == LocationType.FileSystem || item is Season;
        }

        private string GetDefaultName(BaseItem item)
        {
            return item.Name;
        }
    }
}
