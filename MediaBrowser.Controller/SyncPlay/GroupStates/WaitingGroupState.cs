#nullable disable

using System;
using System.Threading;
using MediaBrowser.Controller.Session;
using MediaBrowser.Controller.SyncPlay.PlaybackRequests;
using MediaBrowser.Model.SyncPlay;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Controller.SyncPlay.GroupStates
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
        /// The logger.
        /// </summary>
        private readonly ILogger<WaitingGroupState> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="WaitingGroupState"/> class.
        /// </summary>
        /// <param name="loggerFactory">Instance of the <see cref="ILoggerFactory"/> interface.</param>
        public WaitingGroupState(ILoggerFactory loggerFactory)
            : base(loggerFactory)
        {
            _logger = LoggerFactory.CreateLogger<WaitingGroupState>();
        }

        /// <inheritdoc />
        public override GroupStateType Type { get; } = GroupStateType.Waiting;

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
                    _logger.LogDebug("Session {SessionId} left group {GroupId}, notifying others to resume.", session.Id, context.GroupId.ToString());

                    // Client, that was buffering, left the group.
                    var playingState = new PlayingGroupState(LoggerFactory);
                    context.SetState(playingState);
                    var unpauseRequest = new UnpauseGroupRequest();
                    playingState.HandleRequest(unpauseRequest, context, Type, session, cancellationToken);
                }
                else
                {
                    _logger.LogDebug("Session {SessionId} left group {GroupId}, returning to previous state.", session.Id, context.GroupId.ToString());

                    // Group is ready, returning to previous state.
                    var pausedState = new PausedGroupState(LoggerFactory);
                    context.SetState(pausedState);
                }
            }
        }

        /// <inheritdoc />
        public override void HandleRequest(PlayGroupRequest request, IGroupStateContext context, GroupStateType prevState, SessionInfo session, CancellationToken cancellationToken)
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
                _logger.LogError("Unable to set playing queue in group {GroupId}.", context.GroupId.ToString());

                // Ignore request and return to previous state.
                IGroupState newState = prevState switch {
                    GroupStateType.Playing => new PlayingGroupState(LoggerFactory),
                    GroupStateType.Paused => new PausedGroupState(LoggerFactory),
                    _ => new IdleGroupState(LoggerFactory)
                };

                context.SetState(newState);
                return;
            }

            var playQueueUpdate = context.GetPlayQueueUpdate(PlayQueueUpdateReason.NewPlaylist);
            var update = context.NewSyncPlayGroupUpdate(GroupUpdateType.PlayQueue, playQueueUpdate);
            context.SendGroupUpdate(session, SyncPlayBroadcastType.AllGroup, update, cancellationToken);

            // Reset status of sessions and await for all Ready events.
            context.SetAllBuffering(true);

            _logger.LogDebug("Session {SessionId} set a new play queue in group {GroupId}.", session.Id, context.GroupId.ToString());
        }

        /// <inheritdoc />
        public override void HandleRequest(SetPlaylistItemGroupRequest request, IGroupStateContext context, GroupStateType prevState, SessionInfo session, CancellationToken cancellationToken)
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
                    GroupStateType.Playing => new PlayingGroupState(LoggerFactory),
                    GroupStateType.Paused => new PausedGroupState(LoggerFactory),
                    _ => new IdleGroupState(LoggerFactory)
                };

                context.SetState(newState);

                _logger.LogDebug("Unable to change current playing item in group {GroupId}.", context.GroupId.ToString());
            }
        }

        /// <inheritdoc />
        public override void HandleRequest(UnpauseGroupRequest request, IGroupStateContext context, GroupStateType prevState, SessionInfo session, CancellationToken cancellationToken)
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

                _logger.LogDebug("Group {GroupId} is waiting for all ready events.", context.GroupId.ToString());
            }
            else
            {
                if (ResumePlaying)
                {
                    _logger.LogDebug("Forcing the playback to start in group {GroupId}. Group-wait is disabled until next state change.", context.GroupId.ToString());

                    // An Unpause request is forcing the playback to start, ignoring sessions that are not ready.
                    context.SetAllBuffering(false);

                    // Change state.
                    var playingState = new PlayingGroupState(LoggerFactory)
                    {
                        IgnoreBuffering = true
                    };
                    context.SetState(playingState);
                    playingState.HandleRequest(request, context, Type, session, cancellationToken);
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
        public override void HandleRequest(PauseGroupRequest request, IGroupStateContext context, GroupStateType prevState, SessionInfo session, CancellationToken cancellationToken)
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
        public override void HandleRequest(StopGroupRequest request, IGroupStateContext context, GroupStateType prevState, SessionInfo session, CancellationToken cancellationToken)
        {
            // Save state if first event.
            if (!InitialStateSet)
            {
                InitialState = prevState;
                InitialStateSet = true;
            }

            // Change state.
            var idleState = new IdleGroupState(LoggerFactory);
            context.SetState(idleState);
            idleState.HandleRequest(request, context, Type, session, cancellationToken);
        }

        /// <inheritdoc />
        public override void HandleRequest(SeekGroupRequest request, IGroupStateContext context, GroupStateType prevState, SessionInfo session, CancellationToken cancellationToken)
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
        public override void HandleRequest(BufferGroupRequest request, IGroupStateContext context, GroupStateType prevState, SessionInfo session, CancellationToken cancellationToken)
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
                _logger.LogDebug("Session {SessionId} reported wrong playlist item in group {GroupId}.", session.Id, context.GroupId.ToString());

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
        public override void HandleRequest(ReadyGroupRequest request, IGroupStateContext context, GroupStateType prevState, SessionInfo session, CancellationToken cancellationToken)
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
                _logger.LogDebug("Session {SessionId} reported wrong playlist item in group {GroupId}.", session.Id, context.GroupId.ToString());

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
                _logger.LogWarning("Session {SessionId} is not time syncing properly. Ignoring elapsed time.", session.Id);

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

            _logger.LogDebug("Session {SessionId} is at {PositionTicks} (delay of {Delay} seconds) in group {GroupId}.", session.Id, clientPosition, TimeSpan.FromTicks(delayTicks).TotalSeconds, context.GroupId.ToString());

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

                    _logger.LogWarning("Session {SessionId} got lost in time, correcting.", session.Id);
                    return;
                }

                // Session is ready.
                context.SetBuffering(session, false);

                if (context.IsBuffering())
                {
                    // Others are still buffering, tell this client to pause when ready.
                    var command = context.NewSyncPlayCommand(SendCommandType.Pause);
                    command.When = currentTime.AddTicks(delayTicks);
                    context.SendCommand(session, SyncPlayBroadcastType.CurrentSession, command, cancellationToken);

                    _logger.LogInformation("Session {SessionId} will pause when ready in {Delay} seconds. Group {GroupId} is waiting for all ready events.", session.Id, TimeSpan.FromTicks(delayTicks).TotalSeconds, context.GroupId.ToString());
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

                        _logger.LogInformation("Session {SessionId} is recovering, group {GroupId} will resume in {Delay} seconds.", session.Id, context.GroupId.ToString(), TimeSpan.FromTicks(delayTicks).TotalSeconds);
                    }
                    else
                    {
                        // Client, that was buffering, resumed playback but did not update others in time.
                        delayTicks = context.GetHighestPing() * 2 * TimeSpan.TicksPerMillisecond;
                        delayTicks = Math.Max(delayTicks, context.DefaultPing);

                        context.LastActivity = currentTime.AddTicks(delayTicks);

                        var command = context.NewSyncPlayCommand(SendCommandType.Unpause);
                        context.SendCommand(session, SyncPlayBroadcastType.AllGroup, command, cancellationToken);

                        _logger.LogWarning("Session {SessionId} resumed playback, group {GroupId} has {Delay} seconds to recover.", session.Id, context.GroupId.ToString(), TimeSpan.FromTicks(delayTicks).TotalSeconds);
                    }

                    // Change state.
                    var playingState = new PlayingGroupState(LoggerFactory);
                    context.SetState(playingState);
                    playingState.HandleRequest(request, context, Type, session, cancellationToken);
                }
            }
            else
            {
                // Check that session is really ready, tolerate player imperfections under a certain threshold.
                if (Math.Abs(context.PositionTicks - requestTicks) > maxPlaybackOffsetTicks)
                {
                    // Session still not ready.
                    context.SetBuffering(session, true);
                    // Session is seeking to wrong position, correcting.
                    var command = context.NewSyncPlayCommand(SendCommandType.Seek);
                    context.SendCommand(session, SyncPlayBroadcastType.CurrentSession, command, cancellationToken);

                    // Notify relevant state change event.
                    SendGroupStateUpdate(context, request, session, cancellationToken);

                    _logger.LogWarning("Session {SessionId} is seeking to wrong position, correcting.", session.Id);
                    return;
                }

                // Session is ready.
                context.SetBuffering(session, false);

                if (!context.IsBuffering())
                {
                    _logger.LogDebug("Session {SessionId} is ready, group {GroupId} is ready.", session.Id, context.GroupId.ToString());

                    // Group is ready, returning to previous state.
                    var pausedState = new PausedGroupState(LoggerFactory);
                    context.SetState(pausedState);

                    if (InitialState.Equals(GroupStateType.Playing))
                    {
                        // Group went from playing to waiting state and a pause request occurred while waiting.
                        var pauseRequest = new PauseGroupRequest();
                        pausedState.HandleRequest(pauseRequest, context, Type, session, cancellationToken);
                    }
                    else if (InitialState.Equals(GroupStateType.Paused))
                    {
                        pausedState.HandleRequest(request, context, Type, session, cancellationToken);
                    }
                }
            }
        }

        /// <inheritdoc />
        public override void HandleRequest(NextItemGroupRequest request, IGroupStateContext context, GroupStateType prevState, SessionInfo session, CancellationToken cancellationToken)
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
                _logger.LogDebug("Session {SessionId} provided the wrong playlist item for group {GroupId}.", session.Id, context.GroupId.ToString());
                return;
            }

            var newItem = context.NextItemInQueue();
            if (newItem)
            {
                // Send playing-queue update.
                var playQueueUpdate = context.GetPlayQueueUpdate(PlayQueueUpdateReason.NextItem);
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
                    GroupStateType.Playing => new PlayingGroupState(LoggerFactory),
                    GroupStateType.Paused => new PausedGroupState(LoggerFactory),
                    _ => new IdleGroupState(LoggerFactory)
                };

                context.SetState(newState);

                _logger.LogDebug("No next item available in group {GroupId}.", context.GroupId.ToString());
            }
        }

        /// <inheritdoc />
        public override void HandleRequest(PreviousItemGroupRequest request, IGroupStateContext context, GroupStateType prevState, SessionInfo session, CancellationToken cancellationToken)
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
                _logger.LogDebug("Session {SessionId} provided the wrong playlist item for group {GroupId}.", session.Id, context.GroupId.ToString());
                return;
            }

            var newItem = context.PreviousItemInQueue();
            if (newItem)
            {
                // Send playing-queue update.
                var playQueueUpdate = context.GetPlayQueueUpdate(PlayQueueUpdateReason.PreviousItem);
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
                    GroupStateType.Playing => new PlayingGroupState(LoggerFactory),
                    GroupStateType.Paused => new PausedGroupState(LoggerFactory),
                    _ => new IdleGroupState(LoggerFactory)
                };

                context.SetState(newState);

                _logger.LogDebug("No previous item available in group {GroupId}.", context.GroupId.ToString());
            }
        }

        /// <inheritdoc />
        public override void HandleRequest(IgnoreWaitGroupRequest request, IGroupStateContext context, GroupStateType prevState, SessionInfo session, CancellationToken cancellationToken)
        {
            context.SetIgnoreGroupWait(session, request.IgnoreWait);

            if (!context.IsBuffering())
            {
                _logger.LogDebug("Ignoring session {SessionId}, group {GroupId} is ready.", session.Id, context.GroupId.ToString());

                if (ResumePlaying)
                {
                    // Client, that was buffering, stopped following playback.
                    var playingState = new PlayingGroupState(LoggerFactory);
                    context.SetState(playingState);
                    var unpauseRequest = new UnpauseGroupRequest();
                    playingState.HandleRequest(unpauseRequest, context, Type, session, cancellationToken);
                }
                else
                {
                    // Group is ready, returning to previous state.
                    var pausedState = new PausedGroupState(LoggerFactory);
                    context.SetState(pausedState);
                }
            }
        }
    }
}
