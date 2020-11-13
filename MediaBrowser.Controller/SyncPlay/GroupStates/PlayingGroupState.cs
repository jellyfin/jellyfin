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
        /// Initializes a new instance of the <see cref="PlayingGroupState"/> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger"/> interface.</param>
        public PlayingGroupState(ILogger logger)
            : base(logger)
        {
            // Do nothing.
        }

        /// <inheritdoc />
        public override GroupStateType Type
        {
            get
            {
                return GroupStateType.Playing;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether requests for buffering should be ignored.
        /// </summary>
        public bool IgnoreBuffering { get; set; }

        /// <inheritdoc />
        public override void SessionJoined(IGroupStateContext context, GroupStateType prevState, SessionInfo session, CancellationToken cancellationToken)
        {
            // Wait for session to be ready.
            var waitingState = new WaitingGroupState(Logger);
            context.SetState(waitingState);
            waitingState.SessionJoined(context, Type, session, cancellationToken);
        }

        /// <inheritdoc />
        public override void SessionLeaving(IGroupStateContext context, GroupStateType prevState, SessionInfo session, CancellationToken cancellationToken)
        {
            // Do nothing.
        }

        /// <inheritdoc />
        public override void HandleRequest(IGroupStateContext context, GroupStateType prevState, PlayGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            // Change state.
            var waitingState = new WaitingGroupState(Logger);
            context.SetState(waitingState);
            waitingState.HandleRequest(context, Type, request, session, cancellationToken);
        }

        /// <inheritdoc />
        public override void HandleRequest(IGroupStateContext context, GroupStateType prevState, UnpauseGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            if (!prevState.Equals(Type))
            {
                // Pick a suitable time that accounts for latency.
                var delayMillis = Math.Max(context.GetHighestPing() * 2, context.DefaultPing);

                // Unpause group and set starting point in future.
                // Clients will start playback at LastActivity (datetime) from PositionTicks (playback position).
                // The added delay does not guarantee, of course, that the command will be received in time.
                // Playback synchronization will mainly happen client side.
                context.LastActivity = DateTime.UtcNow.AddMilliseconds(delayMillis);

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
        public override void HandleRequest(IGroupStateContext context, GroupStateType prevState, PauseGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            // Change state.
            var pausedState = new PausedGroupState(Logger);
            context.SetState(pausedState);
            pausedState.HandleRequest(context, Type, request, session, cancellationToken);
        }

        /// <inheritdoc />
        public override void HandleRequest(IGroupStateContext context, GroupStateType prevState, StopGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            // Change state.
            var idleState = new IdleGroupState(Logger);
            context.SetState(idleState);
            idleState.HandleRequest(context, Type, request, session, cancellationToken);
        }

        /// <inheritdoc />
        public override void HandleRequest(IGroupStateContext context, GroupStateType prevState, SeekGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            // Change state.
            var waitingState = new WaitingGroupState(Logger);
            context.SetState(waitingState);
            waitingState.HandleRequest(context, Type, request, session, cancellationToken);
        }

        /// <inheritdoc />
        public override void HandleRequest(IGroupStateContext context, GroupStateType prevState, BufferGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            if (IgnoreBuffering)
            {
                return;
            }

            // Change state.
            var waitingState = new WaitingGroupState(Logger);
            context.SetState(waitingState);
            waitingState.HandleRequest(context, Type, request, session, cancellationToken);
        }

        /// <inheritdoc />
        public override void HandleRequest(IGroupStateContext context, GroupStateType prevState, ReadyGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            if (prevState.Equals(Type))
            {
                // Group was not waiting, make sure client has latest state.
                var command = context.NewSyncPlayCommand(SendCommandType.Unpause);
                context.SendCommand(session, SyncPlayBroadcastType.CurrentSession, command, cancellationToken);
            }
            else if (prevState.Equals(GroupStateType.Waiting))
            {
                // Notify relevant state change event.
                SendGroupStateUpdate(context, request, session, cancellationToken);
            }
        }

        /// <inheritdoc />
        public override void HandleRequest(IGroupStateContext context, GroupStateType prevState, NextTrackGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            // Change state.
            var waitingState = new WaitingGroupState(Logger);
            context.SetState(waitingState);
            waitingState.HandleRequest(context, Type, request, session, cancellationToken);
        }

        /// <inheritdoc />
        public override void HandleRequest(IGroupStateContext context, GroupStateType prevState, PreviousTrackGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            // Change state.
            var waitingState = new WaitingGroupState(Logger);
            context.SetState(waitingState);
            waitingState.HandleRequest(context, Type, request, session, cancellationToken);
        }
    }
}
