using MediaBrowser.Common.Extensions;
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

                        var localItems = await dataProvider.GetItems(syncTarget, serverId, item.Id.ToString("N")).ConfigureAwait(false);

                        foreach (var localItem in localItems)
                        {
                            foreach (var mediaSource in localItem.Item.MediaSources)
                            {
                                AddMediaSource(list, localItem, mediaSource, syncProvider, syncTarget);
                            }
                        }
                    }
                }
            }

            return list;
        }

        private void AddMediaSource(List<MediaSourceInfo> list,
            LocalItem item,
            MediaSourceInfo mediaSource,
            IServerSyncProvider provider,
            SyncTarget target)
        {
            SetStaticMediaSourceInfo(item, mediaSource);

            var requiresDynamicAccess = provider as IHasDynamicAccess;

            if (requiresDynamicAccess != null)
            {
                mediaSource.RequiresOpening = true;

                var keyList = new List<string>();
                keyList.Add(provider.GetType().FullName.GetMD5().ToString("N"));
                keyList.Add(target.Id.GetMD5().ToString("N"));
                keyList.Add(item.Id);
                mediaSource.OpenToken = string.Join(StreamIdDelimeterString, keyList.ToArray());
            }

            list.Add(mediaSource);
        }

        // Do not use a pipe here because Roku http requests to the server will fail, without any explicit error message.
        private const string StreamIdDelimeterString = "_";

        public async Task<MediaSourceInfo> OpenMediaSource(string openToken, CancellationToken cancellationToken)
        {
            var openKeys = openToken.Split(new[] { StreamIdDelimeterString[0] }, 3);

            var provider = _syncManager.ServerSyncProviders
                .FirstOrDefault(i => string.Equals(openKeys[0], i.GetType().FullName.GetMD5().ToString("N"), StringComparison.OrdinalIgnoreCase));

            var target = provider.GetAllSyncTargets()
                .FirstOrDefault(i => string.Equals(openKeys[1], i.Id.GetMD5().ToString("N"), StringComparison.OrdinalIgnoreCase));

            var dataProvider = _syncManager.GetDataProvider(provider, target);
            var localItem = await dataProvider.Get(target, openKeys[2]).ConfigureAwait(false);

            var fileId = localItem.FileId;
            if (string.IsNullOrWhiteSpace(fileId))
            {
            }

            var requiresDynamicAccess = (IHasDynamicAccess)provider;
            var dynamicInfo = await requiresDynamicAccess.GetSyncedFileInfo(fileId, target, cancellationToken).ConfigureAwait(false);

            var mediaSource = localItem.Item.MediaSources.First();
            mediaSource.LiveStreamId = Guid.NewGuid().ToString();
            SetStaticMediaSourceInfo(localItem, mediaSource);

            foreach (var stream in mediaSource.MediaStreams)
            {
                if (!string.IsNullOrWhiteSpace(stream.ExternalId))
                {
                    var dynamicStreamInfo = await requiresDynamicAccess.GetSyncedFileInfo(stream.ExternalId, target, cancellationToken).ConfigureAwait(false);
                    stream.Path = dynamicStreamInfo.Path;
                }
            }

            mediaSource.Path = dynamicInfo.Path;
            mediaSource.Protocol = dynamicInfo.Protocol;
            mediaSource.RequiredHttpHeaders = dynamicInfo.RequiredHttpHeaders;

            return mediaSource;
        }

        private void SetStaticMediaSourceInfo(LocalItem item, MediaSourceInfo mediaSource)
        {
            mediaSource.Id = item.Id;
            mediaSource.SupportsTranscoding = false;
        }

        public Task CloseMediaSource(string liveStreamId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
