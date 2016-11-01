using MediaBrowser.Controller.Sync;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Sync;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediaBrowser.Model.Threading;

namespace MediaBrowser.Api.Sync
{
    /// <summary>
    /// Class SessionInfoWebSocketListener
    /// </summary>
    class SyncJobsWebSocketListener : BasePeriodicWebSocketListener<IEnumerable<SyncJob>, WebSocketListenerState>
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        protected override string Name
        {
            get { return "SyncJobs"; }
        }

        private readonly ISyncManager _syncManager;
        private string _userId;
        private string _targetId;

        public SyncJobsWebSocketListener(ILogger logger, ISyncManager syncManager, ITimerFactory timerFactory)
            : base(logger, timerFactory)
        {
            _syncManager = syncManager;
            _syncManager.SyncJobCancelled += _syncManager_SyncJobCancelled;
            _syncManager.SyncJobCreated += _syncManager_SyncJobCreated;
            _syncManager.SyncJobUpdated += _syncManager_SyncJobUpdated;
        }

        protected override void ParseMessageParams(string[] values)
        {
            base.ParseMessageParams(values);

            if (values.Length > 0)
            {
                _userId = values[0];
            }

            if (values.Length > 1)
            {
                _targetId = values[1];
            }
        }

        void _syncManager_SyncJobUpdated(object sender, Model.Events.GenericEventArgs<SyncJob> e)
        {
            SendData(false);
        }

        void _syncManager_SyncJobCreated(object sender, Model.Events.GenericEventArgs<SyncJobCreationResult> e)
        {
            SendData(true);
        }

        void _syncManager_SyncJobCancelled(object sender, Model.Events.GenericEventArgs<SyncJob> e)
        {
            SendData(true);
        }

        /// <summary>
        /// Gets the data to send.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <returns>Task{SystemInfo}.</returns>
        protected override async Task<IEnumerable<SyncJob>> GetDataToSend(WebSocketListenerState state)
        {
            var jobs = await _syncManager.GetJobs(new SyncJobQuery
            {
                UserId = _userId,
                TargetId = _targetId

            }).ConfigureAwait(false);

            return jobs.Items;
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
            _syncManager.SyncJobCreated -= _syncManager_SyncJobCreated;
            _syncManager.SyncJobUpdated -= _syncManager_SyncJobUpdated;

            base.Dispose(dispose);
        }
    }
}
