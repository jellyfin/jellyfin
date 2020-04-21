using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Session;
using MediaBrowser.Controller.Syncplay;
using MediaBrowser.Model.Session;
using MediaBrowser.Model.Syncplay;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.Syncplay
{
    /// <summary>
    /// Class SyncplayController.
    /// </summary>
    public class SyncplayController : ISyncplayController, IDisposable
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
        /// The logger.
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// The session manager.
        /// </summary>
        private readonly ISessionManager _sessionManager;

        /// <summary>
        /// The syncplay manager.
        /// </summary>
        private readonly ISyncplayManager _syncplayManager;

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

        private bool _disposed = false;

        public SyncplayController(
            ILogger logger,
            ISessionManager sessionManager,
            ISyncplayManager syncplayManager)
        {
            _logger = logger;
            _sessionManager = sessionManager;
            _syncplayManager = syncplayManager;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and optionally managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
        }

        // TODO: use this somewhere
        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
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
                    return new SessionInfo[] { };
            }
        }

        /// <summary>
        /// Sends a GroupUpdate message to the interested sessions.
        /// </summary>
        /// <param name="from">The current session.</param>
        /// <param name="type">The filtering type.</param>
        /// <param name="message">The message to send.</param>
        /// <value>The task.</value>
        private Task SendGroupUpdate<T>(SessionInfo from, BroadcastType type, GroupUpdate<T> message)
        {
            IEnumerable<Task> GetTasks()
            {
                SessionInfo[] sessions = FilterSessions(from, type);
                foreach (var session in sessions)
                {
                    yield return _sessionManager.SendSyncplayGroupUpdate(session.Id.ToString(), message, CancellationToken.None);
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
        /// <value>The task.</value>
        private Task SendCommand(SessionInfo from, BroadcastType type, SendCommand message)
        {
            IEnumerable<Task> GetTasks()
            {
                SessionInfo[] sessions = FilterSessions(from, type);
                foreach (var session in sessions)
                {
                    yield return _sessionManager.SendSyncplayCommand(session.Id.ToString(), message, CancellationToken.None);
                }
            }

            return Task.WhenAll(GetTasks());
        }

        /// <summary>
        /// Builds a new playback command with some default values.
        /// </summary>
        /// <param name="type">The command type.</param>
        /// <value>The SendCommand.</value>
        private SendCommand NewSyncplayCommand(SendCommandType type)
        {
            return new SendCommand()
            {
                GroupId = _group.GroupId.ToString(),
                Command = type,
                PositionTicks = _group.PositionTicks,
                When = _group.LastActivity.ToUniversalTime().ToString("o"),
                EmittedAt = DateTime.UtcNow.ToUniversalTime().ToString("o")
            };
        }

        /// <summary>
        /// Builds a new group update message.
        /// </summary>
        /// <param name="type">The update type.</param>
        /// <param name="data">The data to send.</param>
        /// <value>The GroupUpdate.</value>
        private GroupUpdate<T> NewSyncplayGroupUpdate<T>(GroupUpdateType type, T data)
        {
            return new GroupUpdate<T>()
            {
                GroupId = _group.GroupId.ToString(),
                Type = type,
                Data = data
            };
        }

        /// <inheritdoc />
        public void InitGroup(SessionInfo session)
        {
            _group.AddSession(session);
            _syncplayManager.AddSessionToGroup(session, this);

            _group.PlayingItem = session.FullNowPlayingItem;
            _group.IsPaused = true;
            _group.PositionTicks = session.PlayState.PositionTicks ??= 0;
            _group.LastActivity = DateTime.UtcNow;

            var updateSession = NewSyncplayGroupUpdate(GroupUpdateType.GroupJoined, DateTime.UtcNow.ToUniversalTime().ToString("o"));
            SendGroupUpdate(session, BroadcastType.CurrentSession, updateSession);
            var pauseCommand = NewSyncplayCommand(SendCommandType.Pause);
            SendCommand(session, BroadcastType.CurrentSession, pauseCommand);
        }

        /// <inheritdoc />
        public void SessionJoin(SessionInfo session, JoinGroupRequest request)
        {
            if (session.NowPlayingItem?.Id == _group.PlayingItem.Id && request.PlayingItemId == _group.PlayingItem.Id)
            {
                _group.AddSession(session);
                _syncplayManager.AddSessionToGroup(session, this);

                var updateSession = NewSyncplayGroupUpdate(GroupUpdateType.GroupJoined, DateTime.UtcNow.ToUniversalTime().ToString("o"));
                SendGroupUpdate(session, BroadcastType.CurrentSession, updateSession);

                var updateOthers = NewSyncplayGroupUpdate(GroupUpdateType.UserJoined, session.UserName);
                SendGroupUpdate(session, BroadcastType.AllExceptCurrentSession, updateOthers);

                // Client join and play, syncing will happen client side
                if (!_group.IsPaused)
                {
                    var playCommand = NewSyncplayCommand(SendCommandType.Play);
                    SendCommand(session, BroadcastType.CurrentSession, playCommand);
                }
                else
                {
                    var pauseCommand = NewSyncplayCommand(SendCommandType.Pause);
                    SendCommand(session, BroadcastType.CurrentSession, pauseCommand);
                }
            }
            else
            {
                var playRequest = new PlayRequest();
                playRequest.ItemIds = new Guid[] { _group.PlayingItem.Id };
                playRequest.StartPositionTicks = _group.PositionTicks;
                var update = NewSyncplayGroupUpdate(GroupUpdateType.PrepareSession, playRequest);
                SendGroupUpdate(session, BroadcastType.CurrentSession, update);
            }
        }

        /// <inheritdoc />
        public void SessionLeave(SessionInfo session)
        {
            _group.RemoveSession(session);
            _syncplayManager.RemoveSessionFromGroup(session, this);

            var updateSession = NewSyncplayGroupUpdate(GroupUpdateType.GroupLeft, _group.PositionTicks);
            SendGroupUpdate(session, BroadcastType.CurrentSession, updateSession);

            var updateOthers = NewSyncplayGroupUpdate(GroupUpdateType.UserLeft, session.UserName);
            SendGroupUpdate(session, BroadcastType.AllExceptCurrentSession, updateOthers);
        }

        /// <inheritdoc />
        public void HandleRequest(SessionInfo session, PlaybackRequest request)
        {
            // The server's job is to mantain a consistent state to which clients refer to,
            // as also to notify clients of state changes.
            // The actual syncing of media playback happens client side.
            // Clients are aware of the server's time and use it to sync.
            switch (request.Type)
            {
                case PlaybackRequestType.Play:
                    HandlePlayRequest(session, request);
                    break;
                case PlaybackRequestType.Pause:
                    HandlePauseRequest(session, request);
                    break;
                case PlaybackRequestType.Seek:
                    HandleSeekRequest(session, request);
                    break;
                case PlaybackRequestType.Buffering:
                    HandleBufferingRequest(session, request);
                    break;
                case PlaybackRequestType.BufferingDone:
                    HandleBufferingDoneRequest(session, request);
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
        private void HandlePlayRequest(SessionInfo session, PlaybackRequest request)
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

                var command = NewSyncplayCommand(SendCommandType.Play);
                SendCommand(session, BroadcastType.AllGroup, command);
            }
            else
            {
                // Client got lost, sending current state
                var command = NewSyncplayCommand(SendCommandType.Play);
                SendCommand(session, BroadcastType.CurrentSession, command);
            }
        }

        /// <summary>
        /// Handles a pause action requested by a session.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="request">The pause action.</param>
        private void HandlePauseRequest(SessionInfo session, PlaybackRequest request)
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

                var command = NewSyncplayCommand(SendCommandType.Pause);
                SendCommand(session, BroadcastType.AllGroup, command);
            }
            else
            {
                // Client got lost, sending current state
                var command = NewSyncplayCommand(SendCommandType.Pause);
                SendCommand(session, BroadcastType.CurrentSession, command);
            }
        }

        /// <summary>
        /// Handles a seek action requested by a session.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="request">The seek action.</param>
        private void HandleSeekRequest(SessionInfo session, PlaybackRequest request)
        {
            // Sanitize PositionTicks
            var ticks = request.PositionTicks ??= 0;
            ticks = ticks >= 0 ? ticks : 0;
            if (_group.PlayingItem.RunTimeTicks != null)
            {
                var runTimeTicks = _group.PlayingItem.RunTimeTicks ??= 0;
                ticks = ticks > runTimeTicks ? runTimeTicks : ticks;
            }

            // Pause and seek
            _group.IsPaused = true;
            _group.PositionTicks = ticks;
            _group.LastActivity = DateTime.UtcNow;

            var command = NewSyncplayCommand(SendCommandType.Seek);
            SendCommand(session, BroadcastType.AllGroup, command);
        }

        /// <summary>
        /// Handles a buffering action requested by a session.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="request">The buffering action.</param>
        private void HandleBufferingRequest(SessionInfo session, PlaybackRequest request)
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
                var command = NewSyncplayCommand(SendCommandType.Pause);
                SendCommand(session, BroadcastType.AllReady, command);

                var updateOthers = NewSyncplayGroupUpdate(GroupUpdateType.GroupWait, session.UserName);
                SendGroupUpdate(session, BroadcastType.AllExceptCurrentSession, updateOthers);
            }
            else
            {
                // Client got lost, sending current state
                var command = NewSyncplayCommand(SendCommandType.Pause);
                SendCommand(session, BroadcastType.CurrentSession, command);
            }
        }

        /// <summary>
        /// Handles a buffering-done action requested by a session.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="request">The buffering-done action.</param>
        private void HandleBufferingDoneRequest(SessionInfo session, PlaybackRequest request)
        {
            if (_group.IsPaused)
            {
                _group.SetBuffering(session, false);

                var when = request.When ??= DateTime.UtcNow;
                var currentTime = DateTime.UtcNow;
                var elapsedTime = currentTime - when;
                var clientPosition = TimeSpan.FromTicks(request.PositionTicks ??= 0) + elapsedTime;
                var delay = _group.PositionTicks - clientPosition.Ticks;

                if (_group.IsBuffering())
                {
                    // Others are buffering, tell this client to pause when ready
                    var command = NewSyncplayCommand(SendCommandType.Pause);
                    command.When = currentTime.AddMilliseconds(
                        delay
                    ).ToUniversalTime().ToString("o");
                    SendCommand(session, BroadcastType.CurrentSession, command);
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
                        var command = NewSyncplayCommand(SendCommandType.Play);
                        SendCommand(session, BroadcastType.AllExceptCurrentSession, command);
                    }
                    else
                    {
                        // Client, that was buffering, resumed playback but did not update others in time
                        delay = _group.GetHighestPing() * 2;
                        delay = delay < _group.DefaulPing ? _group.DefaulPing : delay;

                        _group.LastActivity = currentTime.AddMilliseconds(
                            delay
                        );

                        var command = NewSyncplayCommand(SendCommandType.Play);
                        SendCommand(session, BroadcastType.AllGroup, command);
                    }
                }
            }
            else
            {
                // Group was not waiting, make sure client has latest state
                var command = NewSyncplayCommand(SendCommandType.Play);
                SendCommand(session, BroadcastType.CurrentSession, command);
            }
        }

        /// <summary>
        /// Updates ping of a session.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="request">The update.</param>
        private void HandlePingUpdateRequest(SessionInfo session, PlaybackRequest request)
        {
            // Collected pings are used to account for network latency when unpausing playback
            _group.UpdatePing(session, request.Ping ??= _group.DefaulPing);
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
                Participants = _group.Participants.Values.Select(session => session.Session.UserName).ToArray()
            };
        }
    }
}
