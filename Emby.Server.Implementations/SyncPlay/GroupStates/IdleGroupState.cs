using System.Threading;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.SyncPlay;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Controller.SyncPlay
{
    /// <summary>
    /// Class IdleGroupState.
    /// </summary>
    /// <remarks>
    /// Class is not thread-safe, external locking is required when accessing methods.
    /// </remarks>
    public class IdleGroupState : AbstractGroupState
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public IdleGroupState(ILogger logger) : base(logger)
        {
            // Do nothing
        }

        /// <inheritdoc />
        public override GroupState GetGroupState()
        {
            return GroupState.Idle;
        }

        /// <inheritdoc />
        public override void SessionJoined(ISyncPlayStateContext context, GroupState prevState, SessionInfo session, CancellationToken cancellationToken)
        {
            SendStopCommand(context, GetGroupState(), session, cancellationToken);
        }

        /// <inheritdoc />
        public override void SessionLeaving(ISyncPlayStateContext context, GroupState prevState, SessionInfo session, CancellationToken cancellationToken)
        {
            // Do nothing
        }

        /// <inheritdoc />
        public override void HandleRequest(ISyncPlayStateContext context, GroupState prevState, PlayGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            // Change state
            var waitingState = new WaitingGroupState(_logger);
            context.SetState(waitingState);
            waitingState.HandleRequest(context, GetGroupState(), request, session, cancellationToken);
        }

        /// <inheritdoc />
        public override void HandleRequest(ISyncPlayStateContext context, GroupState prevState, UnpauseGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            // Change state
            var waitingState = new WaitingGroupState(_logger);
            context.SetState(waitingState);
            waitingState.HandleRequest(context, GetGroupState(), request, session, cancellationToken);
        }

        /// <inheritdoc />
        public override void HandleRequest(ISyncPlayStateContext context, GroupState prevState, PauseGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            SendStopCommand(context, prevState, session, cancellationToken);
        }

        /// <inheritdoc />
        public override void HandleRequest(ISyncPlayStateContext context, GroupState prevState, StopGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            SendStopCommand(context, prevState, session, cancellationToken);
        }

        /// <inheritdoc />
        public override void HandleRequest(ISyncPlayStateContext context, GroupState prevState, SeekGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            SendStopCommand(context, prevState, session, cancellationToken);
        }

        /// <inheritdoc />
        public override void HandleRequest(ISyncPlayStateContext context, GroupState prevState, BufferGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            SendStopCommand(context, prevState, session, cancellationToken);
        }

        /// <inheritdoc />
        public override void HandleRequest(ISyncPlayStateContext context, GroupState prevState, ReadyGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            SendStopCommand(context, prevState, session, cancellationToken);
        }

        /// <inheritdoc />
        public override void HandleRequest(ISyncPlayStateContext context, GroupState prevState, NextTrackGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            // Change state
            var waitingState = new WaitingGroupState(_logger);
            context.SetState(waitingState);
            waitingState.HandleRequest(context, GetGroupState(), request, session, cancellationToken);
        }

        /// <inheritdoc />
        public override void HandleRequest(ISyncPlayStateContext context, GroupState prevState, PreviousTrackGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            // Change state
            var waitingState = new WaitingGroupState(_logger);
            context.SetState(waitingState);
            waitingState.HandleRequest(context, GetGroupState(), request, session, cancellationToken);
        }

        private void SendStopCommand(ISyncPlayStateContext context, GroupState prevState, SessionInfo session, CancellationToken cancellationToken)
        {
            var command = context.NewSyncPlayCommand(SendCommandType.Stop);
            if (!prevState.Equals(GetGroupState()))
            {
                context.SendCommand(session, SyncPlayBroadcastType.AllGroup, command, cancellationToken);
            }
            else
            {
                context.SendCommand(session, SyncPlayBroadcastType.CurrentSession, command, cancellationToken);
            }
        }
    }
}
