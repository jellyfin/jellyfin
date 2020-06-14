using System;
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
    public class ActivityLogWebSocketListener : BasePeriodicWebSocketListener<ActivityLogEntry[], WebSocketListenerState>
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

        public ActivityLogWebSocketListener(ILogger<ActivityLogWebSocketListener> logger, IActivityManager activityManager) : base(logger)
        {
            _activityManager = activityManager;
            _activityManager.EntryCreated += OnEntryCreated;
        }

        private void OnEntryCreated(object sender, GenericEventArgs<ActivityLogEntry> e)
        {
            SendData(true);
        }

        /// <summary>
        /// Gets the data to send.
        /// </summary>
        /// <returns>Task{SystemInfo}.</returns>
        protected override Task<ActivityLogEntry[]> GetDataToSend()
        {
            return Task.FromResult(Array.Empty<ActivityLogEntry>());
        }

        /// <inheritdoc />
        protected override void Dispose(bool dispose)
        {
            _activityManager.EntryCreated -= OnEntryCreated;

            base.Dispose(dispose);
        }
    }
}
