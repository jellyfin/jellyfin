using System.Linq;
using System;
using System.Threading;
using System.Collections.Generic;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Session;
using MediaBrowser.Model.SyncPlay;

namespace MediaBrowser.Controller.SyncPlay
{
    /// <summary>
    /// Class PlayingGroupState.
    /// </summary>
    /// <remarks>
    /// Class is not thread-safe, external locking is required when accessing methods.
    /// </remarks>
    public class PlayingGroupState : SyncPlayAbstractState
    {
        /// <inheritdoc />
        public override GroupState GetGroupState()
        {
            return GroupState.Playing;
        }

        /// <inheritdoc />
        public override bool HandleRequest(ISyncPlayStateContext context, bool newState, PlayGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            GroupInfo group = context.GetGroup();

            if (newState)
            {
                // Pick a suitable time that accounts for latency
                var delay = Math.Max(group.GetHighestPing() * 2, group.DefaultPing);

                // Unpause group and set starting point in future
                // Clients will start playback at LastActivity (datetime) from PositionTicks (playback position)
                // The added delay does not guarantee, of course, that the command will be received in time
                // Playback synchronization will mainly happen client side
                group.LastActivity = DateTime.UtcNow.AddMilliseconds(
                    delay
                );

                var command = context.NewSyncPlayCommand(SendCommandType.Play);
                context.SendCommand(session, SyncPlayBroadcastType.AllGroup, command, cancellationToken);
            }
            else
            {
                // Client got lost, sending current state
                var command = context.NewSyncPlayCommand(SendCommandType.Play);
                context.SendCommand(session, SyncPlayBroadcastType.CurrentSession, command, cancellationToken);
            }

            return true;
        }

        /// <inheritdoc />
        public override bool HandleRequest(ISyncPlayStateContext context, bool newState, PauseGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            // Change state
            var pausedState = new PausedGroupState();
            context.SetState(pausedState);
            return pausedState.HandleRequest(context, true, request, session, cancellationToken);
        }

        /// <inheritdoc />
        public override bool HandleRequest(ISyncPlayStateContext context, bool newState, SeekGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            // Change state
            var pausedState = new PausedGroupState();
            context.SetState(pausedState);
            return pausedState.HandleRequest(context, true, request, session, cancellationToken);
        }

        /// <inheritdoc />
        public override bool HandleRequest(ISyncPlayStateContext context, bool newState, BufferGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            // Change state
            var pausedState = new PausedGroupState();
            context.SetState(pausedState);
            return pausedState.HandleRequest(context, true, request, session, cancellationToken);
        }

        /// <inheritdoc />
        public override bool HandleRequest(ISyncPlayStateContext context, bool newState, ReadyGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            // Group was not waiting, make sure client has latest state
            var command = context.NewSyncPlayCommand(SendCommandType.Play);
            context.SendCommand(session, SyncPlayBroadcastType.CurrentSession, command, cancellationToken);

            return true;
        }
    }
}
