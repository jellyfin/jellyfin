using System;
using System.Threading;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.SyncPlay;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Controller.SyncPlay
{
    /// <summary>
    /// Class PausedGroupState.
    /// </summary>
    /// <remarks>
    /// Class is not thread-safe, external locking is required when accessing methods.
    /// </remarks>
    public class PausedGroupState : AbstractGroupState
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public PausedGroupState(ILogger logger) : base(logger)
        {
            // Do nothing.
        }

        /// <inheritdoc />
        public override GroupState GetGroupState()
        {
            return GroupState.Paused;
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
            // Change state.
            var playingState = new PlayingGroupState(_logger);
            context.SetState(playingState);
            playingState.HandleRequest(context, GetGroupState(), request, session, cancellationToken);
        }

        /// <inheritdoc />
        public override void HandleRequest(ISyncPlayStateContext context, GroupState prevState, PauseGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            if (!prevState.Equals(GetGroupState()))
            {
                // Pause group and compute the media playback position.
                var currentTime = DateTime.UtcNow;
                var elapsedTime = currentTime - context.LastActivity;
                context.LastActivity = currentTime;
                // Seek only if playback actually started.
                // Pause request may be issued during the delay added to account for latency.
                context.PositionTicks += Math.Max(elapsedTime.Ticks, 0);

                var command = context.NewSyncPlayCommand(SendCommandType.Pause);
                context.SendCommand(session, SyncPlayBroadcastType.AllGroup, command, cancellationToken);

                // Notify relevant state change event.
                SendGroupStateUpdate(context, request, session, cancellationToken);
            }
            else
            {
                // Client got lost, sending current state.
                var command = context.NewSyncPlayCommand(SendCommandType.Pause);
                context.SendCommand(session, SyncPlayBroadcastType.CurrentSession, command, cancellationToken);
            }
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
                // Client got lost, sending current state.
                var command = context.NewSyncPlayCommand(SendCommandType.Pause);
                context.SendCommand(session, SyncPlayBroadcastType.CurrentSession, command, cancellationToken);
            }
            else if (prevState.Equals(GroupState.Waiting))
            {
                // Sending current state to all clients.
                var command = context.NewSyncPlayCommand(SendCommandType.Pause);
                context.SendCommand(session, SyncPlayBroadcastType.AllGroup, command, cancellationToken);

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
