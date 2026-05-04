using System;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Session;

namespace Jellyfin.Server.Implementations.Events.Consumers.Updates
{
    public abstract class PluginNotificationConsumer<TEventArgs, TMessageData> : IEventConsumer<TEventArgs>
        where TEventArgs : EventArgs
    {
        private readonly ISessionManager _sessionManager;
        private readonly SessionMessageType _messageType;

        protected PluginNotificationConsumer(ISessionManager sessionManager, SessionMessageType messageType)
        {
            _sessionManager = sessionManager;
            _messageType = messageType;
        }

        public async Task OnEvent(TEventArgs eventArgs)
        {
            await _sessionManager.SendMessageToAdminSessions(_messageType, GetMessageData(eventArgs), CancellationToken.None).ConfigureAwait(false);
        }

        protected abstract TMessageData GetMessageData(TEventArgs eventArgs);
    }
}
