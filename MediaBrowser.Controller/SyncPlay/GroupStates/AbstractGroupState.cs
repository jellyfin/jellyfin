using System;
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
    public abstract class AbstractGroupState : IGroupState
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AbstractGroupState"/> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger"/> interface.</param>
        protected AbstractGroupState(ILogger logger)
        {
            Logger = logger;
        }

        /// <inheritdoc />
        public abstract GroupStateType Type { get; }

        /// <summary>
        /// Gets the logger.
        /// </summary>
        protected ILogger Logger { get; }

        /// <inheritdoc />
        public abstract void SessionJoined(IGroupStateContext context, GroupStateType prevState, SessionInfo session, CancellationToken cancellationToken);

        /// <inheritdoc />
        public abstract void SessionLeaving(IGroupStateContext context, GroupStateType prevState, SessionInfo session, CancellationToken cancellationToken);

        /// <inheritdoc />
        public virtual void HandleRequest(IGroupStateContext context, GroupStateType prevState, IGroupPlaybackRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            UnhandledRequest(request);
        }

        /// <inheritdoc />
        public virtual void HandleRequest(IGroupStateContext context, GroupStateType prevState, PlayGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            UnhandledRequest(request);
        }

        /// <inheritdoc />
        public virtual void HandleRequest(IGroupStateContext context, GroupStateType prevState, SetPlaylistItemGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            var waitingState = new WaitingGroupState(Logger);
            context.SetState(waitingState);
            waitingState.HandleRequest(context, Type, request, session, cancellationToken);
        }

        /// <inheritdoc />
        public virtual void HandleRequest(IGroupStateContext context, GroupStateType prevState, RemoveFromPlaylistGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            var playingItemRemoved = context.RemoveFromPlayQueue(request.PlaylistItemIds);

            var playQueueUpdate = context.GetPlayQueueUpdate(PlayQueueUpdateReason.RemoveItems);
            var update = context.NewSyncPlayGroupUpdate(GroupUpdateType.PlayQueue, playQueueUpdate);
            context.SendGroupUpdate(session, SyncPlayBroadcastType.AllGroup, update, cancellationToken);

            if (playingItemRemoved && !context.PlayQueue.IsItemPlaying())
            {
                Logger.LogDebug("HandleRequest: {0} in group {1}, play queue is empty.", request.Type, context.GroupId.ToString());

                IGroupState idleState = new IdleGroupState(Logger);
                context.SetState(idleState);
                var stopRequest = new StopGroupRequest();
                idleState.HandleRequest(context, Type, stopRequest, session, cancellationToken);
            }
        }

        /// <inheritdoc />
        public virtual void HandleRequest(IGroupStateContext context, GroupStateType prevState, MovePlaylistItemGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            var result = context.MoveItemInPlayQueue(request.PlaylistItemId, request.NewIndex);

            if (!result)
            {
                Logger.LogError("HandleRequest: {0} in group {1}, unable to move item in play queue.", request.Type, context.GroupId.ToString());
                return;
            }

            var playQueueUpdate = context.GetPlayQueueUpdate(PlayQueueUpdateReason.MoveItem);
            var update = context.NewSyncPlayGroupUpdate(GroupUpdateType.PlayQueue, playQueueUpdate);
            context.SendGroupUpdate(session, SyncPlayBroadcastType.AllGroup, update, cancellationToken);
        }

        /// <inheritdoc />
        public virtual void HandleRequest(IGroupStateContext context, GroupStateType prevState, QueueGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            var result = context.AddToPlayQueue(request.ItemIds, request.Mode);

            if (!result)
            {
                Logger.LogError("HandleRequest: {0} in group {1}, unable to add items to play queue.", request.Type, context.GroupId.ToString());
                return;
            }

            var reason = request.Mode.Equals("next", StringComparison.OrdinalIgnoreCase) ? PlayQueueUpdateReason.QueueNext : PlayQueueUpdateReason.Queue;
            var playQueueUpdate = context.GetPlayQueueUpdate(reason);
            var update = context.NewSyncPlayGroupUpdate(GroupUpdateType.PlayQueue, playQueueUpdate);
            context.SendGroupUpdate(session, SyncPlayBroadcastType.AllGroup, update, cancellationToken);
        }

        /// <inheritdoc />
        public virtual void HandleRequest(IGroupStateContext context, GroupStateType prevState, UnpauseGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            UnhandledRequest(request);
        }

        /// <inheritdoc />
        public virtual void HandleRequest(IGroupStateContext context, GroupStateType prevState, PauseGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            UnhandledRequest(request);
        }

        /// <inheritdoc />
        public virtual void HandleRequest(IGroupStateContext context, GroupStateType prevState, StopGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            UnhandledRequest(request);
        }

        /// <inheritdoc />
        public virtual void HandleRequest(IGroupStateContext context, GroupStateType prevState, SeekGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            UnhandledRequest(request);
        }

        /// <inheritdoc />
        public virtual void HandleRequest(IGroupStateContext context, GroupStateType prevState, BufferGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            UnhandledRequest(request);
        }

        /// <inheritdoc />
        public virtual void HandleRequest(IGroupStateContext context, GroupStateType prevState, ReadyGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            UnhandledRequest(request);
        }

        /// <inheritdoc />
        public virtual void HandleRequest(IGroupStateContext context, GroupStateType prevState, NextTrackGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            UnhandledRequest(request);
        }

        /// <inheritdoc />
        public virtual void HandleRequest(IGroupStateContext context, GroupStateType prevState, PreviousTrackGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            UnhandledRequest(request);
        }

        /// <inheritdoc />
        public virtual void HandleRequest(IGroupStateContext context, GroupStateType prevState, SetRepeatModeGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            context.SetRepeatMode(request.Mode);
            var playQueueUpdate = context.GetPlayQueueUpdate(PlayQueueUpdateReason.RepeatMode);
            var update = context.NewSyncPlayGroupUpdate(GroupUpdateType.PlayQueue, playQueueUpdate);
            context.SendGroupUpdate(session, SyncPlayBroadcastType.AllGroup, update, cancellationToken);
        }

        /// <inheritdoc />
        public virtual void HandleRequest(IGroupStateContext context, GroupStateType prevState, SetShuffleModeGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            context.SetShuffleMode(request.Mode);
            var playQueueUpdate = context.GetPlayQueueUpdate(PlayQueueUpdateReason.ShuffleMode);
            var update = context.NewSyncPlayGroupUpdate(GroupUpdateType.PlayQueue, playQueueUpdate);
            context.SendGroupUpdate(session, SyncPlayBroadcastType.AllGroup, update, cancellationToken);
        }

        /// <inheritdoc />
        public virtual void HandleRequest(IGroupStateContext context, GroupStateType prevState, PingGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            // Collected pings are used to account for network latency when unpausing playback.
            context.UpdatePing(session, request.Ping);
        }

        /// <inheritdoc />
        public virtual void HandleRequest(IGroupStateContext context, GroupStateType prevState, IgnoreWaitGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
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
            var stateUpdate = new GroupStateUpdate()
            {
                State = Type,
                Reason = reason.Type
            };
            var update = context.NewSyncPlayGroupUpdate(GroupUpdateType.StateUpdate, stateUpdate);
            context.SendGroupUpdate(session, SyncPlayBroadcastType.AllGroup, update, cancellationToken);
        }

        private void UnhandledRequest(IGroupPlaybackRequest request)
        {
            Logger.LogWarning("HandleRequest: unhandled {0} request for {1} state.", request.Type, Type);
        }
    }
}
