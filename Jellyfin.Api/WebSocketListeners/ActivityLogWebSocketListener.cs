using System;
using System.Threading.Tasks;
using Jellyfin.Data.Events;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Activity;
using MediaBrowser.Model.Session;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Api.WebSocketListeners
{
    /// <summary>
    /// Class SessionInfoWebSocketListener.
    /// </summary>
    public class ActivityLogWebSocketListener : BasePeriodicWebSocketListener<ActivityLogEntry[], WebSocketListenerState>
    {
        /// <summary>
        /// The _kernel.
        /// </summary>
        private readonly IActivityManager _activityManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActivityLogWebSocketListener"/> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger{ActivityLogWebSocketListener}"/> interface.</param>
        /// <param name="activityManager">Instance of the <see cref="IActivityManager"/> interface.</param>
        public ActivityLogWebSocketListener(ILogger<ActivityLogWebSocketListener> logger, IActivityManager activityManager)
            : base(logger)
        {
            _activityManager = activityManager;
            _activityManager.EntryCreated += OnEntryCreated;
        }

        /// <inheritdoc />
        protected override SessionMessageType Type => SessionMessageType.ActivityLogEntry;

        /// <inheritdoc />
        protected override SessionMessageType StartType => SessionMessageType.ActivityLogEntryStart;

        /// <inheritdoc />
        protected override SessionMessageType StopType => SessionMessageType.ActivityLogEntryStop;

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

        private void OnEntryCreated(object? sender, GenericEventArgs<ActivityLogEntry> e)
        {
            SendData(true).GetAwaiter().GetResult();
        }
    }
}
