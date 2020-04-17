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
        private enum BroadcastType
        {
            AllGroup = 0,
            SingleSession = 1,
            AllExceptSession = 2,
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

        private SessionInfo[] FilterSessions(SessionInfo from, BroadcastType type)
        {
            if (type == BroadcastType.SingleSession)
            {
                return new SessionInfo[] { from };
            }
            else if (type == BroadcastType.AllGroup)
            {
                return _group.Partecipants.Values.Select(
                    session => session.Session
                ).ToArray();
            }
            else if (type == BroadcastType.AllExceptSession)
            {
                return _group.Partecipants.Values.Select(
                    session => session.Session
                ).Where(
                    session => !session.Id.Equals(from.Id)
                ).ToArray();
            }
            else if (type == BroadcastType.AllReady)
            {
                return _group.Partecipants.Values.Where(
                    session => !session.IsBuffering
                ).Select(
                    session => session.Session
                ).ToArray();
            }
            else
            {
                return new SessionInfo[] {};
            }
        }

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

        private SendCommand NewSyncplayCommand(SendCommandType type)
        {
            var command = new SendCommand();
            command.GroupId = _group.GroupId.ToString();
            command.Command = type;
            command.PositionTicks = _group.PositionTicks;
            command.When = _group.LastActivity.ToUniversalTime().ToString("o");
            command.EmittedAt = DateTime.UtcNow.ToUniversalTime().ToString("o");
            return command;
        }

        private GroupUpdate<T> NewSyncplayGroupUpdate<T>(GroupUpdateType type, T data)
        {
            var command = new GroupUpdate<T>();
            command.GroupId = _group.GroupId.ToString();
            command.Type = type;
            command.Data = data;
            return command;
        }

        /// <inheritdoc />
        public void InitGroup(SessionInfo session)
        {
            _group.AddSession(session);
            _syncplayManager.MapSessionToGroup(session, this);

            _group.PlayingItem = session.FullNowPlayingItem;
            _group.IsPaused = true;
            _group.PositionTicks = session.PlayState.PositionTicks ??= 0;
            _group.LastActivity = DateTime.UtcNow;

            var updateSession = NewSyncplayGroupUpdate(GroupUpdateType.GroupJoined, DateTime.UtcNow.ToUniversalTime().ToString("o"));
            SendGroupUpdate(session, BroadcastType.SingleSession, updateSession);
            var pauseCommand = NewSyncplayCommand(SendCommandType.Pause);
            SendCommand(session, BroadcastType.SingleSession, pauseCommand);
        }

        /// <inheritdoc />
        public void SessionJoin(SessionInfo session, JoinGroupRequest request)
        {
            if (session.NowPlayingItem != null &&
                session.NowPlayingItem.Id.Equals(_group.PlayingItem.Id) &&
                request.PlayingItemId.Equals(_group.PlayingItem.Id))
            {
                _group.AddSession(session);
                _syncplayManager.MapSessionToGroup(session, this);

                var updateSession = NewSyncplayGroupUpdate(GroupUpdateType.GroupJoined, DateTime.UtcNow.ToUniversalTime().ToString("o"));
                SendGroupUpdate(session, BroadcastType.SingleSession, updateSession);

                var updateOthers = NewSyncplayGroupUpdate(GroupUpdateType.UserJoined, session.UserName);
                SendGroupUpdate(session, BroadcastType.AllExceptSession, updateOthers);

                // Client join and play, syncing will happen client side
                if (!_group.IsPaused)
                {
                    var playCommand = NewSyncplayCommand(SendCommandType.Play);
                    SendCommand(session, BroadcastType.SingleSession, playCommand);
                }
                else
                {
                    var pauseCommand = NewSyncplayCommand(SendCommandType.Pause);
                    SendCommand(session, BroadcastType.SingleSession, pauseCommand);
                }
            }
            else
            {
                var playRequest = new PlayRequest();
                playRequest.ItemIds = new Guid[] { _group.PlayingItem.Id };
                playRequest.StartPositionTicks = _group.PositionTicks;
                var update = NewSyncplayGroupUpdate(GroupUpdateType.PrepareSession, playRequest);
                SendGroupUpdate(session, BroadcastType.SingleSession, update);
            }
        }

        /// <inheritdoc />
        public void SessionLeave(SessionInfo session)
        {
            _group.RemoveSession(session);
            _syncplayManager.UnmapSessionFromGroup(session, this);

            var updateSession = NewSyncplayGroupUpdate(GroupUpdateType.GroupLeft, _group.PositionTicks);
            SendGroupUpdate(session, BroadcastType.SingleSession, updateSession);

            var updateOthers = NewSyncplayGroupUpdate(GroupUpdateType.UserLeft, session.UserName);
            SendGroupUpdate(session, BroadcastType.AllExceptSession, updateOthers);
        }

        /// <inheritdoc />
        public void HandleRequest(SessionInfo session, PlaybackRequest request)
        {
            if (request.Type.Equals(PlaybackRequestType.Play))
            {
                if (_group.IsPaused)
                {
                    var delay = _group.GetHighestPing() * 2;
                    delay = delay < _group.DefaulPing ? _group.DefaulPing : delay;

                    _group.IsPaused = false;
                    _group.LastActivity = DateTime.UtcNow.AddMilliseconds(
                        delay
                    );

                    var command = NewSyncplayCommand(SendCommandType.Play);
                    SendCommand(session, BroadcastType.AllGroup, command);
                }
                else
                {
                    // Client got lost
                    var command = NewSyncplayCommand(SendCommandType.Play);
                    SendCommand(session, BroadcastType.SingleSession, command);
                }
            }
            else if (request.Type.Equals(PlaybackRequestType.Pause))
            {
                if (!_group.IsPaused)
                {
                    _group.IsPaused = true;
                    var currentTime = DateTime.UtcNow;
                    var elapsedTime = currentTime - _group.LastActivity;
                    _group.LastActivity = currentTime;
                    _group.PositionTicks += elapsedTime.Ticks > 0 ? elapsedTime.Ticks : 0;

                    var command = NewSyncplayCommand(SendCommandType.Pause);
                    SendCommand(session, BroadcastType.AllGroup, command);
                }
                else
                {
                    var command = NewSyncplayCommand(SendCommandType.Pause);
                    SendCommand(session, BroadcastType.SingleSession, command);
                }
            }
            else if (request.Type.Equals(PlaybackRequestType.Seek))
            {
                // Sanitize PositionTicks
                var ticks = request.PositionTicks ??= 0;
                ticks = ticks >= 0 ? ticks : 0;
                if (_group.PlayingItem.RunTimeTicks != null)
                {
                    var runTimeTicks = _group.PlayingItem.RunTimeTicks ??= 0;
                    ticks = ticks > runTimeTicks ? runTimeTicks : ticks;
                }

                _group.IsPaused = true;
                _group.PositionTicks = ticks;
                _group.LastActivity = DateTime.UtcNow;

                var command = NewSyncplayCommand(SendCommandType.Seek);
                SendCommand(session, BroadcastType.AllGroup, command);
            }
            // TODO: client does not implement this yet
            else if (request.Type.Equals(PlaybackRequestType.Buffering))
            {
                if (!_group.IsPaused)
                {
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
                    SendGroupUpdate(session, BroadcastType.AllExceptSession, updateOthers);
                }
                else
                {
                    var command = NewSyncplayCommand(SendCommandType.Pause);
                    SendCommand(session, BroadcastType.SingleSession, command);
                }
            }
            // TODO: client does not implement this yet
            else if (request.Type.Equals(PlaybackRequestType.BufferingComplete))
            {
                if (_group.IsPaused)
                {
                    _group.SetBuffering(session, false);

                    if (_group.IsBuffering()) {
                        // Others are buffering, tell this client to pause when ready
                        var when = request.When ??= DateTime.UtcNow;
                        var currentTime = DateTime.UtcNow;
                        var elapsedTime = currentTime - when;
                        var clientPosition = TimeSpan.FromTicks(request.PositionTicks ??= 0) + elapsedTime;
                        var delay = _group.PositionTicks - clientPosition.Ticks;

                        var command = NewSyncplayCommand(SendCommandType.Pause);
                        command.When = currentTime.AddMilliseconds(
                            delay
                        ).ToUniversalTime().ToString("o");
                        SendCommand(session, BroadcastType.SingleSession, command);
                    }
                    else
                    {
                        // Let other clients resume as soon as the buffering client catches up
                        var when = request.When ??= DateTime.UtcNow;
                        var currentTime = DateTime.UtcNow;
                        var elapsedTime = currentTime - when;
                        var clientPosition = TimeSpan.FromTicks(request.PositionTicks ??= 0) + elapsedTime;
                        var delay = _group.PositionTicks - clientPosition.Ticks;

                        _group.IsPaused = false;

                        if (delay > _group.GetHighestPing() * 2)
                        {
                            // Client that was buffering is recovering, notifying others to resume
                            _group.LastActivity = currentTime.AddMilliseconds(
                                delay
                            );
                            var command = NewSyncplayCommand(SendCommandType.Play);
                            SendCommand(session, BroadcastType.AllExceptSession, command);
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
                    // Make sure client has latest group state
                    var command = NewSyncplayCommand(SendCommandType.Play);
                    SendCommand(session, BroadcastType.SingleSession, command);
                }
            }
            else if (request.Type.Equals(PlaybackRequestType.UpdatePing))
            {
                _group.UpdatePing(session, request.Ping ??= _group.DefaulPing);
            }
        }

        /// <inheritdoc />
        public GroupInfoView GetInfo()
        {
            var info = new GroupInfoView();
            info.GroupId = GetGroupId().ToString();
            info.PlayingItemName = _group.PlayingItem.Name;
            info.PlayingItemId = _group.PlayingItem.Id.ToString();
            info.PositionTicks = _group.PositionTicks;
            info.Partecipants = _group.Partecipants.Values.Select(session => session.Session.UserName).ToArray();
            return info;
        }
    }
}
