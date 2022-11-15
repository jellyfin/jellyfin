#nullable disable

using System.Threading;
using MediaBrowser.Controller.Session;
using MediaBrowser.Controller.SyncPlay.PlaybackRequests;
using MediaBrowser.Model.SyncPlay;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Controller.SyncPlay.GroupStates
{
    /// <summary>
    /// Class AbstractGroupState.
    /// </summary>
    /// <remarks>
    /// Class is not thread-safe, external locking is required when accessing methods.
    /// </remarks>
    public abstract class AbstractGroupState : IGroupState
    {
        /// <summary>
        /// The logger.
        /// </summary>
        private readonly ILogger<AbstractGroupState> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="AbstractGroupState"/> class.
        /// </summary>
        /// <param name="loggerFactory">Instance of the <see cref="ILoggerFactory"/> interface.</param>
        protected AbstractGroupState(ILoggerFactory loggerFactory)
        {
            LoggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<AbstractGroupState>();
        }

        /// <inheritdoc />
        public abstract GroupStateType Type { get; }

        /// <summary>
        /// Gets the logger factory.
        /// </summary>
        protected ILoggerFactory LoggerFactory { get; }

        /// <inheritdoc />
        public abstract void SessionJoined(IGroupStateContext context, GroupStateType prevState, SessionInfo session, CancellationToken cancellationToken);

        /// <inheritdoc />
        public abstract void SessionLeaving(IGroupStateContext context, GroupStateType prevState, SessionInfo session, CancellationToken cancellationToken);

        /// <inheritdoc />
        public virtual void HandleRequest(IGroupPlaybackRequest request, IGroupStateContext context, GroupStateType prevState, SessionInfo session, CancellationToken cancellationToken)
        {
            UnhandledRequest(request);
        }

        /// <inheritdoc />
        public virtual void HandleRequest(PlayGroupRequest request, IGroupStateContext context, GroupStateType prevState, SessionInfo session, CancellationToken cancellationToken)
        {
            UnhandledRequest(request);
        }

        /// <inheritdoc />
        public virtual void HandleRequest(SetPlaylistItemGroupRequest request, IGroupStateContext context, GroupStateType prevState, SessionInfo session, CancellationToken cancellationToken)
        {
            var waitingState = new WaitingGroupState(LoggerFactory);
            context.SetState(waitingState);
            waitingState.HandleRequest(request, context, Type, session, cancellationToken);
        }

        /// <inheritdoc />
        public virtual void HandleRequest(RemoveFromPlaylistGroupRequest request, IGroupStateContext context, GroupStateType prevState, SessionInfo session, CancellationToken cancellationToken)
        {
            bool playingItemRemoved;
            if (request.ClearPlaylist)
            {
                context.ClearPlayQueue(request.ClearPlayingItem);
                playingItemRemoved = request.ClearPlayingItem;
            }
            else
            {
                playingItemRemoved = context.RemoveFromPlayQueue(request.PlaylistItemIds);
            }

            var playQueueUpdate = context.GetPlayQueueUpdate(PlayQueueUpdateReason.RemoveItems);
            var update = context.NewSyncPlayGroupUpdate(GroupUpdateType.PlayQueue, playQueueUpdate);
            context.SendGroupUpdate(session, SyncPlayBroadcastType.AllGroup, update, cancellationToken);

            if (playingItemRemoved && !context.PlayQueue.IsItemPlaying())
            {
                _logger.LogDebug("Play queue in group {GroupId} is now empty.", context.GroupId.ToString());

                IGroupState idleState = new IdleGroupState(LoggerFactory);
                context.SetState(idleState);
                var stopRequest = new StopGroupRequest();
                idleState.HandleRequest(stopRequest, context, Type, session, cancellationToken);
            }
        }

        /// <inheritdoc />
        public virtual void HandleRequest(MovePlaylistItemGroupRequest request, IGroupStateContext context, GroupStateType prevState, SessionInfo session, CancellationToken cancellationToken)
        {
            var result = context.MoveItemInPlayQueue(request.PlaylistItemId, request.NewIndex);

            if (!result)
            {
                _logger.LogError("Unable to move item in group {GroupId}.", context.GroupId.ToString());
                return;
            }

            var playQueueUpdate = context.GetPlayQueueUpdate(PlayQueueUpdateReason.MoveItem);
            var update = context.NewSyncPlayGroupUpdate(GroupUpdateType.PlayQueue, playQueueUpdate);
            context.SendGroupUpdate(session, SyncPlayBroadcastType.AllGroup, update, cancellationToken);
        }

        /// <inheritdoc />
        public virtual void HandleRequest(QueueGroupRequest request, IGroupStateContext context, GroupStateType prevState, SessionInfo session, CancellationToken cancellationToken)
        {
            var result = context.AddToPlayQueue(request.ItemIds, request.Mode);

            if (!result)
            {
                _logger.LogError("Unable to add items to play queue in group {GroupId}.", context.GroupId.ToString());
                return;
            }

            var reason = request.Mode switch
            {
                GroupQueueMode.QueueNext => PlayQueueUpdateReason.QueueNext,
                _ => PlayQueueUpdateReason.Queue
            };
            var playQueueUpdate = context.GetPlayQueueUpdate(reason);
            var update = context.NewSyncPlayGroupUpdate(GroupUpdateType.PlayQueue, playQueueUpdate);
            context.SendGroupUpdate(session, SyncPlayBroadcastType.AllGroup, update, cancellationToken);
        }

        /// <inheritdoc />
        public virtual void HandleRequest(UnpauseGroupRequest request, IGroupStateContext context, GroupStateType prevState, SessionInfo session, CancellationToken cancellationToken)
        {
            UnhandledRequest(request);
        }

        /// <inheritdoc />
        public virtual void HandleRequest(PauseGroupRequest request, IGroupStateContext context, GroupStateType prevState, SessionInfo session, CancellationToken cancellationToken)
        {
            UnhandledRequest(request);
        }

        /// <inheritdoc />
        public virtual void HandleRequest(StopGroupRequest request, IGroupStateContext context, GroupStateType prevState, SessionInfo session, CancellationToken cancellationToken)
        {
            UnhandledRequest(request);
        }

        /// <inheritdoc />
        public virtual void HandleRequest(SeekGroupRequest request, IGroupStateContext context, GroupStateType prevState, SessionInfo session, CancellationToken cancellationToken)
        {
            UnhandledRequest(request);
        }

        /// <inheritdoc />
        public virtual void HandleRequest(BufferGroupRequest request, IGroupStateContext context, GroupStateType prevState, SessionInfo session, CancellationToken cancellationToken)
        {
            UnhandledRequest(request);
        }

        /// <inheritdoc />
        public virtual void HandleRequest(ReadyGroupRequest request, IGroupStateContext context, GroupStateType prevState, SessionInfo session, CancellationToken cancellationToken)
        {
            UnhandledRequest(request);
        }

        /// <inheritdoc />
        public virtual void HandleRequest(NextItemGroupRequest request, IGroupStateContext context, GroupStateType prevState, SessionInfo session, CancellationToken cancellationToken)
        {
            UnhandledRequest(request);
        }

        /// <inheritdoc />
        public virtual void HandleRequest(PreviousItemGroupRequest request, IGroupStateContext context, GroupStateType prevState, SessionInfo session, CancellationToken cancellationToken)
        {
            UnhandledRequest(request);
        }

        /// <inheritdoc />
        public virtual void HandleRequest(SetRepeatModeGroupRequest request, IGroupStateContext context, GroupStateType prevState, SessionInfo session, CancellationToken cancellationToken)
        {
            context.SetRepeatMode(request.Mode);
            var playQueueUpdate = context.GetPlayQueueUpdate(PlayQueueUpdateReason.RepeatMode);
            var update = context.NewSyncPlayGroupUpdate(GroupUpdateType.PlayQueue, playQueueUpdate);
            context.SendGroupUpdate(session, SyncPlayBroadcastType.AllGroup, update, cancellationToken);
        }

        /// <inheritdoc />
        public virtual void HandleRequest(SetShuffleModeGroupRequest request, IGroupStateContext context, GroupStateType prevState, SessionInfo session, CancellationToken cancellationToken)
        {
            context.SetShuffleMode(request.Mode);
            var playQueueUpdate = context.GetPlayQueueUpdate(PlayQueueUpdateReason.ShuffleMode);
            var update = context.NewSyncPlayGroupUpdate(GroupUpdateType.PlayQueue, playQueueUpdate);
            context.SendGroupUpdate(session, SyncPlayBroadcastType.AllGroup, update, cancellationToken);
        }

        /// <inheritdoc />
        public virtual void HandleRequest(PingGroupRequest request, IGroupStateContext context, GroupStateType prevState, SessionInfo session, CancellationToken cancellationToken)
        {
            // Collected pings are used to account for network latency when unpausing playback.
            context.UpdatePing(session, request.Ping);
        }

        /// <inheritdoc />
        public virtual void HandleRequest(IgnoreWaitGroupRequest request, IGroupStateContext context, GroupStateType prevState, SessionInfo session, CancellationToken cancellationToken)
        {
            context.SetIgnoreGroupWait(session, request.IgnoreWait);
        }

        /// <summary>
        /// Sends a group state update to all group.
        /// </summary>
        /// <param name="context">The context of the state.</param>
        /// <param name="reason">The reason of the state change.</param>
        /// <param name="session">The session.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        protected void SendGroupStateUpdate(IGroupStateContext context, IGroupPlaybackRequest reason, SessionInfo session, CancellationToken cancellationToken)
        {
            // Notify relevant state change event.
            var stateUpdate = new GroupStateUpdate(Type, reason.Action);
            var update = context.NewSyncPlayGroupUpdate(GroupUpdateType.StateUpdate, stateUpdate);
            context.SendGroupUpdate(session, SyncPlayBroadcastType.AllGroup, update, cancellationToken);
        }

        private void UnhandledRequest(IGroupPlaybackRequest request)
        {
            _logger.LogWarning("Unhandled request of type {RequestType} in {StateType} state.", request.Action, Type);
        }
    }
}
