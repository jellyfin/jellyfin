using System.Collections.Generic;
using System.Threading.Tasks;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Activity;
using MediaBrowser.Model.Events;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Api.System
{
    /// <summary>
    /// Class SessionInfoWebSocketListener
    /// </summary>
    public class ActivityLogWebSocketListener : BasePeriodicWebSocketListener<List<ActivityLogEntry>, WebSocketListenerState>
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        protected override string Name => "ActivityLogEntry";

        /// <summary>
        /// The _kernel
        /// </summary>
        private readonly IActivityManager _activityManager;

        public ActivityLogWebSocketListener(ILogger logger, IActivityManager activityManager) : base(logger)
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
        /// <returns>Task{SystemInfo}.</returns>
        protected override Task<List<ActivityLogEntry>> GetDataToSend()
        {
            return Task.FromResult(new List<ActivityLogEntry>());
        }


        protected override void Dispose(bool dispose)
        {
            _activityManager.EntryCreated -= _activityManager_EntryCreated;

            base.Dispose(dispose);
        }
    }
}
