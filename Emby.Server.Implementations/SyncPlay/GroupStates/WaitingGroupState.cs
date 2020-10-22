using System;
using System.Threading;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.SyncPlay;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Controller.SyncPlay
{
    /// <summary>
    /// Class WaitingGroupState.
    /// </summary>
    /// <remarks>
    /// Class is not thread-safe, external locking is required when accessing methods.
    /// </remarks>
    public class WaitingGroupState : AbstractGroupState
    {
        /// <summary>
        /// Tells the state to switch to after buffering is done.
        /// </summary>
        public bool ResumePlaying { get; set; } = false;

        /// <summary>
        /// Whether the initial state has been set.
        /// </summary>
        private bool InitialStateSet { get; set; } = false;

        /// <summary>
        /// The group state before the first ever event.
        /// </summary>
        private GroupState InitialState { get; set; }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public WaitingGroupState(ILogger logger)
            : base(logger)
        {
            // Do nothing.
        }

        /// <inheritdoc />
        public override GroupState GetGroupState()
        {
            return GroupState.Waiting;
        }

        /// <inheritdoc />
        public override void SessionJoined(ISyncPlayStateContext context, GroupState prevState, SessionInfo session, CancellationToken cancellationToken)
        {
            // Save state if first event.
            if (!InitialStateSet)
            {
                InitialState = prevState;
                InitialStateSet = true;
            }

            if (prevState.Equals(GroupState.Playing)) {
                ResumePlaying = true;
                // Pause group and compute the media playback position.
                var currentTime = DateTime.UtcNow;
                var elapsedTime = currentTime - context.LastActivity;
                context.LastActivity = currentTime;
                // Elapsed time is negative if event happens
                // during the delay added to account for latency.
                // In this phase clients haven't started the playback yet.
                // In other words, LastActivity is in the future,
                // when playback unpause is supposed to happen.
                // Seek only if playback actually started.
                context.PositionTicks += Math.Max(elapsedTime.Ticks, 0);
            }

            // Prepare new session.
            var playQueueUpdate = context.GetPlayQueueUpdate(PlayQueueUpdateReason.NewPlaylist);
            var update = context.NewSyncPlayGroupUpdate(GroupUpdateType.PlayQueue, playQueueUpdate);
            context.SendGroupUpdate(session, SyncPlayBroadcastType.CurrentSession, update, cancellationToken);

            context.SetBuffering(session, true);

            // Send pause command to all non-buffering sessions.
            var command = context.NewSyncPlayCommand(SendCommandType.Pause);
            context.SendCommand(session, SyncPlayBroadcastType.AllReady, command, cancellationToken);
        }

        /// <inheritdoc />
        public override void SessionLeaving(ISyncPlayStateContext context, GroupState prevState, SessionInfo session, CancellationToken cancellationToken)
        {
            // Save state if first event.
            if (!InitialStateSet)
            {
                InitialState = prevState;
                InitialStateSet = true;
            }

            context.SetBuffering(session, false);

            if (!context.IsBuffering())
            {
                if (ResumePlaying)
                {
                    // Client, that was buffering, left the group.
                    var playingState = new PlayingGroupState(_logger);
                    context.SetState(playingState);
                    var unpauseRequest = new UnpauseGroupRequest();
                    playingState.HandleRequest(context, GetGroupState(), unpauseRequest, session, cancellationToken);

                    _logger.LogDebug("SessionLeaving: {0} left the group {1}, notifying others to resume.", session.Id.ToString(), context.GroupId.ToString());
                }
                else
                {
                    // Group is ready, returning to previous state.
                    var pausedState = new PausedGroupState(_logger);
                    context.SetState(pausedState);

                    _logger.LogDebug("SessionLeaving: {0} left the group {1}, returning to previous state.", session.Id.ToString(), context.GroupId.ToString());
                }
            }
        }

        /// <inheritdoc />
        public override void HandleRequest(ISyncPlayStateContext context, GroupState prevState, PlayGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            // Save state if first event.
            if (!InitialStateSet)
            {
                InitialState = prevState;
                InitialStateSet = true;
            }

            ResumePlaying = true;

            var setQueueStatus = context.SetPlayQueue(request.PlayingQueue, request.PlayingItemPosition, request.StartPositionTicks);
            if (!setQueueStatus)
            {
                _logger.LogError("HandleRequest: {0} in group {1}, unable to set playing queue.", request.GetRequestType(), context.GroupId.ToString());

                // Ignore request and return to previous state.
                ISyncPlayState newState;
                switch (prevState)
                {
                    case GroupState.Playing:
                        newState = new PlayingGroupState(_logger);
                        break;
                    case GroupState.Paused:
                        newState = new PausedGroupState(_logger);
                        break;
                    default:
                        newState = new IdleGroupState(_logger);
                        break;
                }

                context.SetState(newState);
                return;
            }

            var playQueueUpdate = context.GetPlayQueueUpdate(PlayQueueUpdateReason.NewPlaylist);
            var update = context.NewSyncPlayGroupUpdate(GroupUpdateType.PlayQueue, playQueueUpdate);
            context.SendGroupUpdate(session, SyncPlayBroadcastType.AllGroup, update, cancellationToken);

            // Reset status of sessions and await for all Ready events.
            context.SetAllBuffering(true);

            _logger.LogDebug("HandleRequest: {0} in group {1}, {2} set a new play queue.", request.GetRequestType(), context.GroupId.ToString(), session.Id.ToString());
        }

        /// <inheritdoc />
        public override void HandleRequest(ISyncPlayStateContext context, GroupState prevState, SetPlaylistItemGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            // Save state if first event.
            if (!InitialStateSet)
            {
                InitialState = prevState;
                InitialStateSet = true;
            }

            ResumePlaying = true;

            var result = context.SetPlayingItem(request.PlaylistItemId);
            if (result)
            {
                var playQueueUpdate = context.GetPlayQueueUpdate(PlayQueueUpdateReason.SetCurrentItem);
                var update = context.NewSyncPlayGroupUpdate(GroupUpdateType.PlayQueue, playQueueUpdate);
                context.SendGroupUpdate(session, SyncPlayBroadcastType.AllGroup, update, cancellationToken);

                // Reset status of sessions and await for all Ready events.
                context.SetAllBuffering(true);
            }
            else
            {
                // Return to old state.
                ISyncPlayState newState;
                switch (prevState)
                {
                    case GroupState.Playing:
                        newState = new PlayingGroupState(_logger);
                        break;
                    case GroupState.Paused:
                        newState = new PausedGroupState(_logger);
                        break;
                    default:
                        newState = new IdleGroupState(_logger);
                        break;
                }

                context.SetState(newState);

                _logger.LogDebug("HandleRequest: {0} in group {1}, unable to change current playing item.", request.GetRequestType(), context.GroupId.ToString());
            }
        }

        /// <inheritdoc />
        public override void HandleRequest(ISyncPlayStateContext context, GroupState prevState, UnpauseGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            // Save state if first event.
            if (!InitialStateSet)
            {
                InitialState = prevState;
                InitialStateSet = true;
            }

            if (prevState.Equals(GroupState.Idle))
            {
                ResumePlaying = true;
                context.RestartCurrentItem();

                var playQueueUpdate = context.GetPlayQueueUpdate(PlayQueueUpdateReason.NewPlaylist);
                var update = context.NewSyncPlayGroupUpdate(GroupUpdateType.PlayQueue, playQueueUpdate);
                context.SendGroupUpdate(session, SyncPlayBroadcastType.AllGroup, update, cancellationToken);

                // Reset status of sessions and await for all Ready events.
                context.SetAllBuffering(true);

                _logger.LogDebug("HandleRequest: {0} in group {1}, waiting for all ready events.", request.GetRequestType(), context.GroupId.ToString());
            }
            else
            {
                if (ResumePlaying)
                {
                    _logger.LogDebug("HandleRequest: {0} in group {1}, ignoring sessions that are not ready and forcing the playback to start.", request.GetRequestType(), context.GroupId.ToString());

                    // An Unpause request is forcing the playback to start, ignoring sessions that are not ready.
                    context.SetAllBuffering(false);

                    // Change state.
                    var playingState = new PlayingGroupState(_logger);
                    playingState.IgnoreBuffering = true;
                    context.SetState(playingState);
                    playingState.HandleRequest(context, GetGroupState(), request, session, cancellationToken);
                }
                else
                {
                    // Group would have gone to paused state, now will go to playing state when ready.
                    ResumePlaying = true;

                    // Notify relevant state change event.
                    SendGroupStateUpdate(context, request, session, cancellationToken);
                }
            }
        }

        /// <inheritdoc />
        public override void HandleRequest(ISyncPlayStateContext context, GroupState prevState, PauseGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            // Save state if first event.
            if (!InitialStateSet)
            {
                InitialState = prevState;
                InitialStateSet = true;
            }

            // Wait for sessions to be ready, then switch to paused state.
            ResumePlaying = false;

            // Notify relevant state change event.
            SendGroupStateUpdate(context, request, session, cancellationToken);
        }

        /// <inheritdoc />
        public override void HandleRequest(ISyncPlayStateContext context, GroupState prevState, StopGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            // Save state if first event.
            if (!InitialStateSet)
            {
                InitialState = prevState;
                InitialStateSet = true;
            }

            // Change state.
            var idleState = new IdleGroupState(_logger);
            context.SetState(idleState);
            idleState.HandleRequest(context, GetGroupState(), request, session, cancellationToken);
        }

        /// <inheritdoc />
        public override void HandleRequest(ISyncPlayStateContext context, GroupState prevState, SeekGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            // Save state if first event.
            if (!InitialStateSet)
            {
                InitialState = prevState;
                InitialStateSet = true;
            }

            if (prevState.Equals(GroupState.Playing))
            {
                ResumePlaying = true;
            }
            else if(prevState.Equals(GroupState.Paused))
            {
                ResumePlaying = false;
            }

            // Sanitize PositionTicks.
            var ticks = context.SanitizePositionTicks(request.PositionTicks);

            // Seek.
            context.PositionTicks = ticks;
            context.LastActivity = DateTime.UtcNow;

            var command = context.NewSyncPlayCommand(SendCommandType.Seek);
            context.SendCommand(session, SyncPlayBroadcastType.AllGroup, command, cancellationToken);

            // Reset status of sessions and await for all Ready events.
            context.SetAllBuffering(true);

            // Notify relevant state change event.
            SendGroupStateUpdate(context, request, session, cancellationToken);
        }

        /// <inheritdoc />
        public override void HandleRequest(ISyncPlayStateContext context, GroupState prevState, BufferGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            // Save state if first event.
            if (!InitialStateSet)
            {
                InitialState = prevState;
                InitialStateSet = true;
            }

            // Make sure the client is playing the correct item.
            if (!request.PlaylistItemId.Equals(context.PlayQueue.GetPlayingItemPlaylistId()))
            {
                _logger.LogDebug("HandleRequest: {0} in group {1}, {2} has wrong playlist item.", request.GetRequestType(), context.GroupId.ToString(), session.Id.ToString());

                var playQueueUpdate = context.GetPlayQueueUpdate(PlayQueueUpdateReason.SetCurrentItem);
                var updateSession = context.NewSyncPlayGroupUpdate(GroupUpdateType.PlayQueue, playQueueUpdate);
                context.SendGroupUpdate(session, SyncPlayBroadcastType.CurrentSession, updateSession, cancellationToken);
                context.SetBuffering(session, true);

                return;
            }

            if (prevState.Equals(GroupState.Playing))
            {
                // Resume playback when all ready.
                ResumePlaying = true;

                context.SetBuffering(session, true);

                // Pause group and compute the media playback position.
                var currentTime = DateTime.UtcNow;
                var elapsedTime = currentTime - context.LastActivity;
                context.LastActivity = currentTime;
                // Elapsed time is negative if event happens
                // during the delay added to account for latency.
                // In this phase clients haven't started the playback yet.
                // In other words, LastActivity is in the future,
                // when playback unpause is supposed to happen.
                // Seek only if playback actually started.
                context.PositionTicks += Math.Max(elapsedTime.Ticks, 0);

                // Send pause command to all non-buffering sessions.
                var command = context.NewSyncPlayCommand(SendCommandType.Pause);
                context.SendCommand(session, SyncPlayBroadcastType.AllReady, command, cancellationToken);
            }
            else if (prevState.Equals(GroupState.Paused))
            {
                // Don't resume playback when all ready.
                ResumePlaying = false;

                context.SetBuffering(session, true);

                // Send pause command to buffering session.
                var command = context.NewSyncPlayCommand(SendCommandType.Pause);
                context.SendCommand(session, SyncPlayBroadcastType.CurrentSession, command, cancellationToken);
            }
            else if (prevState.Equals(GroupState.Waiting))
            {
                // Another session is now buffering.
                context.SetBuffering(session, true);

                if (!ResumePlaying)
                {
                    // Force update for this session that should be paused.
                    var command = context.NewSyncPlayCommand(SendCommandType.Pause);
                    context.SendCommand(session, SyncPlayBroadcastType.CurrentSession, command, cancellationToken);
                }
            }

            // Notify relevant state change event.
            SendGroupStateUpdate(context, request, session, cancellationToken);
        }

        /// <inheritdoc />
        public override void HandleRequest(ISyncPlayStateContext context, GroupState prevState, ReadyGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            // Save state if first event.
            if (!InitialStateSet)
            {
                InitialState = prevState;
                InitialStateSet = true;
            }

            // Make sure the client is playing the correct item.
            if (!request.PlaylistItemId.Equals(context.PlayQueue.GetPlayingItemPlaylistId()))
            {
                _logger.LogDebug("HandleRequest: {0} in group {1}, {2} has wrong playlist item.", request.GetRequestType(), context.GroupId.ToString(), session.Id.ToString());

                var playQueueUpdate = context.GetPlayQueueUpdate(PlayQueueUpdateReason.SetCurrentItem);
                var update = context.NewSyncPlayGroupUpdate(GroupUpdateType.PlayQueue, playQueueUpdate);
                context.SendGroupUpdate(session, SyncPlayBroadcastType.CurrentSession, update, cancellationToken);
                context.SetBuffering(session, true);

                return;
            }

            var requestTicks = context.SanitizePositionTicks(request.PositionTicks);
            var currentTime = DateTime.UtcNow;
            var elapsedTime = currentTime - request.When;
            if (!request.IsPlaying)
            {
                elapsedTime = TimeSpan.Zero;
            }

            var clientPosition = TimeSpan.FromTicks(requestTicks) + elapsedTime;
            var delayTicks = context.PositionTicks - clientPosition.Ticks;

            if (delayTicks > TimeSpan.FromSeconds(5).Ticks)
            {
                // The client is really behind, other participants will have to wait a lot of time...
                _logger.LogWarning("HandleRequest: {0} in group {1}, {2} got lost in time.", request.GetRequestType(), context.GroupId.ToString(), session.Id.ToString());
            }

            if (ResumePlaying)
            {
                // Handle case where session reported as ready but in reality
                // it has no clue of the real position nor the playback state.
                if (!request.IsPlaying && Math.Abs(context.PositionTicks - requestTicks) > TimeSpan.FromSeconds(0.5).Ticks) {
                    // Session not ready at all.
                    context.SetBuffering(session, true);

                    // Correcting session's position.
                    var command = context.NewSyncPlayCommand(SendCommandType.Seek);
                    context.SendCommand(session, SyncPlayBroadcastType.CurrentSession, command, cancellationToken);

                    // Notify relevant state change event.
                    SendGroupStateUpdate(context, request, session, cancellationToken);

                    _logger.LogDebug("HandleRequest: {0} in group {1}, {2} got lost in time, correcting.", request.GetRequestType(), context.GroupId.ToString(), session.Id.ToString());
                    return;
                }

                // Session is ready.
                context.SetBuffering(session, false);

                if (context.IsBuffering())
                {
                    // Others are still buffering, tell this client to pause when ready.
                    var command = context.NewSyncPlayCommand(SendCommandType.Pause);
                    var pauseAtTime = currentTime.AddTicks(delayTicks);
                    command.When = context.DateToUTCString(pauseAtTime);
                    context.SendCommand(session, SyncPlayBroadcastType.CurrentSession, command, cancellationToken);

                    _logger.LogDebug("HandleRequest: {0} in group {1}, others still buffering, {2} will pause when ready.", request.GetRequestType(), context.GroupId.ToString(), session.Id.ToString());
                }
                else
                {
                    // If all ready, then start playback.
                    // Let other clients resume as soon as the buffering client catches up.
                    if (delayTicks > context.GetHighestPing() * 2 * TimeSpan.TicksPerMillisecond)
                    {
                        // Client that was buffering is recovering, notifying others to resume.
                        context.LastActivity = currentTime.AddTicks(delayTicks);
                        var command = context.NewSyncPlayCommand(SendCommandType.Unpause);
                        var filter = SyncPlayBroadcastType.AllExceptCurrentSession;
                        if (!request.IsPlaying)
                        {
                            filter = SyncPlayBroadcastType.AllGroup;
                        }

                        context.SendCommand(session, filter, command, cancellationToken);

                        _logger.LogDebug("HandleRequest: {0} in group {1}, {2} is recovering, notifying others to resume.", request.GetRequestType(), context.GroupId.ToString(), session.Id.ToString());
                    }
                    else
                    {
                        // Client, that was buffering, resumed playback but did not update others in time.
                        delayTicks = context.GetHighestPing() * 2 * TimeSpan.TicksPerMillisecond;
                        delayTicks = Math.Max(delayTicks, context.DefaultPing);

                        context.LastActivity = currentTime.AddTicks(delayTicks);

                        var command = context.NewSyncPlayCommand(SendCommandType.Unpause);
                        context.SendCommand(session, SyncPlayBroadcastType.AllGroup, command, cancellationToken);

                        _logger.LogDebug("HandleRequest: {0} in group {1}, {2} resumed playback but did not update others in time.", request.GetRequestType(), context.GroupId.ToString(), session.Id.ToString());
                    }

                    // Change state.
                    var playingState = new PlayingGroupState(_logger);
                    context.SetState(playingState);
                    playingState.HandleRequest(context, GetGroupState(), request, session, cancellationToken);
                }
            }
            else
            {
                // Check that session is really ready, tollerate half second difference to account for player imperfections.
                if (Math.Abs(context.PositionTicks - requestTicks) > TimeSpan.FromSeconds(0.5).Ticks)
                {
                    // Session still not ready.
                    context.SetBuffering(session, true);

                    // Session is seeking to wrong position, correcting.
                    var command = context.NewSyncPlayCommand(SendCommandType.Seek);
                    context.SendCommand(session, SyncPlayBroadcastType.CurrentSession, command, cancellationToken);

                    // Notify relevant state change event.
                    SendGroupStateUpdate(context, request, session, cancellationToken);

                    _logger.LogDebug("HandleRequest: {0} in group {1}, {2} was seeking to wrong position, correcting.", request.GetRequestType(), context.GroupId.ToString(), session.Id.ToString());
                    return;
                } else {
                    // Session is ready.
                    context.SetBuffering(session, false);
                }

                if (!context.IsBuffering())
                {
                    // Group is ready, returning to previous state.
                    var pausedState = new PausedGroupState(_logger);
                    context.SetState(pausedState);

                    if (InitialState.Equals(GroupState.Playing))
                    {
                        // Group went from playing to waiting state and a pause request occured while waiting.
                        var pauserequest = new PauseGroupRequest();
                        pausedState.HandleRequest(context, GetGroupState(), pauserequest, session, cancellationToken);
                    }
                    else if (InitialState.Equals(GroupState.Paused))
                    {
                        pausedState.HandleRequest(context, GetGroupState(), request, session, cancellationToken);
                    }

                    _logger.LogDebug("HandleRequest: {0} in group {1}, {2} is ready, returning to previous state.", request.GetRequestType(), context.GroupId.ToString(), session.Id.ToString());
                }
            }
        }

        /// <inheritdoc />
        public override void HandleRequest(ISyncPlayStateContext context, GroupState prevState, NextTrackGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            // Save state if first event.
            if (!InitialStateSet)
            {
                InitialState = prevState;
                InitialStateSet = true;
            }

            ResumePlaying = true;

            // Make sure the client knows the playing item, to avoid duplicate requests.
            if (!request.PlaylistItemId.Equals(context.PlayQueue.GetPlayingItemPlaylistId()))
            {
                _logger.LogDebug("HandleRequest: {0} in group {1}, client provided the wrong playlist identifier.", request.GetRequestType(), context.GroupId.ToString());
                return;
            }

            var newItem = context.NextItemInQueue();
            if (newItem)
            {
                // Send playing-queue update.
                var playQueueUpdate = context.GetPlayQueueUpdate(PlayQueueUpdateReason.NextTrack);
                var update = context.NewSyncPlayGroupUpdate(GroupUpdateType.PlayQueue, playQueueUpdate);
                context.SendGroupUpdate(session, SyncPlayBroadcastType.AllGroup, update, cancellationToken);

                // Reset status of sessions and await for all Ready events.
                context.SetAllBuffering(true);
            }
            else
            {
                // Return to old state.
                ISyncPlayState newState;
                switch (prevState)
                {
                    case GroupState.Playing:
                        newState = new PlayingGroupState(_logger);
                        break;
                    case GroupState.Paused:
                        newState = new PausedGroupState(_logger);
                        break;
                    default:
                        newState = new IdleGroupState(_logger);
                        break;
                }

                context.SetState(newState);

                _logger.LogDebug("HandleRequest: {0} in group {1}, no next track available.", request.GetRequestType(), context.GroupId.ToString());
            }
        }

        /// <inheritdoc />
        public override void HandleRequest(ISyncPlayStateContext context, GroupState prevState, PreviousTrackGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            // Save state if first event.
            if (!InitialStateSet)
            {
                InitialState = prevState;
                InitialStateSet = true;
            }

            ResumePlaying = true;

            // Make sure the client knows the playing item, to avoid duplicate requests.
            if (!request.PlaylistItemId.Equals(context.PlayQueue.GetPlayingItemPlaylistId()))
            {
                _logger.LogDebug("HandleRequest: {0} in group {1}, client provided the wrong playlist identifier.", request.GetRequestType(), context.GroupId.ToString());
                return;
            }

            var newItem = context.PreviousItemInQueue();
            if (newItem)
            {
                // Send playing-queue update.
                var playQueueUpdate = context.GetPlayQueueUpdate(PlayQueueUpdateReason.PreviousTrack);
                var update = context.NewSyncPlayGroupUpdate(GroupUpdateType.PlayQueue, playQueueUpdate);
                context.SendGroupUpdate(session, SyncPlayBroadcastType.AllGroup, update, cancellationToken);

                // Reset status of sessions and await for all Ready events.
                context.SetAllBuffering(true);
            }
            else
            {
                // Return to old state.
                ISyncPlayState newState;
                switch (prevState)
                {
                    case GroupState.Playing:
                        newState = new PlayingGroupState(_logger);
                        break;
                    case GroupState.Paused:
                        newState = new PausedGroupState(_logger);
                        break;
                    default:
                        newState = new IdleGroupState(_logger);
                        break;
                }

                context.SetState(newState);

                _logger.LogDebug("HandleRequest: {0} in group {1}, no previous track available.", request.GetRequestType(), context.GroupId.ToString());
            }
        }
    }
}
