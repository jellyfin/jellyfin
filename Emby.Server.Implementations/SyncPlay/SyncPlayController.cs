using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Session;
using MediaBrowser.Controller.SyncPlay;
using MediaBrowser.Model.Session;
using MediaBrowser.Model.SyncPlay;

namespace Emby.Server.Implementations.SyncPlay
{
    /// <summary>
    /// Class SyncPlayController.
    /// </summary>
    /// <remarks>
    /// Class is not thread-safe, external locking is required when accessing methods.
    /// </remarks>
    public class SyncPlayController : ISyncPlayController
    {
        /// <summary>
        /// Used to filter the sessions of a group.
        /// </summary>
        private enum BroadcastType
        {
            /// <summary>
            /// All sessions will receive the message.
            /// </summary>
            AllGroup = 0,
            /// <summary>
            /// Only the specified session will receive the message.
            /// </summary>
            CurrentSession = 1,
            /// <summary>
            /// All sessions, except the current one, will receive the message.
            /// </summary>
            AllExceptCurrentSession = 2,
            /// <summary>
            /// Only sessions that are not buffering will receive the message.
            /// </summary>
            AllReady = 3
        }

        /// <summary>
        /// The session manager.
        /// </summary>
        private readonly ISessionManager _sessionManager;

        /// <summary>
        /// The SyncPlay manager.
        /// </summary>
        private readonly ISyncPlayManager _syncPlayManager;

        /// <summary>
        /// The group to manage.
        /// </summary>
        private readonly GroupInfo _group = new GroupInfo();

        /// <inheritdoc />
        public Guid GetGroupId() => _group.GroupId;

        /// <inheritdoc />
        public Guid GetPlayingItemId() => _group.PlayingItem.Id;

        /// <inheritdoc />
        public bool IsGroupEmpty() => _group.IsEmpty();

        /// <summary>
        /// Initializes a new instance of the <see cref="SyncPlayController" /> class.
        /// </summary>
        /// <param name="sessionManager">The session manager.</param>
        /// <param name="syncPlayManager">The SyncPlay manager.</param>
        public SyncPlayController(
            ISessionManager sessionManager,
            ISyncPlayManager syncPlayManager)
        {
            _sessionManager = sessionManager;
            _syncPlayManager = syncPlayManager;
        }

        /// <summary>
        /// Converts DateTime to UTC string.
        /// </summary>
        /// <param name="date">The date to convert.</param>
        /// <value>The UTC string.</value>
        private string DateToUTCString(DateTime date)
        {
            return date.ToUniversalTime().ToString("o");
        }

        /// <summary>
        /// Filters sessions of this group.
        /// </summary>
        /// <param name="from">The current session.</param>
        /// <param name="type">The filtering type.</param>
        /// <value>The array of sessions matching the filter.</value>
        private SessionInfo[] FilterSessions(SessionInfo from, BroadcastType type)
        {
            switch (type)
            {
                case BroadcastType.CurrentSession:
                    return new SessionInfo[] { from };
                case BroadcastType.AllGroup:
                    return _group.Participants.Values.Select(
                        session => session.Session
                    ).ToArray();
                case BroadcastType.AllExceptCurrentSession:
                    return _group.Participants.Values.Select(
                        session => session.Session
                    ).Where(
                        session => !session.Id.Equals(from.Id)
                    ).ToArray();
                case BroadcastType.AllReady:
                    return _group.Participants.Values.Where(
                        session => !session.IsBuffering
                    ).Select(
                        session => session.Session
                    ).ToArray();
                default:
                    return Array.Empty<SessionInfo>();
            }
        }

        /// <summary>
        /// Sends a GroupUpdate message to the interested sessions.
        /// </summary>
        /// <param name="from">The current session.</param>
        /// <param name="type">The filtering type.</param>
        /// <param name="message">The message to send.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <value>The task.</value>
        private Task SendGroupUpdate<T>(SessionInfo from, BroadcastType type, GroupUpdate<T> message, CancellationToken cancellationToken)
        {
            IEnumerable<Task> GetTasks()
            {
                SessionInfo[] sessions = FilterSessions(from, type);
                foreach (var session in sessions)
                {
                    yield return _sessionManager.SendSyncPlayGroupUpdate(session.Id.ToString(), message, cancellationToken);
                }
            }

            return Task.WhenAll(GetTasks());
        }

