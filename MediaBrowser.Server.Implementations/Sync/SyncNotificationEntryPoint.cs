using System.Threading;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Session;
using MediaBrowser.Controller.Sync;
using MediaBrowser.Model.Events;
using MediaBrowser.Model.Sync;

namespace MediaBrowser.Server.Implementations.Sync
{
    public class SyncNotificationEntryPoint : IServerEntryPoint
    {
        private readonly ISessionManager _sessionManager;
        private readonly ISyncManager _syncManager;

        public SyncNotificationEntryPoint(ISyncManager syncManager, ISessionManager sessionManager)
        {
            _syncManager = syncManager;
            _sessionManager = sessionManager;
        }

        public void Run()
        {
            _syncManager.SyncJobItemUpdated += _syncManager_SyncJobItemUpdated;
        }

        private async void _syncManager_SyncJobItemUpdated(object sender, GenericEventArgs<SyncJobItem> e)
        {
            var item = e.Argument;

            if (item.Status == SyncJobItemStatus.ReadyToTransfer)
            {
                try
                {
                    await _sessionManager.SendMessageToUserDeviceSessions(item.TargetId, "SyncJobItemReady", item, CancellationToken.None).ConfigureAwait(false);
                }
                catch
                {

                }
            }
        }

        public void Dispose()
        {
            _syncManager.SyncJobItemUpdated -= _syncManager_SyncJobItemUpdated;
        }
    }
}
