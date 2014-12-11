using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Sync;
using MediaBrowser.Model.Sync;
using MoreLinq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Sync
{
    public class SyncJobProcessor
    {
        private readonly ILibraryManager _libraryManager;
        private readonly ISyncRepository _syncRepo;

        public SyncJobProcessor(ILibraryManager libraryManager, ISyncRepository syncRepo)
        {
            _libraryManager = libraryManager;
            _syncRepo = syncRepo;
        }

        public void ProcessJobItem(SyncJob job, SyncJobItem jobItem, SyncTarget target)
        {

        }

        public async Task EnsureJobItems(SyncJob job)
        {
            var items = GetItemsForSync(job.RequestedItemIds)
                .ToList();

            var jobItems = _syncRepo.GetJobItems(job.Id)
                .ToList();

            var created = 0;

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
                    TargetId = job.TargetId
                };

                await _syncRepo.Create(jobItem).ConfigureAwait(false);

                created++;
            }

            job.ItemCount = jobItems.Count + created;
            await _syncRepo.Update(job).ConfigureAwait(false);
        }

        public IEnumerable<BaseItem> GetItemsForSync(IEnumerable<string> itemIds)
        {
            return itemIds.SelectMany(GetItemsForSync).DistinctBy(i => i.Id);
        }

        private IEnumerable<BaseItem> GetItemsForSync(string id)
        {
            var item = _libraryManager.GetItemById(id);

            if (item == null)
            {
                return new List<BaseItem>();
            }

            return GetItemsForSync(item);
        }

        private IEnumerable<BaseItem> GetItemsForSync(BaseItem item)
        {
            return new[] { item };
        }
    }
}
