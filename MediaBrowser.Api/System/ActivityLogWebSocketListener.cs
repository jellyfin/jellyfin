using MediaBrowser.Model.Activity;
using MediaBrowser.Model.Events;
using MediaBrowser.Model.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediaBrowser.Model.Threading;

namespace MediaBrowser.Api.System
{
    /// <summary>
    /// Class SessionInfoWebSocketListener
    /// </summary>
    class ActivityLogWebSocketListener : BasePeriodicWebSocketListener<List<ActivityLogEntry>, WebSocketListenerState>
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        protected override string Name
        {
            get { return "ActivityLogEntry"; }
        }

        /// <summary>
        /// The _kernel
        /// </summary>
        private readonly IActivityManager _activityManager;

        public ActivityLogWebSocketListener(ILogger logger, ITimerFactory timerFactory, IActivityManager activityManager) : base(logger, timerFactory)
        {
            _activityManager = activityManager;
            _activityManager.EntryCreated += _activityManager_EntryCreated;
        }

        void _activityManager_EntryCreated(object sender, GenericEventArgs<ActivityLogEntry> e)
        {
            SendData(true);
        }

        /// <summary>
        /// Gets the data to send.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <returns>Task{SystemInfo}.</returns>
        protected override Task<List<ActivityLogEntry>> GetDataToSend(WebSocketListenerState state)
        {
            return Task.FromResult(new List<ActivityLogEntry>());
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
            _activityManager.EntryCreated -= _activityManager_EntryCreated;

            base.Dispose(dispose);
        }
    }
}
