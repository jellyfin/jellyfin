using System.Threading;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.SyncPlay;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Controller.SyncPlay
{
    /// <summary>
    /// Class AbstractGroupState.
    /// </summary>
    /// <remarks>
    /// Class is not thread-safe, external locking is required when accessing methods.
    /// </remarks>
    public abstract class AbstractGroupState : ISyncPlayState
    {
        /// <summary>
        /// The logger.
        /// </summary>
        protected readonly ILogger _logger;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public AbstractGroupState(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Sends a group state update to all group.
        /// </summary>
        /// <param name="context">The context of the state.</param>
        /// <param name="reason">The reason of the state change.</param>
        /// <param name="session">The session.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        protected void SendGroupStateUpdate(ISyncPlayStateContext context, IPlaybackGroupRequest reason, SessionInfo session, CancellationToken cancellationToken)
        {
            // Notify relevant state change event
            var stateUpdate = new GroupStateUpdate()
            {
                State = GetGroupState(),
                Reason = reason.GetRequestType()
            };
            var update = context.NewSyncPlayGroupUpdate(GroupUpdateType.StateUpdate, stateUpdate);
            context.SendGroupUpdate(session, SyncPlayBroadcastType.AllGroup, update, cancellationToken);
        }

        /// <inheritdoc />
        public abstract GroupState GetGroupState();

        /// <inheritdoc />
        public abstract void SessionJoined(ISyncPlayStateContext context, GroupState prevState, SessionInfo session, CancellationToken cancellationToken);

        /// <inheritdoc />
        public abstract void SessionLeaving(ISyncPlayStateContext context, GroupState prevState, SessionInfo session, CancellationToken cancellationToken);

        /// <inheritdoc />
        public virtual void HandleRequest(ISyncPlayStateContext context, GroupState prevState, IPlaybackGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            UnhandledRequest(request);
        }

        /// <inheritdoc />
        public virtual void HandleRequest(ISyncPlayStateContext context, GroupState prevState, PlayGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            UnhandledRequest(request);
        }

        /// <inheritdoc />
        public virtual void HandleRequest(ISyncPlayStateContext context, GroupState prevState, SetPlaylistItemGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            var waitingState = new WaitingGroupState(_logger);
            context.SetState(waitingState);
            waitingState.HandleRequest(context, GetGroupState(), request, session, cancellationToken);
        }

        /// <inheritdoc />
        public virtual void HandleRequest(ISyncPlayStateContext context, GroupState prevState, RemoveFromPlaylistGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            var playingItemRemoved = context.RemoveFromPlayQueue(request.PlaylistItemIds);

            var playQueueUpdate = context.GetPlayQueueUpdate(PlayQueueUpdateReason.RemoveItems);
            var update = context.NewSyncPlayGroupUpdate(GroupUpdateType.PlayQueue, playQueueUpdate);
            context.SendGroupUpdate(session, SyncPlayBroadcastType.AllGroup, update, cancellationToken);

            if (playingItemRemoved)
            {
                var PlayingItemIndex = context.PlayQueue.PlayingItemIndex;
                if (context.PlayQueue.PlayingItemIndex == -1)
                {
                    _logger.LogDebug("HandleRequest: {0} in group {1}, play queue is empty.", request.GetRequestType(), context.GroupId.ToString());

                    ISyncPlayState idleState = new IdleGroupState(_logger);
                    context.SetState(idleState);
                    var stopRequest = new StopGroupRequest();
                    idleState.HandleRequest(context, GetGroupState(), stopRequest, session, cancellationToken);
                }
            }
        }

        /// <inheritdoc />
        public virtual void HandleRequest(ISyncPlayStateContext context, GroupState prevState, MovePlaylistItemGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            var result = context.MoveItemInPlayQueue(request.PlaylistItemId, request.NewIndex);

            if (!result)
            {
                _logger.LogError("HandleRequest: {0} in group {1}, unable to move item in play queue.", request.GetRequestType(), context.GroupId.ToString());
                return;
            }

            var playQueueUpdate = context.GetPlayQueueUpdate(PlayQueueUpdateReason.MoveItem);
            var update = context.NewSyncPlayGroupUpdate(GroupUpdateType.PlayQueue, playQueueUpdate);
            context.SendGroupUpdate(session, SyncPlayBroadcastType.AllGroup, update, cancellationToken);
        }

        /// <inheritdoc />
        public virtual void HandleRequest(ISyncPlayStateContext context, GroupState prevState, QueueGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            var result = context.AddToPlayQueue(request.ItemIds, request.Mode);

            if (!result)
            {
                _logger.LogError("HandleRequest: {0} in group {1}, unable to add items to play queue.", request.GetRequestType(), context.GroupId.ToString());
                return;
            }

            var reason = request.Mode.Equals("next") ? PlayQueueUpdateReason.QueueNext : PlayQueueUpdateReason.Queue;
            var playQueueUpdate = context.GetPlayQueueUpdate(reason);
            var update = context.NewSyncPlayGroupUpdate(GroupUpdateType.PlayQueue, playQueueUpdate);
            context.SendGroupUpdate(session, SyncPlayBroadcastType.AllGroup, update, cancellationToken);
        }

        /// <inheritdoc />
        public virtual void HandleRequest(ISyncPlayStateContext context, GroupState prevState, UnpauseGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            UnhandledRequest(request);
        }

        /// <inheritdoc />
        public virtual void HandleRequest(ISyncPlayStateContext context, GroupState prevState, PauseGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            UnhandledRequest(request);
        }

        /// <inheritdoc />
        public virtual void HandleRequest(ISyncPlayStateContext context, GroupState prevState, StopGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            UnhandledRequest(request);
        }

        /// <inheritdoc />
        public virtual void HandleRequest(ISyncPlayStateContext context, GroupState prevState, SeekGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            UnhandledRequest(request);
        }

        /// <inheritdoc />
        public virtual void HandleRequest(ISyncPlayStateContext context, GroupState prevState, BufferGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            UnhandledRequest(request);
        }

        /// <inheritdoc />
        public virtual void HandleRequest(ISyncPlayStateContext context, GroupState prevState, ReadyGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            UnhandledRequest(request);
        }

        /// <inheritdoc />
        public virtual void HandleRequest(ISyncPlayStateContext context, GroupState prevState, NextTrackGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            UnhandledRequest(request);
        }

        /// <inheritdoc />
        public virtual void HandleRequest(ISyncPlayStateContext context, GroupState prevState, PreviousTrackGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            UnhandledRequest(request);
        }

        /// <inheritdoc />
        public virtual void HandleRequest(ISyncPlayStateContext context, GroupState prevState, SetRepeatModeGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            context.SetRepeatMode(request.Mode);
            var playQueueUpdate = context.GetPlayQueueUpdate(PlayQueueUpdateReason.RepeatMode);
            var update = context.NewSyncPlayGroupUpdate(GroupUpdateType.PlayQueue, playQueueUpdate);
            context.SendGroupUpdate(session, SyncPlayBroadcastType.AllGroup, update, cancellationToken);
        }

        /// <inheritdoc />
        public virtual void HandleRequest(ISyncPlayStateContext context, GroupState prevState, SetShuffleModeGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            context.SetShuffleMode(request.Mode);
            var playQueueUpdate = context.GetPlayQueueUpdate(PlayQueueUpdateReason.ShuffleMode);
            var update = context.NewSyncPlayGroupUpdate(GroupUpdateType.PlayQueue, playQueueUpdate);
            context.SendGroupUpdate(session, SyncPlayBroadcastType.AllGroup, update, cancellationToken);
        }

        /// <inheritdoc />
        public virtual void HandleRequest(ISyncPlayStateContext context, GroupState prevState, PingGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            // Collected pings are used to account for network latency when unpausing playback
            context.UpdatePing(session, request.Ping);
        }

        /// <inheritdoc />
        public virtual void HandleRequest(ISyncPlayStateContext context, GroupState prevState, IgnoreWaitGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            context.SetIgnoreGroupWait(session, request.IgnoreWait);
        }

        private void UnhandledRequest(IPlaybackGroupRequest request)
        {
            _logger.LogWarning("HandleRequest: unhandled {0} request for {1} state.", request.GetRequestType(), this.GetGroupState());
        }
    }
}
