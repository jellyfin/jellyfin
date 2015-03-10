using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Sync;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Sync;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Sync
{
    public class SyncedMediaSourceProvider : IMediaSourceProvider
    {
        private readonly SyncManager _syncManager;
        private readonly IServerApplicationHost _appHost;

        public SyncedMediaSourceProvider(ISyncManager syncManager, IServerApplicationHost appHost)
        {
            _appHost = appHost;
            _syncManager = (SyncManager)syncManager;
        }

        public async Task<IEnumerable<MediaSourceInfo>> GetMediaSources(IHasMediaSources item, CancellationToken cancellationToken)
        {
            var jobItemResult = _syncManager.GetJobItems(new SyncJobItemQuery
            {
                AddMetadata = false,
                Statuses = new List<SyncJobItemStatus> { SyncJobItemStatus.Synced },
                ItemId = item.Id.ToString("N")
            });

            var list = new List<MediaSourceInfo>();

            if (jobItemResult.Items.Length > 0)
            {
                var targets = _syncManager.ServerSyncProviders
                    .SelectMany(i => i.GetAllSyncTargets().Select(t => new Tuple<IServerSyncProvider, SyncTarget>(i, t)))
                    .ToList();

                var serverId = _appHost.SystemId;

                foreach (var jobItem in jobItemResult.Items)
                {
                    var targetTuple = targets.FirstOrDefault(i => string.Equals(i.Item2.Id, jobItem.TargetId, StringComparison.OrdinalIgnoreCase));

                    if (targetTuple != null)
                    {
                        var syncTarget = targetTuple.Item2;

                        var dataProvider = _syncManager.GetDataProvider(targetTuple.Item1, syncTarget);

                        var localItems = await dataProvider.GetCachedItems(syncTarget, serverId, item.Id.ToString("N")).ConfigureAwait(false);

                        list.AddRange(localItems.SelectMany(i => i.Item.MediaSources));
                    }
                }
            }

            return list;
        }
    }
}
