#nullable enable

using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Events.Session;
using MediaBrowser.Controller.SyncPlay;
using MediaBrowser.Controller.SyncPlay.Requests;
using Rebus.Handlers;

namespace Emby.Server.Implementations.SyncPlay
{
    /// <summary>
    /// Handles ensuring new sessions are mapped to the appropriate SyncPlay groups.
    /// </summary>
    public class SyncPlaySessionConnectedHandler : IHandleMessages<SessionControllerConnectedEventArgs>
    {
        private readonly ISyncPlayManager _syncPlayManager;

        /// <summary>
        ///Initializes a new instance of the <see cref="SyncPlaySessionConnectedHandler"/> class.
        /// </summary>
        /// <param name="syncPlayManager">The SyncPlay manager.</param>
        public SyncPlaySessionConnectedHandler(ISyncPlayManager syncPlayManager)
        {
            _syncPlayManager = syncPlayManager;
        }

        /// <inheritdoc />
        public Task Handle(SessionControllerConnectedEventArgs e)
        {
            var session = e.Argument;
            var group = _syncPlayManager.GetGroupForSession(session.Id);

            if (group != null)
            {
                _syncPlayManager.JoinGroup(session, new JoinGroupRequest(group.GroupId), CancellationToken.None);
            }

            return Task.CompletedTask;
        }
    }
}
