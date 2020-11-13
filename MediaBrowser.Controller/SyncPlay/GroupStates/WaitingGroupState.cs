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
        /// Initializes a new instance of the <see cref="WaitingGroupState"/> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger"/> interface.</param>
        public WaitingGroupState(ILogger logger)
            : base(logger)
        {
            // Do nothing.
        }

        /// <inheritdoc />
        public override GroupStateType Type
        {
            get
            {
                return GroupStateType.Waiting;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether playback should resume when group is ready.
        /// </summary>
        public bool ResumePlaying { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether the initial state has been set.
        /// </summary>
        private bool InitialStateSet { get; set; } = false;

        /// <summary>
        /// Gets or sets the group state before the first ever event.
        /// </summary>
        private GroupStateType InitialState { get; set; }

        /// <inheritdoc />
        public override void SessionJoined(IGroupStateContext context, GroupStateType prevState, SessionInfo session, CancellationToken cancellationToken)
        {
            // Save state if first event.
            if (!InitialStateSet)
            {
                InitialState = prevState;
                InitialStateSet = true;
            }

            if (prevState.Equals(GroupStateType.Playing))
            {
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
        public override void SessionLeaving(IGroupStateContext context, GroupStateType prevState, SessionInfo session, CancellationToken cancellationToken)
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
                    var playingState = new PlayingGroupState(Logger);
                    context.SetState(playingState);
                    var unpauseRequest = new UnpauseGroupRequest();
                    playingState.HandleRequest(context, Type, unpauseRequest, session, cancellationToken);

                    Logger.LogDebug("SessionLeaving: {0} left the group {1}, notifying others to resume.", session.Id, context.GroupId.ToString());
                }
                else
                {
                    // Group is ready, returning to previous state.
                    var pausedState = new PausedGroupState(Logger);
                    context.SetState(pausedState);

                    Logger.LogDebug("SessionLeaving: {0} left the group {1}, returning to previous state.", session.Id, context.GroupId.ToString());
                }
            }
        }

        /// <inheritdoc />
        public override void HandleRequest(IGroupStateContext context, GroupStateType prevState, PlayGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
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
                Logger.LogError("HandleRequest: {0} in group {1}, unable to set playing queue.", request.Type, context.GroupId.ToString());

                // Ignore request and return to previous state.
                IGroupState newState = prevState switch {
                    GroupStateType.Playing => new PlayingGroupState(Logger),
                    GroupStateType.Paused => new PausedGroupState(Logger),
                    _ => new IdleGroupState(Logger)
                };

                context.SetState(newState);
                return;
            }

            var playQueueUpdate = context.GetPlayQueueUpdate(PlayQueueUpdateReason.NewPlaylist);
            var update = context.NewSyncPlayGroupUpdate(GroupUpdateType.PlayQueue, playQueueUpdate);
            context.SendGroupUpdate(session, SyncPlayBroadcastType.AllGroup, update, cancellationToken);

            // Reset status of sessions and await for all Ready events.
            context.SetAllBuffering(true);

            Logger.LogDebug("HandleRequest: {0} in group {1}, {2} set a new play queue.", request.Type, context.GroupId.ToString(), session.Id);
        }

        /// <inheritdoc />
        public override void HandleRequest(IGroupStateContext context, GroupStateType prevState, SetPlaylistItemGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
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
                IGroupState newState = prevState switch
                {
                    GroupStateType.Playing => new PlayingGroupState(Logger),
                    GroupStateType.Paused => new PausedGroupState(Logger),
                    _ => new IdleGroupState(Logger)
                };

                context.SetState(newState);

                Logger.LogDebug("HandleRequest: {0} in group {1}, unable to change current playing item.", request.Type, context.GroupId.ToString());
            }
        }

        /// <inheritdoc />
        public override void HandleRequest(IGroupStateContext context, GroupStateType prevState, UnpauseGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            // Save state if first event.
            if (!InitialStateSet)
            {
                InitialState = prevState;
                InitialStateSet = true;
            }

            if (prevState.Equals(GroupStateType.Idle))
            {
                ResumePlaying = true;
                context.RestartCurrentItem();

                var playQueueUpdate = context.GetPlayQueueUpdate(PlayQueueUpdateReason.NewPlaylist);
                var update = context.NewSyncPlayGroupUpdate(GroupUpdateType.PlayQueue, playQueueUpdate);
                context.SendGroupUpdate(session, SyncPlayBroadcastType.AllGroup, update, cancellationToken);

                // Reset status of sessions and await for all Ready events.
                context.SetAllBuffering(true);

                Logger.LogDebug("HandleRequest: {0} in group {1}, waiting for all ready events.", request.Type, context.GroupId.ToString());
            }
            else
            {
                if (ResumePlaying)
                {
                    Logger.LogDebug("HandleRequest: {0} in group {1}, ignoring sessions that are not ready and forcing the playback to start.", request.Type, context.GroupId.ToString());

                    // An Unpause request is forcing the playback to start, ignoring sessions that are not ready.
                    context.SetAllBuffering(false);

                    // Change state.
                    var playingState = new PlayingGroupState(Logger)
                    {
                        IgnoreBuffering = true
                    };
                    context.SetState(playingState);
                    playingState.HandleRequest(context, Type, request, session, cancellationToken);
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
        public override void HandleRequest(IGroupStateContext context, GroupStateType prevState, PauseGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
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
        public override void HandleRequest(IGroupStateContext context, GroupStateType prevState, StopGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            // Save state if first event.
            if (!InitialStateSet)
            {
                InitialState = prevState;
                InitialStateSet = true;
            }

            // Change state.
            var idleState = new IdleGroupState(Logger);
            context.SetState(idleState);
            idleState.HandleRequest(context, Type, request, session, cancellationToken);
        }

        /// <inheritdoc />
        public override void HandleRequest(IGroupStateContext context, GroupStateType prevState, SeekGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            // Save state if first event.
            if (!InitialStateSet)
            {
                InitialState = prevState;
                InitialStateSet = true;
            }

            if (prevState.Equals(GroupStateType.Playing))
            {
                ResumePlaying = true;
            }
            else if (prevState.Equals(GroupStateType.Paused))
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
        public override void HandleRequest(IGroupStateContext context, GroupStateType prevState, BufferGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            // Save state if first event.
            if (!InitialStateSet)
            {
                InitialState = prevState;
                InitialStateSet = true;
            }

            // Make sure the client is playing the correct item.
            if (!request.PlaylistItemId.Equals(context.PlayQueue.GetPlayingItemPlaylistId(), StringComparison.OrdinalIgnoreCase))
            {
                Logger.LogDebug("HandleRequest: {0} in group {1}, {2} has wrong playlist item.", request.Type, context.GroupId.ToString(), session.Id);

                var playQueueUpdate = context.GetPlayQueueUpdate(PlayQueueUpdateReason.SetCurrentItem);
                var updateSession = context.NewSyncPlayGroupUpdate(GroupUpdateType.PlayQueue, playQueueUpdate);
                context.SendGroupUpdate(session, SyncPlayBroadcastType.CurrentSession, updateSession, cancellationToken);
                context.SetBuffering(session, true);

                return;
            }

            if (prevState.Equals(GroupStateType.Playing))
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
            else if (prevState.Equals(GroupStateType.Paused))
            {
                // Don't resume playback when all ready.
                ResumePlaying = false;

                context.SetBuffering(session, true);

                // Send pause command to buffering session.
                var command = context.NewSyncPlayCommand(SendCommandType.Pause);
                context.SendCommand(session, SyncPlayBroadcastType.CurrentSession, command, cancellationToken);
            }
            else if (prevState.Equals(GroupStateType.Waiting))
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
        public override void HandleRequest(IGroupStateContext context, GroupStateType prevState, ReadyGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            // Save state if first event.
            if (!InitialStateSet)
            {
                InitialState = prevState;
                InitialStateSet = true;
            }

            // Make sure the client is playing the correct item.
            if (!request.PlaylistItemId.Equals(context.PlayQueue.GetPlayingItemPlaylistId(), StringComparison.OrdinalIgnoreCase))
            {
                Logger.LogDebug("HandleRequest: {0} in group {1}, {2} has wrong playlist item.", request.Type, context.GroupId.ToString(), session.Id);

                var playQueueUpdate = context.GetPlayQueueUpdate(PlayQueueUpdateReason.SetCurrentItem);
                var update = context.NewSyncPlayGroupUpdate(GroupUpdateType.PlayQueue, playQueueUpdate);
                context.SendGroupUpdate(session, SyncPlayBroadcastType.CurrentSession, update, cancellationToken);
                context.SetBuffering(session, true);

                return;
            }

            // Compute elapsed time between the client reported time and now.
            // Elapsed time is used to estimate the client position when playback is unpaused.
            // Ideally, the request is received and handled without major delays.
            // However, to avoid waiting indefinitely when a client is not reporting a correct time,
            // the elapsed time is ignored after a certain threshold.
            var currentTime = DateTime.UtcNow;
            var elapsedTime = currentTime.Subtract(request.When);
            var timeSyncThresholdTicks = TimeSpan.FromMilliseconds(context.TimeSyncOffset).Ticks;
            if (Math.Abs(elapsedTime.Ticks) > timeSyncThresholdTicks)
            {
                Logger.LogWarning("HandleRequest: {0} in group {1}, {2} is not time syncing properly. Ignoring elapsed time.", request.Type, context.GroupId.ToString(), session.Id);

                elapsedTime = TimeSpan.Zero;
            }

            // Ignore elapsed time if client is paused.
            if (!request.IsPlaying)
            {
                elapsedTime = TimeSpan.Zero;
            }

            var requestTicks = context.SanitizePositionTicks(request.PositionTicks);
            var clientPosition = TimeSpan.FromTicks(requestTicks) + elapsedTime;
            var delayTicks = context.PositionTicks - clientPosition.Ticks;
            var maxPlaybackOffsetTicks = TimeSpan.FromMilliseconds(context.MaxPlaybackOffset).Ticks;

            Logger.LogDebug("HandleRequest: {0} in group {1}, {2} at {3} (delay of {4} seconds).", request.Type, context.GroupId.ToString(), session.Id, clientPosition, TimeSpan.FromTicks(delayTicks).TotalSeconds);

            if (ResumePlaying)
            {
                // Handle case where session reported as ready but in reality
                // it has no clue of the real position nor the playback state.
                if (!request.IsPlaying && Math.Abs(delayTicks) > maxPlaybackOffsetTicks)
                {
                    // Session not ready at all.
                    context.SetBuffering(session, true);

                    // Correcting session's position.
                    var command = context.NewSyncPlayCommand(SendCommandType.Seek);
                    context.SendCommand(session, SyncPlayBroadcastType.CurrentSession, command, cancellationToken);

                    // Notify relevant state change event.
                    SendGroupStateUpdate(context, request, session, cancellationToken);

                    Logger.LogWarning("HandleRequest: {0} in group {1}, {2} got lost in time, correcting.", request.Type, context.GroupId.ToString(), session.Id);
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

                    Logger.LogInformation("HandleRequest: {0} in group {1}, others still buffering, {2} will pause when ready in {3} seconds.", request.Type, context.GroupId.ToString(), session.Id, TimeSpan.FromTicks(delayTicks).TotalSeconds);
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

                        Logger.LogInformation("HandleRequest: {0} in group {1}, {2} is recovering, notifying others to resume in {3} seconds.", request.Type, context.GroupId.ToString(), session.Id, TimeSpan.FromTicks(delayTicks).TotalSeconds);
                    }
                    else
                    {
                        // Client, that was buffering, resumed playback but did not update others in time.
                        delayTicks = context.GetHighestPing() * 2 * TimeSpan.TicksPerMillisecond;
                        delayTicks = Math.Max(delayTicks, context.DefaultPing);

                        context.LastActivity = currentTime.AddTicks(delayTicks);

                        var command = context.NewSyncPlayCommand(SendCommandType.Unpause);
                        context.SendCommand(session, SyncPlayBroadcastType.AllGroup, command, cancellationToken);

                        Logger.LogWarning("HandleRequest: {0} in group {1}, {2} resumed playback but did not update others in time. {3} seconds to recover.", request.Type, context.GroupId.ToString(), session.Id, TimeSpan.FromTicks(delayTicks).TotalSeconds);
                    }

                    // Change state.
                    var playingState = new PlayingGroupState(Logger);
                    context.SetState(playingState);
                    playingState.HandleRequest(context, Type, request, session, cancellationToken);
                }
            }
            else
            {
                // Check that session is really ready, tollerate player imperfections under a certain threshold.
                if (Math.Abs(context.PositionTicks - requestTicks) > maxPlaybackOffsetTicks)
                {
                    // Session still not ready.
                    context.SetBuffering(session, true);
                    // Session is seeking to wrong position, correcting.
                    var command = context.NewSyncPlayCommand(SendCommandType.Seek);
                    context.SendCommand(session, SyncPlayBroadcastType.CurrentSession, command, cancellationToken);

                    // Notify relevant state change event.
                    SendGroupStateUpdate(context, request, session, cancellationToken);

                    Logger.LogWarning("HandleRequest: {0} in group {1}, {2} was seeking to wrong position, correcting.", request.Type, context.GroupId.ToString(), session.Id);
                    return;
                }
                else
                {
                    // Session is ready.
                    context.SetBuffering(session, false);
                }

                if (!context.IsBuffering())
                {
                    // Group is ready, returning to previous state.
                    var pausedState = new PausedGroupState(Logger);
                    context.SetState(pausedState);

                    if (InitialState.Equals(GroupStateType.Playing))
                    {
                        // Group went from playing to waiting state and a pause request occured while waiting.
                        var pauserequest = new PauseGroupRequest();
                        pausedState.HandleRequest(context, Type, pauserequest, session, cancellationToken);
                    }
                    else if (InitialState.Equals(GroupStateType.Paused))
                    {
                        pausedState.HandleRequest(context, Type, request, session, cancellationToken);
                    }

                    Logger.LogDebug("HandleRequest: {0} in group {1}, {2} is ready, returning to previous state.", request.Type, context.GroupId.ToString(), session.Id);
                }
            }
        }

        /// <inheritdoc />
        public override void HandleRequest(IGroupStateContext context, GroupStateType prevState, NextTrackGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            // Save state if first event.
            if (!InitialStateSet)
            {
                InitialState = prevState;
                InitialStateSet = true;
            }

            ResumePlaying = true;

            // Make sure the client knows the playing item, to avoid duplicate requests.
            if (!request.PlaylistItemId.Equals(context.PlayQueue.GetPlayingItemPlaylistId(), StringComparison.OrdinalIgnoreCase))
            {
                Logger.LogDebug("HandleRequest: {0} in group {1}, client provided the wrong playlist identifier.", request.Type, context.GroupId.ToString());
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
                IGroupState newState = prevState switch
                {
                    GroupStateType.Playing => new PlayingGroupState(Logger),
                    GroupStateType.Paused => new PausedGroupState(Logger),
                    _ => new IdleGroupState(Logger)
                };

                context.SetState(newState);

                Logger.LogDebug("HandleRequest: {0} in group {1}, no next track available.", request.Type, context.GroupId.ToString());
            }
        }

        /// <inheritdoc />
        public override void HandleRequest(IGroupStateContext context, GroupStateType prevState, PreviousTrackGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            // Save state if first event.
            if (!InitialStateSet)
            {
                InitialState = prevState;
                InitialStateSet = true;
            }

            ResumePlaying = true;

            // Make sure the client knows the playing item, to avoid duplicate requests.
            if (!request.PlaylistItemId.Equals(context.PlayQueue.GetPlayingItemPlaylistId(), StringComparison.OrdinalIgnoreCase))
            {
                Logger.LogDebug("HandleRequest: {0} in group {1}, client provided the wrong playlist identifier.", request.Type, context.GroupId.ToString());
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
                IGroupState newState = prevState switch
                {
                    GroupStateType.Playing => new PlayingGroupState(Logger),
                    GroupStateType.Paused => new PausedGroupState(Logger),
                    _ => new IdleGroupState(Logger)
                };

                context.SetState(newState);

                Logger.LogDebug("HandleRequest: {0} in group {1}, no previous track available.", request.Type, context.GroupId.ToString());
            }
        }
    }
}
