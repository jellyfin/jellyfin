using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Sync;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Sync;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Sync
{
    public class SyncedMediaSourceProvider : IMediaSourceProvider
    {
        private readonly ISyncManager _syncManager;

        public SyncedMediaSourceProvider(ISyncManager syncManager)
        {
            _syncManager = syncManager;
        }

        public async Task<IEnumerable<MediaSourceInfo>> GetMediaSources(IHasMediaSources item, CancellationToken cancellationToken)
        {
            var jobItemResult = _syncManager.GetJobItems(new SyncJobItemQuery
            {
                AddMetadata = false,
                Statuses = new List<SyncJobItemStatus> { SyncJobItemStatus.Synced },
                ItemId = item.Id.ToString("N")
            });

            var jobItems = jobItemResult
                .Items
                .Where(i => true);

            return new List<MediaSourceInfo>();
        }
    }
}
