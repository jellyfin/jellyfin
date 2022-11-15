#nullable disable

using System.Threading;
using MediaBrowser.Controller.Session;
using MediaBrowser.Controller.SyncPlay.PlaybackRequests;
using MediaBrowser.Model.SyncPlay;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Controller.SyncPlay.GroupStates
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
        /// The logger.
        /// </summary>
        private readonly ILogger<IdleGroupState> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="IdleGroupState"/> class.
        /// </summary>
        /// <param name="loggerFactory">Instance of the <see cref="ILoggerFactory"/> interface.</param>
        public IdleGroupState(ILoggerFactory loggerFactory)
            : base(loggerFactory)
        {
            _logger = LoggerFactory.CreateLogger<IdleGroupState>();
        }

        /// <inheritdoc />
        public override GroupStateType Type { get; } = GroupStateType.Idle;

        /// <inheritdoc />
        public override void SessionJoined(IGroupStateContext context, GroupStateType prevState, SessionInfo session, CancellationToken cancellationToken)
        {
            SendStopCommand(context, Type, session, cancellationToken);
        }

        /// <inheritdoc />
        public override void SessionLeaving(IGroupStateContext context, GroupStateType prevState, SessionInfo session, CancellationToken cancellationToken)
        {
            // Do nothing.
        }

        /// <inheritdoc />
        public override void HandleRequest(PlayGroupRequest request, IGroupStateContext context, GroupStateType prevState, SessionInfo session, CancellationToken cancellationToken)
        {
            // Change state.
            var waitingState = new WaitingGroupState(LoggerFactory);
            context.SetState(waitingState);
            waitingState.HandleRequest(request, context, Type, session, cancellationToken);
        }

        /// <inheritdoc />
        public override void HandleRequest(UnpauseGroupRequest request, IGroupStateContext context, GroupStateType prevState, SessionInfo session, CancellationToken cancellationToken)
        {
            // Change state.
            var waitingState = new WaitingGroupState(LoggerFactory);
            context.SetState(waitingState);
            waitingState.HandleRequest(request, context, Type, session, cancellationToken);
        }

        /// <inheritdoc />
        public override void HandleRequest(PauseGroupRequest request, IGroupStateContext context, GroupStateType prevState, SessionInfo session, CancellationToken cancellationToken)
        {
            SendStopCommand(context, prevState, session, cancellationToken);
        }

        /// <inheritdoc />
        public override void HandleRequest(StopGroupRequest request, IGroupStateContext context, GroupStateType prevState, SessionInfo session, CancellationToken cancellationToken)
        {
            SendStopCommand(context, prevState, session, cancellationToken);
        }

        /// <inheritdoc />
        public override void HandleRequest(SeekGroupRequest request, IGroupStateContext context, GroupStateType prevState, SessionInfo session, CancellationToken cancellationToken)
        {
            SendStopCommand(context, prevState, session, cancellationToken);
        }

        /// <inheritdoc />
        public override void HandleRequest(BufferGroupRequest request, IGroupStateContext context, GroupStateType prevState, SessionInfo session, CancellationToken cancellationToken)
        {
            SendStopCommand(context, prevState, session, cancellationToken);
        }

        /// <inheritdoc />
        public override void HandleRequest(ReadyGroupRequest request, IGroupStateContext context, GroupStateType prevState, SessionInfo session, CancellationToken cancellationToken)
        {
            SendStopCommand(context, prevState, session, cancellationToken);
        }

        /// <inheritdoc />
        public override void HandleRequest(NextItemGroupRequest request, IGroupStateContext context, GroupStateType prevState, SessionInfo session, CancellationToken cancellationToken)
        {
            // Change state.
            var waitingState = new WaitingGroupState(LoggerFactory);
            context.SetState(waitingState);
            waitingState.HandleRequest(request, context, Type, session, cancellationToken);
        }

        /// <inheritdoc />
        public override void HandleRequest(PreviousItemGroupRequest request, IGroupStateContext context, GroupStateType prevState, SessionInfo session, CancellationToken cancellationToken)
        {
            // Change state.
            var waitingState = new WaitingGroupState(LoggerFactory);
            context.SetState(waitingState);
            waitingState.HandleRequest(request, context, Type, session, cancellationToken);
        }

        private void SendStopCommand(IGroupStateContext context, GroupStateType prevState, SessionInfo session, CancellationToken cancellationToken)
        {
            var command = context.NewSyncPlayCommand(SendCommandType.Stop);
            if (!prevState.Equals(Type))
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
