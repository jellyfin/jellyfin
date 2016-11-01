using MediaBrowser.Controller.Sync;
using MediaBrowser.Model.Events;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Sync;
using System;
using System.Linq;
using System.Threading.Tasks;
using MediaBrowser.Model.Threading;

namespace MediaBrowser.Api.Sync
{
    /// <summary>
    /// Class SessionInfoWebSocketListener
    /// </summary>
    class SyncJobWebSocketListener : BasePeriodicWebSocketListener<CompleteSyncJobInfo, WebSocketListenerState>
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        protected override string Name
        {
            get { return "SyncJob"; }
        }

        private readonly ISyncManager _syncManager;
        private string _jobId;

        public SyncJobWebSocketListener(ILogger logger, ISyncManager syncManager, ITimerFactory timerFactory)
            : base(logger, timerFactory)
        {
            _syncManager = syncManager;
            _syncManager.SyncJobCancelled += _syncManager_SyncJobCancelled;
            _syncManager.SyncJobUpdated += _syncManager_SyncJobUpdated;
            _syncManager.SyncJobItemCreated += _syncManager_SyncJobItemCreated;
            _syncManager.SyncJobItemUpdated += _syncManager_SyncJobItemUpdated;
        }

        void _syncManager_SyncJobItemUpdated(object sender, GenericEventArgs<SyncJobItem> e)
        {
            if (string.Equals(e.Argument.Id, _jobId, StringComparison.Ordinal))
            {
                SendData(false);
            }
        }

        void _syncManager_SyncJobItemCreated(object sender, GenericEventArgs<SyncJobItem> e)
        {
            if (string.Equals(e.Argument.Id, _jobId, StringComparison.Ordinal))
            {
                SendData(true);
            }
        }

        protected override void ParseMessageParams(string[] values)
        {
            base.ParseMessageParams(values);

            if (values.Length > 0)
            {
                _jobId = values[0];
            }
        }

        void _syncManager_SyncJobUpdated(object sender, GenericEventArgs<SyncJob> e)
        {
            if (string.Equals(e.Argument.Id, _jobId, StringComparison.Ordinal))
            {
                SendData(false);
            }
        }

        void _syncManager_SyncJobCancelled(object sender, GenericEventArgs<SyncJob> e)
        {
            if (string.Equals(e.Argument.Id, _jobId, StringComparison.Ordinal))
            {
                SendData(true);
            }
        }

        /// <summary>
        /// Gets the data to send.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <returns>Task{SystemInfo}.</returns>
        protected override Task<CompleteSyncJobInfo> GetDataToSend(WebSocketListenerState state)
        {
            var job = _syncManager.GetJob(_jobId);
            var items = _syncManager.GetJobItems(new SyncJobItemQuery
            {
                AddMetadata = true,
                JobId = _jobId
            });

            var info = new CompleteSyncJobInfo
            {
                Job = job,
                JobItems = items.Items.ToList()
            };

            return Task.FromResult(info);
        }

        protected override bool SendOnTimer
        {
            get
            {
                return false;
            }
        }

        protected override void Dispose(bool dispose)
        {
            _syncManager.SyncJobCancelled -= _syncManager_SyncJobCancelled;
            _syncManager.SyncJobUpdated -= _syncManager_SyncJobUpdated;

            base.Dispose(dispose);
        }
    }
}