        /// <summary>
        /// Sends a playback command to the interested sessions.
        /// </summary>
        /// <param name="from">The current session.</param>
        /// <param name="type">The filtering type.</param>
        /// <param name="message">The message to send.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <value>The task.</value>
        private Task SendCommand(SessionInfo from, BroadcastType type, SendCommand message, CancellationToken cancellationToken)
        {
            IEnumerable<Task> GetTasks()
            {
                SessionInfo[] sessions = FilterSessions(from, type);
                foreach (var session in sessions)
                {
                    yield return _sessionManager.SendSyncPlayCommand(session.Id.ToString(), message, cancellationToken);
                }
            }

            return Task.WhenAll(GetTasks());
        }

        /// <summary>
        /// Builds a new playback command with some default values.
        /// </summary>
        /// <param name="type">The command type.</param>
        /// <value>The SendCommand.</value>
        private SendCommand NewSyncPlayCommand(SendCommandType type)
        {
            return new SendCommand()
            {
                GroupId = _group.GroupId.ToString(),
                Command = type,
                PositionTicks = _group.PositionTicks,
                When = DateToUTCString(_group.LastActivity),
                EmittedAt = DateToUTCString(DateTime.UtcNow)
            };
        }

        /// <summary>
        /// Builds a new group update message.
        /// </summary>
        /// <param name="type">The update type.</param>
        /// <param name="data">The data to send.</param>
        /// <value>The GroupUpdate.</value>
        private GroupUpdate<T> NewSyncPlayGroupUpdate<T>(GroupUpdateType type, T data)
        {
            return new GroupUpdate<T>()
            {
                GroupId = _group.GroupId.ToString(),
                Type = type,
                Data = data
            };
        }

        /// <inheritdoc />
        public void InitGroup(SessionInfo session, CancellationToken cancellationToken)
        {
            _group.AddSession(session);
            _syncPlayManager.AddSessionToGroup(session, this);

            _group.PlayingItem = session.FullNowPlayingItem;
            _group.IsPaused = true;
            _group.PositionTicks = session.PlayState.PositionTicks ?? 0;
            _group.LastActivity = DateTime.UtcNow;

            var updateSession = NewSyncPlayGroupUpdate(GroupUpdateType.GroupJoined, DateToUTCString(DateTime.UtcNow));
            SendGroupUpdate(session, BroadcastType.CurrentSession, updateSession, cancellationToken);
            var pauseCommand = NewSyncPlayCommand(SendCommandType.Pause);
            SendCommand(session, BroadcastType.CurrentSession, pauseCommand, cancellationToken);
        }

        /// <inheritdoc />
        public void SessionJoin(SessionInfo session, JoinGroupRequest request, CancellationToken cancellationToken)
        {
            if (session.NowPlayingItem?.Id == _group.PlayingItem.Id && request.PlayingItemId == _group.PlayingItem.Id)
            {
                _group.AddSession(session);
                _syncPlayManager.AddSessionToGroup(session, this);

                var updateSession = NewSyncPlayGroupUpdate(GroupUpdateType.GroupJoined, DateToUTCString(DateTime.UtcNow));
                SendGroupUpdate(session, BroadcastType.CurrentSession, updateSession, cancellationToken);

                var updateOthers = NewSyncPlayGroupUpdate(GroupUpdateType.UserJoined, session.UserName);
                SendGroupUpdate(session, BroadcastType.AllExceptCurrentSession, updateOthers, cancellationToken);

                // Client join and play, syncing will happen client side
                if (!_group.IsPaused)
                {
                    var playCommand = NewSyncPlayCommand(SendCommandType.Play);
                    SendCommand(session, BroadcastType.CurrentSession, playCommand, cancellationToken);
                }
                else
                {
                    var pauseCommand = NewSyncPlayCommand(SendCommandType.Pause);
                    SendCommand(session, BroadcastType.CurrentSession, pauseCommand, cancellationToken);
                }
            }
            else
            {
                var playRequest = new PlayRequest();
                playRequest.ItemIds = new Guid[] { _group.PlayingItem.Id };
                playRequest.StartPositionTicks = _group.PositionTicks;
                var update = NewSyncPlayGroupUpdate(GroupUpdateType.PrepareSession, playRequest);
                SendGroupUpdate(session, BroadcastType.CurrentSession, update, cancellationToken);
            }
        }

        /// <inheritdoc />
        public void SessionLeave(SessionInfo session, CancellationToken cancellationToken)
        {
            _group.RemoveSession(session);
            _syncPlayManager.RemoveSessionFromGroup(session, this);

            var updateSession = NewSyncPlayGroupUpdate(GroupUpdateType.GroupLeft, _group.PositionTicks);
            SendGroupUpdate(session, BroadcastType.CurrentSession, updateSession, cancellationToken);

            var updateOthers = NewSyncPlayGroupUpdate(GroupUpdateType.UserLeft, session.UserName);
            SendGroupUpdate(session, BroadcastType.AllExceptCurrentSession, updateOthers, cancellationToken);
        }

