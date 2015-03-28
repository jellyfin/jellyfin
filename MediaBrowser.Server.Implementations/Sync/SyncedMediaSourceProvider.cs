using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Sync;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Logging;
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
        private readonly ILogger _logger;

        public SyncedMediaSourceProvider(ISyncManager syncManager, IServerApplicationHost appHost, ILogger logger)
        {
            _appHost = appHost;
            _logger = logger;
            _syncManager = (SyncManager)syncManager;
        }

        public async Task<IEnumerable<MediaSourceInfo>> GetMediaSources(IHasMediaSources item, CancellationToken cancellationToken)
        {
            var jobItemResult = _syncManager.GetJobItems(new SyncJobItemQuery
            {
                AddMetadata = false,
                Statuses = new[] { SyncJobItemStatus.Synced },
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
                        var syncProvider = targetTuple.Item1;
                        var dataProvider = _syncManager.GetDataProvider(targetTuple.Item1, syncTarget);

                        var localItems = await dataProvider.GetCachedItems(syncTarget, serverId, item.Id.ToString("N")).ConfigureAwait(false);

                        foreach (var localItem in localItems)
                        {
                            foreach (var mediaSource in localItem.Item.MediaSources)
                            {
                                await TryAddMediaSource(list, localItem, mediaSource, syncProvider, syncTarget, cancellationToken).ConfigureAwait(false);
                            }
                        }
                    }
                }
            }

            return list;
        }

        private async Task TryAddMediaSource(List<MediaSourceInfo> list,
            LocalItem item,
            MediaSourceInfo mediaSource,
            IServerSyncProvider provider,
            SyncTarget target,
            CancellationToken cancellationToken)
        {
            var requiresDynamicAccess = provider as IHasDynamicAccess;

            if (requiresDynamicAccess == null)
            {
                list.Add(mediaSource);
                return;
            }

            try
            {
                var dynamicInfo = await requiresDynamicAccess.GetSyncedFileInfo(item.LocalPath, target, cancellationToken).ConfigureAwait(false);

                foreach (var stream in mediaSource.MediaStreams)
                {
                    var dynamicStreamInfo = await requiresDynamicAccess.GetSyncedFileInfo(stream.ExternalId, target, cancellationToken).ConfigureAwait(false);

                    stream.Path = dynamicStreamInfo.Path;
                }

                mediaSource.Path = dynamicInfo.Path;
                mediaSource.Protocol = dynamicInfo.Protocol;

                list.Add(mediaSource);
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error getting dynamic media source info", ex);
            }
        }
    }
}
