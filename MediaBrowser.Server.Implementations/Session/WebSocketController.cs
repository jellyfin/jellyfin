using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Session;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Session
{
    public class WebSocketController : ISessionRemoteController
    {
        private readonly ILogger _logger;

        public WebSocketController(ILogger logger)
        {
            _logger = logger;
        }

        public bool Supports(SessionInfo session)
        {
            return session.WebSockets.Any(i => i.State == WebSocketState.Open);
        }

        public async Task SendSystemCommand(SessionInfo session, SystemCommand command, CancellationToken cancellationToken)
        {
            var socket = session.WebSockets.OrderByDescending(i => i.LastActivityDate).FirstOrDefault(i => i.State == WebSocketState.Open);

            if (socket != null)
            {
                try
                {
                    await socket.SendAsync(new WebSocketMessage<string>
                    {
                        MessageType = "SystemCommand",
                        Data = command.ToString()

                    }, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error sending web socket message", ex);
                }
            }
            else
            {
                throw new InvalidOperationException("The requested session does not have an open web socket.");
            }
        }
    }
}