        /// <inheritdoc />
        public void HandleRequest(SessionInfo session, PlaybackRequest request, CancellationToken cancellationToken)
        {
            // The server's job is to mantain a consistent state to which clients refer to,
            // as also to notify clients of state changes.
            // The actual syncing of media playback happens client side.
            // Clients are aware of the server's time and use it to sync.
            switch (request.Type)
            {
                case PlaybackRequestType.Play:
                    HandlePlayRequest(session, request, cancellationToken);
                    break;
                case PlaybackRequestType.Pause:
                    HandlePauseRequest(session, request, cancellationToken);
                    break;
                case PlaybackRequestType.Seek:
                    HandleSeekRequest(session, request, cancellationToken);
                    break;
                case PlaybackRequestType.Buffering:
                    HandleBufferingRequest(session, request, cancellationToken);
                    break;
                case PlaybackRequestType.BufferingDone:
                    HandleBufferingDoneRequest(session, request, cancellationToken);
                    break;
                case PlaybackRequestType.UpdatePing:
                    HandlePingUpdateRequest(session, request);
                    break;
            }
        }

        /// <summary>
        /// Handles a play action requested by a session.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="request">The play action.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        private void HandlePlayRequest(SessionInfo session, PlaybackRequest request, CancellationToken cancellationToken)
        {
            if (_group.IsPaused)
            {
                // Pick a suitable time that accounts for latency
                var delay = _group.GetHighestPing() * 2;
                delay = delay < _group.DefaulPing ? _group.DefaulPing : delay;

                // Unpause group and set starting point in future
                // Clients will start playback at LastActivity (datetime) from PositionTicks (playback position)
                // The added delay does not guarantee, of course, that the command will be received in time
                // Playback synchronization will mainly happen client side
                _group.IsPaused = false;
                _group.LastActivity = DateTime.UtcNow.AddMilliseconds(
                    delay
                );

                var command = NewSyncPlayCommand(SendCommandType.Play);
                SendCommand(session, BroadcastType.AllGroup, command, cancellationToken);
            }
            else
            {
                // Client got lost, sending current state
                var command = NewSyncPlayCommand(SendCommandType.Play);
                SendCommand(session, BroadcastType.CurrentSession, command, cancellationToken);
            }
        }

        /// <summary>
        /// Handles a pause action requested by a session.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="request">The pause action.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        private void HandlePauseRequest(SessionInfo session, PlaybackRequest request, CancellationToken cancellationToken)
        {
            if (!_group.IsPaused)
            {
                // Pause group and compute the media playback position
                _group.IsPaused = true;
                var currentTime = DateTime.UtcNow;
                var elapsedTime = currentTime - _group.LastActivity;
                _group.LastActivity = currentTime;
                // Seek only if playback actually started
                // (a pause request may be issued during the delay added to account for latency)
                _group.PositionTicks += elapsedTime.Ticks > 0 ? elapsedTime.Ticks : 0;

                var command = NewSyncPlayCommand(SendCommandType.Pause);
                SendCommand(session, BroadcastType.AllGroup, command, cancellationToken);
            }
            else
            {
                // Client got lost, sending current state
                var command = NewSyncPlayCommand(SendCommandType.Pause);
                SendCommand(session, BroadcastType.CurrentSession, command, cancellationToken);
            }
        }

        /// <summary>
        /// Handles a seek action requested by a session.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="request">The seek action.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        private void HandleSeekRequest(SessionInfo session, PlaybackRequest request, CancellationToken cancellationToken)
        {
            // Sanitize PositionTicks
            var ticks = SanitizePositionTicks(request.PositionTicks);

            // Pause and seek
            _group.IsPaused = true;
            _group.PositionTicks = ticks;
            _group.LastActivity = DateTime.UtcNow;

            var command = NewSyncPlayCommand(SendCommandType.Seek);
            SendCommand(session, BroadcastType.AllGroup, command, cancellationToken);
        }

