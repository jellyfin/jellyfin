using System;
using System.Threading.Tasks;
using Emby.Server.Implementations.Events.ConsumerArgs;
using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Activity;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.Events.Consumers.Activity
{
    /// <summary>
    /// Class SessionInfoWebSocketListener.
    /// </summary>
    public class ActivityLogWebSocketListener
        : BasePeriodicWebSocketListener<ActivityLogEntry[], WebSocketListenerState>,
            IEventConsumer<ActivityManagerEntryCreatedEventArgs>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ActivityLogWebSocketListener"/> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger{ActivityLogWebSocketListener}"/> interface.</param>
        public ActivityLogWebSocketListener(ILogger<ActivityLogWebSocketListener> logger)
            : base(logger)
        {
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        protected override string Name => "ActivityLogEntry";

        /// <inheritdoc />
        public Task OnEvent(ActivityManagerEntryCreatedEventArgs eventArgs)
        {
            SendData(true);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Gets the data to send.
        /// </summary>
        /// <returns>Task{SystemInfo}.</returns>
        protected override Task<ActivityLogEntry[]> GetDataToSend()
        {
            return Task.FromResult(Array.Empty<ActivityLogEntry>());
        }
    }
}
