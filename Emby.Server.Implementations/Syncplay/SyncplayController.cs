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
            SingleUser = 1,
            AllExceptUser = 2,
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

        private SessionInfo[] FilterUsers(SessionInfo from, BroadcastType type)
        {
            if (type == BroadcastType.SingleUser)
            {
                return new SessionInfo[] { from };
            }
            else if (type == BroadcastType.AllGroup)
            {
                return _group.Partecipants.Values.Select(
                    user => user.Session
                ).ToArray();
            }
            else if (type == BroadcastType.AllExceptUser)
            {
                return _group.Partecipants.Values.Select(
                    user => user.Session
                ).Where(
                    user => !user.Id.Equals(from.Id)
                ).ToArray();
            }
            else if (type == BroadcastType.AllReady)
            {
                return _group.Partecipants.Values.Where(
                    user => !user.IsBuffering
                ).Select(
                    user => user.Session
                ).ToArray();
            }
            else
            {
                return new SessionInfo[] {};
            }
        }

        private Task SendGroupUpdate<T>(SessionInfo from, BroadcastType type, SyncplayGroupUpdate<T> message)
        {
            IEnumerable<Task> GetTasks()
            {
                SessionInfo[] users = FilterUsers(from, type);
                foreach (var user in users)
                {
                    yield return _sessionManager.SendSyncplayGroupUpdate(user.Id.ToString(), message, CancellationToken.None);
                }
            }

            return Task.WhenAll(GetTasks());
        }

        private Task SendCommand(SessionInfo from, BroadcastType type, SyncplayCommand message)
        {
            IEnumerable<Task> GetTasks()
            {
                SessionInfo[] users = FilterUsers(from, type);
                foreach (var user in users)
                {
                    yield return _sessionManager.SendSyncplayCommand(user.Id.ToString(), message, CancellationToken.None);
                }
            }

            return Task.WhenAll(GetTasks());
        }

        private SyncplayCommand NewSyncplayCommand(SyncplayCommandType type) {
            var command = new SyncplayCommand();
            command.GroupId = _group.GroupId.ToString();
            command.Command = type;
            command.PositionTicks = _group.PositionTicks;
            command.When = _group.LastActivity.ToUniversalTime().ToString("o");
            return command;
        }

        private SyncplayGroupUpdate<T> NewSyncplayGroupUpdate<T>(SyncplayGroupUpdateType type, T data)
        {
            var command = new SyncplayGroupUpdate<T>();
            command.GroupId = _group.GroupId.ToString();
            command.Type = type;
            command.Data = data;
            return command;
        }

        /// <inheritdoc />
        public void InitGroup(SessionInfo user)
        {
            _group.AddUser(user);
            _syncplayManager.MapUserToGroup(user, this);

            _group.PlayingItem = user.FullNowPlayingItem;
            _group.IsPaused = true;
            _group.PositionTicks = user.PlayState.PositionTicks ??= 0;
            _group.LastActivity = DateTime.UtcNow;

            var updateUser = NewSyncplayGroupUpdate(SyncplayGroupUpdateType.GroupJoined, DateTime.UtcNow.ToUniversalTime().ToString("o"));
            SendGroupUpdate(user, BroadcastType.SingleUser, updateUser);
            var pauseCommand = NewSyncplayCommand(SyncplayCommandType.Pause);
            SendCommand(user, BroadcastType.SingleUser, pauseCommand);
        }

        /// <inheritdoc />
        public void UserJoin(SessionInfo user)
        {
            if (user.NowPlayingItem != null && user.NowPlayingItem.Id.Equals(_group.PlayingItem.Id))
            {
                _group.AddUser(user);
                _syncplayManager.MapUserToGroup(user, this);

                var updateUser = NewSyncplayGroupUpdate(SyncplayGroupUpdateType.GroupJoined, _group.PositionTicks);
                SendGroupUpdate(user, BroadcastType.SingleUser, updateUser);

                var updateOthers = NewSyncplayGroupUpdate(SyncplayGroupUpdateType.UserJoined, user.UserName);
                SendGroupUpdate(user, BroadcastType.AllExceptUser, updateOthers);

                // Client join and play, syncing will happen client side
                if (!_group.IsPaused)
                {
                    var playCommand = NewSyncplayCommand(SyncplayCommandType.Play);
                    SendCommand(user, BroadcastType.SingleUser, playCommand);
                }
                else
                {
                    var pauseCommand = NewSyncplayCommand(SyncplayCommandType.Pause);
                    SendCommand(user, BroadcastType.SingleUser, pauseCommand);
                }
            }
            else
            {
                var playRequest = new PlayRequest();
                playRequest.ItemIds = new Guid[] { _group.PlayingItem.Id };
                playRequest.StartPositionTicks = _group.PositionTicks;
                var update = NewSyncplayGroupUpdate(SyncplayGroupUpdateType.PrepareSession, playRequest);
                SendGroupUpdate(user, BroadcastType.SingleUser, update);
            }
        }

        /// <inheritdoc />
        public void UserLeave(SessionInfo user)
        {
            _group.RemoveUser(user);
            _syncplayManager.UnmapUserFromGroup(user, this);

            var updateUser = NewSyncplayGroupUpdate(SyncplayGroupUpdateType.GroupLeft, _group.PositionTicks);
            SendGroupUpdate(user, BroadcastType.SingleUser, updateUser);

            var updateOthers = NewSyncplayGroupUpdate(SyncplayGroupUpdateType.UserLeft, user.UserName);
            SendGroupUpdate(user, BroadcastType.AllExceptUser, updateOthers);
        }

        /// <inheritdoc />
        public void HandleRequest(SessionInfo user, SyncplayRequestInfo request)
        {
            if (request.Type.Equals(SyncplayRequestType.Play))
            {
                if (_group.IsPaused)
                {
                    var delay = _group.GetHighestPing() * 2;
                    delay = delay < _group.DefaulPing ? _group.DefaulPing : delay;

                    _group.IsPaused = false;
                    _group.LastActivity = DateTime.UtcNow.AddMilliseconds(
                        delay
                    );

                    var command = NewSyncplayCommand(SyncplayCommandType.Play);
                    SendCommand(user, BroadcastType.AllGroup, command);
                }
                else
                {
                    // Client got lost
                    var command = NewSyncplayCommand(SyncplayCommandType.Play);
                    SendCommand(user, BroadcastType.SingleUser, command);
                }
            }
            else if (request.Type.Equals(SyncplayRequestType.Pause))
            {
                if (!_group.IsPaused)
                {
                    _group.IsPaused = true;
                    var currentTime = DateTime.UtcNow;
                    var elapsedTime = currentTime - _group.LastActivity;
                    _group.LastActivity = currentTime;
                    _group.PositionTicks += elapsedTime.Ticks > 0 ? elapsedTime.Ticks : 0;

                    var command = NewSyncplayCommand(SyncplayCommandType.Pause);
                    SendCommand(user, BroadcastType.AllGroup, command);
                }
                else
                {
                    var command = NewSyncplayCommand(SyncplayCommandType.Pause);
                    SendCommand(user, BroadcastType.SingleUser, command);
                }
            }
            else if (request.Type.Equals(SyncplayRequestType.Seek))
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

                var command = NewSyncplayCommand(SyncplayCommandType.Seek);
                SendCommand(user, BroadcastType.AllGroup, command);
            }
            // TODO: client does not implement this yet
            else if (request.Type.Equals(SyncplayRequestType.Buffering))
            {
                if (!_group.IsPaused)
                {
                    _group.IsPaused = true;
                    var currentTime = DateTime.UtcNow;
                    var elapsedTime = currentTime - _group.LastActivity;
                    _group.LastActivity = currentTime;
                    _group.PositionTicks += elapsedTime.Ticks > 0 ? elapsedTime.Ticks : 0;

                    _group.SetBuffering(user, true);

                    // Send pause command to all non-buffering users
                    var command = NewSyncplayCommand(SyncplayCommandType.Pause);
                    SendCommand(user, BroadcastType.AllReady, command);

                    var updateOthers = NewSyncplayGroupUpdate(SyncplayGroupUpdateType.GroupWait, user.UserName);
                    SendGroupUpdate(user, BroadcastType.AllExceptUser, updateOthers);
                }
                else
                {
                    var command = NewSyncplayCommand(SyncplayCommandType.Pause);
                    SendCommand(user, BroadcastType.SingleUser, command);
                }
            }
            // TODO: client does not implement this yet
            else if (request.Type.Equals(SyncplayRequestType.BufferingComplete))
            {
                if (_group.IsPaused)
                {
                    _group.SetBuffering(user, false);

                    if (_group.IsBuffering()) {
                        // Others are buffering, tell this client to pause when ready
                        var when = request.When ??= DateTime.UtcNow;
                        var currentTime = DateTime.UtcNow;
                        var elapsedTime = currentTime - when;
                        var clientPosition = TimeSpan.FromTicks(request.PositionTicks ??= 0) + elapsedTime;
                        var delay = _group.PositionTicks - clientPosition.Ticks;

                        var command = NewSyncplayCommand(SyncplayCommandType.Pause);
                        command.When = currentTime.AddMilliseconds(
                            delay
                        ).ToUniversalTime().ToString("o");
                        SendCommand(user, BroadcastType.SingleUser, command);
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
                            var command = NewSyncplayCommand(SyncplayCommandType.Play);
                            SendCommand(user, BroadcastType.AllExceptUser, command);
                        }
                        else
                        {
                            // Client, that was buffering, resumed playback but did not update others in time
                            delay = _group.GetHighestPing() * 2;
                            delay = delay < _group.DefaulPing ? _group.DefaulPing : delay;

                            _group.LastActivity = currentTime.AddMilliseconds(
                                delay
                            );

                            var command = NewSyncplayCommand(SyncplayCommandType.Play);
                            SendCommand(user, BroadcastType.AllGroup, command);
                        }
                    }                    
                }
                else
                {
                    // Make sure client has latest group state
                    var command = NewSyncplayCommand(SyncplayCommandType.Play);
                    SendCommand(user, BroadcastType.SingleUser, command);
                }
            }
            else if (request.Type.Equals(SyncplayRequestType.KeepAlive))
            {
                _group.UpdatePing(user, request.Ping ??= _group.DefaulPing);

                var keepAlive = new SyncplayGroupUpdate<string>();
                keepAlive.GroupId = _group.GroupId.ToString();
                keepAlive.Type = SyncplayGroupUpdateType.KeepAlive;
                SendGroupUpdate(user, BroadcastType.SingleUser, keepAlive);
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
            info.Partecipants = _group.Partecipants.Values.Select(user => user.Session.UserName).ToArray();
            return info;
        }
    }
}
