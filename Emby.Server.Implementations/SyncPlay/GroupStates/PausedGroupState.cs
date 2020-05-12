using System.Linq;
using System;
using System.Threading;
using System.Collections.Generic;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Session;
using MediaBrowser.Model.SyncPlay;

namespace MediaBrowser.Controller.SyncPlay
{
    /// <summary>
    /// Class PausedGroupState.
    /// </summary>
    /// <remarks>
    /// Class is not thread-safe, external locking is required when accessing methods.
    /// </remarks>
    public class PausedGroupState : SyncPlayAbstractState
    {
        /// <inheritdoc />
        public override GroupState GetGroupState()
        {
            return GroupState.Paused;
        }

        /// <inheritdoc />
        public override bool HandleRequest(ISyncPlayStateContext context, bool newState, PlayGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            // Change state
            var playingState = new PlayingGroupState();
            context.SetState(playingState);
            return playingState.HandleRequest(context, true, request, session, cancellationToken);
        }

        /// <inheritdoc />
        public override bool HandleRequest(ISyncPlayStateContext context, bool newState, PauseGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            if (newState)
            {
                GroupInfo group = context.GetGroup();

                // Pause group and compute the media playback position
                var currentTime = DateTime.UtcNow;
                var elapsedTime = currentTime - group.LastActivity;
                group.LastActivity = currentTime;
                // Seek only if playback actually started
                // Pause request may be issued during the delay added to account for latency
                group.PositionTicks += elapsedTime.Ticks > 0 ? elapsedTime.Ticks : 0;

                var command = context.NewSyncPlayCommand(SendCommandType.Pause);
                context.SendCommand(session, SyncPlayBroadcastType.AllGroup, command, cancellationToken);
            }
            else
            {
                // Client got lost, sending current state
                var command = context.NewSyncPlayCommand(SendCommandType.Pause);
                context.SendCommand(session, SyncPlayBroadcastType.CurrentSession, command, cancellationToken);
            }

            return true;
        }

        /// <inheritdoc />
        public override bool HandleRequest(ISyncPlayStateContext context, bool newState, SeekGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            GroupInfo group = context.GetGroup();

            // Sanitize PositionTicks
            var ticks = context.SanitizePositionTicks(request.PositionTicks);

            // Seek
            group.PositionTicks = ticks;
            group.LastActivity = DateTime.UtcNow;

            var command = context.NewSyncPlayCommand(SendCommandType.Seek);
            context.SendCommand(session, SyncPlayBroadcastType.AllGroup, command, cancellationToken);

            return true;
        }

        /// <inheritdoc />
        public override bool HandleRequest(ISyncPlayStateContext context, bool newState, BufferGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            GroupInfo group = context.GetGroup();

            if (newState)
            {
                // Pause group and compute the media playback position
                var currentTime = DateTime.UtcNow;
                var elapsedTime = currentTime - group.LastActivity;
                group.LastActivity = currentTime;
                group.PositionTicks += elapsedTime.Ticks > 0 ? elapsedTime.Ticks : 0;

                group.SetBuffering(session, true);

                // Send pause command to all non-buffering sessions
                var command = context.NewSyncPlayCommand(SendCommandType.Pause);
                context.SendCommand(session, SyncPlayBroadcastType.AllReady, command, cancellationToken);

                var updateOthers = context.NewSyncPlayGroupUpdate(GroupUpdateType.GroupWait, session.UserName);
                context.SendGroupUpdate(session, SyncPlayBroadcastType.AllExceptCurrentSession, updateOthers, cancellationToken);
            }
            else
            {
                // TODO: no idea?
                // group.SetBuffering(session, true);

                // Client got lost, sending current state
                var command = context.NewSyncPlayCommand(SendCommandType.Pause);
                context.SendCommand(session, SyncPlayBroadcastType.CurrentSession, command, cancellationToken);
            }

            return true;
        }

        /// <inheritdoc />
        public override bool HandleRequest(ISyncPlayStateContext context, bool newState, ReadyGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            GroupInfo group = context.GetGroup();

            group.SetBuffering(session, false);

            var requestTicks = context.SanitizePositionTicks(request.PositionTicks);

            var currentTime = DateTime.UtcNow;
            var elapsedTime = currentTime - request.When;
            var clientPosition = TimeSpan.FromTicks(requestTicks) + elapsedTime;
            var delay = group.PositionTicks - clientPosition.Ticks;

            if (group.IsBuffering())
            {
                // Others are still buffering, tell this client to pause when ready
                var command = context.NewSyncPlayCommand(SendCommandType.Pause);
                var pauseAtTime = currentTime.AddMilliseconds(delay);
                command.When = context.DateToUTCString(pauseAtTime);
                context.SendCommand(session, SyncPlayBroadcastType.CurrentSession, command, cancellationToken);
            }
            else
            {
                // Let other clients resume as soon as the buffering client catches up
                if (delay > group.GetHighestPing() * 2)
                {
                    // Client that was buffering is recovering, notifying others to resume
                    group.LastActivity = currentTime.AddMilliseconds(
                        delay
                    );
                    var command = context.NewSyncPlayCommand(SendCommandType.Play);
                    context.SendCommand(session, SyncPlayBroadcastType.AllExceptCurrentSession, command, cancellationToken);
                }
                else
                {
                    // Client, that was buffering, resumed playback but did not update others in time
                    delay = Math.Max(group.GetHighestPing() * 2, group.DefaultPing);

                    group.LastActivity = currentTime.AddMilliseconds(
                        delay
                    );

                    var command = context.NewSyncPlayCommand(SendCommandType.Play);
                    context.SendCommand(session, SyncPlayBroadcastType.AllGroup, command, cancellationToken);
                }

                // Change state
                var playingState = new PlayingGroupState();
                context.SetState(playingState);
            }

            return true;
        }
    }
}