        /// <summary>
        /// Handles a buffering action requested by a session.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="request">The buffering action.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        private void HandleBufferingRequest(SessionInfo session, PlaybackRequest request, CancellationToken cancellationToken)
        {
            if (!_group.IsPaused)
            {
                // Pause group and compute the media playback position
                _group.IsPaused = true;
                var currentTime = DateTime.UtcNow;
                var elapsedTime = currentTime - _group.LastActivity;
                _group.LastActivity = currentTime;
                _group.PositionTicks += elapsedTime.Ticks > 0 ? elapsedTime.Ticks : 0;

                _group.SetBuffering(session, true);

                // Send pause command to all non-buffering sessions
                var command = NewSyncPlayCommand(SendCommandType.Pause);
                SendCommand(session, BroadcastType.AllReady, command, cancellationToken);

                var updateOthers = NewSyncPlayGroupUpdate(GroupUpdateType.GroupWait, session.UserName);
                SendGroupUpdate(session, BroadcastType.AllExceptCurrentSession, updateOthers, cancellationToken);
            }
            else
            {
                // Client got lost, sending current state
                var command = NewSyncPlayCommand(SendCommandType.Pause);
                SendCommand(session, BroadcastType.CurrentSession, command, cancellationToken);
            }
        }

        /// <summary>
        /// Handles a buffering-done action requested by a session.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="request">The buffering-done action.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        private void HandleBufferingDoneRequest(SessionInfo session, PlaybackRequest request, CancellationToken cancellationToken)
        {
            if (_group.IsPaused)
            {
                _group.SetBuffering(session, false);

                var requestTicks = SanitizePositionTicks(request.PositionTicks);

                var when = request.When ?? DateTime.UtcNow;
                var currentTime = DateTime.UtcNow;
                var elapsedTime = currentTime - when;
                var clientPosition = TimeSpan.FromTicks(requestTicks) + elapsedTime;
                var delay = _group.PositionTicks - clientPosition.Ticks;

                if (_group.IsBuffering())
                {
                    // Others are still buffering, tell this client to pause when ready
                    var command = NewSyncPlayCommand(SendCommandType.Pause);
                    var pauseAtTime = currentTime.AddMilliseconds(delay);
                    command.When = DateToUTCString(pauseAtTime);
                    SendCommand(session, BroadcastType.CurrentSession, command, cancellationToken);
                }
                else
                {
                    // Let other clients resume as soon as the buffering client catches up
                    _group.IsPaused = false;

                    if (delay > _group.GetHighestPing() * 2)
                    {
                        // Client that was buffering is recovering, notifying others to resume
                        _group.LastActivity = currentTime.AddMilliseconds(
                            delay
                        );
                        var command = NewSyncPlayCommand(SendCommandType.Play);
                        SendCommand(session, BroadcastType.AllExceptCurrentSession, command, cancellationToken);
                    }
                    else
                    {
                        // Client, that was buffering, resumed playback but did not update others in time
                        delay = _group.GetHighestPing() * 2;
                        delay = delay < _group.DefaulPing ? _group.DefaulPing : delay;

                        _group.LastActivity = currentTime.AddMilliseconds(
                            delay
                        );

                        var command = NewSyncPlayCommand(SendCommandType.Play);
                        SendCommand(session, BroadcastType.AllGroup, command, cancellationToken);
                    }
                }
            }
            else
            {
                // Group was not waiting, make sure client has latest state
                var command = NewSyncPlayCommand(SendCommandType.Play);
                SendCommand(session, BroadcastType.CurrentSession, command, cancellationToken);
            }
        }

        /// <summary>
        /// Sanitizes the PositionTicks, considers the current playing item when available.
        /// </summary>
        /// <param name="positionTicks">The PositionTicks.</param>
        /// <value>The sanitized PositionTicks.</value>
        private long SanitizePositionTicks(long? positionTicks)
        {
            var ticks = positionTicks ?? 0;
            ticks = ticks >= 0 ? ticks : 0;
            if (_group.PlayingItem != null)
            {
                var runTimeTicks = _group.PlayingItem.RunTimeTicks ?? 0;
                ticks = ticks > runTimeTicks ? runTimeTicks : ticks;
            }

            return ticks;
        }

        /// <summary>
        /// Updates ping of a session.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="request">The update.</param>
        private void HandlePingUpdateRequest(SessionInfo session, PlaybackRequest request)
        {
            // Collected pings are used to account for network latency when unpausing playback
            _group.UpdatePing(session, request.Ping ?? _group.DefaulPing);
        }

        /// <inheritdoc />
        public GroupInfoView GetInfo()
        {
            return new GroupInfoView()
            {
                GroupId = GetGroupId().ToString(),
                PlayingItemName = _group.PlayingItem.Name,
                PlayingItemId = _group.PlayingItem.Id.ToString(),
                PositionTicks = _group.PositionTicks,
                Participants = _group.Participants.Values.Select(session => session.Session.UserName).Distinct().ToList()
            };
        }
    }
}
