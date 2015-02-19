using MediaBrowser.Common.Progress;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Sync;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Sync;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Sync
{
    public class MediaSync
    {
        private readonly ISyncManager _syncManager;
        private readonly IServerApplicationHost _appHost;
        private readonly ILogger _logger;

        public MediaSync(ILogger logger, ISyncManager syncManager, IServerApplicationHost appHost)
        {
            _logger = logger;
            _syncManager = syncManager;
            _appHost = appHost;
        }

        public async Task Sync(IServerSyncProvider provider, 
            SyncTarget target,
            IProgress<double> progress,
            CancellationToken cancellationToken)
        {
            var serverId = _appHost.SystemId;

            await SyncData(provider, serverId, target, cancellationToken).ConfigureAwait(false);
            progress.Report(2);

            // Do the data sync twice so the server knows what was removed from the device
            await SyncData(provider, serverId, target, cancellationToken).ConfigureAwait(false);
            progress.Report(3);

            var innerProgress = new ActionableProgress<double>();
            innerProgress.RegisterAction(pct =>
            {
                var totalProgress = pct * .97;
                totalProgress += 1;
                progress.Report(totalProgress);
            });
            await GetNewMedia(provider, target, serverId, innerProgress, cancellationToken);
            progress.Report(100);
        }

        private async Task SyncData(IServerSyncProvider provider,
            string serverId,
            SyncTarget target,
            CancellationToken cancellationToken)
        {
            var localIds = await provider.GetServerItemIds(serverId, target, cancellationToken).ConfigureAwait(false);

            var result = await _syncManager.SyncData(new SyncDataRequest
            {
                TargetId = target.Id,
                LocalItemIds = localIds

            }).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            foreach (var itemIdToRemove in result.ItemIdsToRemove)
            {
                try
                {
                    await RemoveItem(provider, serverId, itemIdToRemove, target, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error deleting item from sync target. Id: {0}", ex, itemIdToRemove);
                }
            }
        }

        private async Task GetNewMedia(IServerSyncProvider provider,
            SyncTarget target,
            string serverId,
            IProgress<double> progress,
            CancellationToken cancellationToken)
        {
            var jobItems =  await _syncManager.GetReadySyncItems(target.Id).ConfigureAwait(false);
            
            var numComplete = 0;
            double startingPercent = 0;
            double percentPerItem = 1;
            if (jobItems.Count > 0)
            {
                percentPerItem /= jobItems.Count;
            }

            foreach (var jobItem in jobItems)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var currentPercent = startingPercent;
                var innerProgress = new ActionableProgress<double>();
                innerProgress.RegisterAction(pct =>
                {
                    var totalProgress = pct * percentPerItem;
                    totalProgress += currentPercent;
                    progress.Report(totalProgress);
                });

                await GetItem(provider, target, serverId, jobItem, innerProgress, cancellationToken).ConfigureAwait(false);

                numComplete++;
                startingPercent = numComplete;
                startingPercent /= jobItems.Count;
                startingPercent *= 100;
                progress.Report(startingPercent);
            }
        }

        private async Task GetItem(IServerSyncProvider provider,
            SyncTarget target,
            string serverId,
            SyncedItem jobItem,
            IProgress<double> progress,
            CancellationToken cancellationToken)
        {
            var libraryItem = jobItem.Item;
            var internalSyncJobItem = _syncManager.GetJobItem(jobItem.SyncJobItemId);

            var fileTransferProgress = new ActionableProgress<double>();
            fileTransferProgress.RegisterAction(pct => progress.Report(pct * .92));

            await _syncManager.ReportSyncJobItemTransferBeginning(internalSyncJobItem.Id);

            var transferSuccess = false;
            Exception transferException = null;

            try
            {
                //await provider.TransferItemFile(serverId, libraryItem.Id, internalSyncJobItem.OutputPath, target, cancellationToken)
                //        .ConfigureAwait(false);

                progress.Report(92);

                transferSuccess = true;

                progress.Report(99);
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error transferring sync job file", ex);
                transferException = ex;
            }

            if (transferSuccess)
            {
                await _syncManager.ReportSyncJobItemTransferred(jobItem.SyncJobItemId).ConfigureAwait(false);
            }
            else
            {
                await _syncManager.ReportSyncJobItemTransferFailed(jobItem.SyncJobItemId).ConfigureAwait(false);

                throw transferException;
            }
        }

        private Task RemoveItem(IServerSyncProvider provider,
            string serverId,
            string itemId,
            SyncTarget target,
            CancellationToken cancellationToken)
        {
            return provider.DeleteItem(serverId, itemId, target, cancellationToken);
        }
    }
}
