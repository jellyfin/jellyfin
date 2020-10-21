using System;
using System.Threading;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.SyncPlay;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Controller.SyncPlay
{
    /// <summary>
    /// Class PlayingGroupState.
    /// </summary>
    /// <remarks>
    /// Class is not thread-safe, external locking is required when accessing methods.
    /// </remarks>
    public class PlayingGroupState : AbstractGroupState
    {
        /// <summary>
        /// Ignore requests for buffering.
        /// </summary>
        public bool IgnoreBuffering { get; set; }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public PlayingGroupState(ILogger logger) : base(logger)
        {
            // Do nothing.
        }

        /// <inheritdoc />
        public override GroupState GetGroupState()
        {
            return GroupState.Playing;
        }

        /// <inheritdoc />
        public override void SessionJoined(ISyncPlayStateContext context, GroupState prevState, SessionInfo session, CancellationToken cancellationToken)
        {
            // Wait for session to be ready.
            var waitingState = new WaitingGroupState(_logger);
            context.SetState(waitingState);
            waitingState.SessionJoined(context, GetGroupState(), session, cancellationToken);
        }

        /// <inheritdoc />
        public override void SessionLeaving(ISyncPlayStateContext context, GroupState prevState, SessionInfo session, CancellationToken cancellationToken)
        {
            // Do nothing.
        }

        /// <inheritdoc />
        public override void HandleRequest(ISyncPlayStateContext context, GroupState prevState, PlayGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            // Change state.
            var waitingState = new WaitingGroupState(_logger);
            context.SetState(waitingState);
            waitingState.HandleRequest(context, GetGroupState(), request, session, cancellationToken);
        }

        /// <inheritdoc />
        public override void HandleRequest(ISyncPlayStateContext context, GroupState prevState, UnpauseGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            if (!prevState.Equals(GetGroupState()))
            {
                // Pick a suitable time that accounts for latency.
                var delayMillis = Math.Max(context.GetHighestPing() * 2, context.DefaultPing);

                // Unpause group and set starting point in future.
                // Clients will start playback at LastActivity (datetime) from PositionTicks (playback position).
                // The added delay does not guarantee, of course, that the command will be received in time.
                // Playback synchronization will mainly happen client side.
                context.LastActivity = DateTime.UtcNow.AddMilliseconds(
                    delayMillis
                );

                var command = context.NewSyncPlayCommand(SendCommandType.Unpause);
                context.SendCommand(session, SyncPlayBroadcastType.AllGroup, command, cancellationToken);

                // Notify relevant state change event.
                SendGroupStateUpdate(context, request, session, cancellationToken);
            }
            else
            {
                // Client got lost, sending current state.
                var command = context.NewSyncPlayCommand(SendCommandType.Unpause);
                context.SendCommand(session, SyncPlayBroadcastType.CurrentSession, command, cancellationToken);
            }
        }

        /// <inheritdoc />
        public override void HandleRequest(ISyncPlayStateContext context, GroupState prevState, PauseGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            // Change state.
            var pausedState = new PausedGroupState(_logger);
            context.SetState(pausedState);
            pausedState.HandleRequest(context, GetGroupState(), request, session, cancellationToken);
        }

        /// <inheritdoc />
        public override void HandleRequest(ISyncPlayStateContext context, GroupState prevState, StopGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            // Change state.
            var idleState = new IdleGroupState(_logger);
            context.SetState(idleState);
            idleState.HandleRequest(context, GetGroupState(), request, session, cancellationToken);
        }

        /// <inheritdoc />
        public override void HandleRequest(ISyncPlayStateContext context, GroupState prevState, SeekGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            // Change state.
            var waitingState = new WaitingGroupState(_logger);
            context.SetState(waitingState);
            waitingState.HandleRequest(context, GetGroupState(), request, session, cancellationToken);
        }

        /// <inheritdoc />
        public override void HandleRequest(ISyncPlayStateContext context, GroupState prevState, BufferGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            if (IgnoreBuffering)
            {
                return;
            }

            // Change state.
            var waitingState = new WaitingGroupState(_logger);
            context.SetState(waitingState);
            waitingState.HandleRequest(context, GetGroupState(), request, session, cancellationToken);
        }

        /// <inheritdoc />
        public override void HandleRequest(ISyncPlayStateContext context, GroupState prevState, ReadyGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            if (prevState.Equals(GetGroupState()))
            {
                // Group was not waiting, make sure client has latest state.
                var command = context.NewSyncPlayCommand(SendCommandType.Unpause);
                context.SendCommand(session, SyncPlayBroadcastType.CurrentSession, command, cancellationToken);
            }
            else if (prevState.Equals(GroupState.Waiting))
            {
                // Notify relevant state change event.
                SendGroupStateUpdate(context, request, session, cancellationToken);
            }
        }

        /// <inheritdoc />
        public override void HandleRequest(ISyncPlayStateContext context, GroupState prevState, NextTrackGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            // Change state.
            var waitingState = new WaitingGroupState(_logger);
            context.SetState(waitingState);
            waitingState.HandleRequest(context, GetGroupState(), request, session, cancellationToken);
        }

        /// <inheritdoc />
        public override void HandleRequest(ISyncPlayStateContext context, GroupState prevState, PreviousTrackGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            // Change state.
            var waitingState = new WaitingGroupState(_logger);
            context.SetState(waitingState);
            waitingState.HandleRequest(context, GetGroupState(), request, session, cancellationToken);
        }
    }
}
