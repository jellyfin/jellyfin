using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Session;
using MediaBrowser.Controller.SyncPlay;
using MediaBrowser.Model.Session;
using MediaBrowser.Model.SyncPlay;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.SyncPlay
{
    /// <summary>
    /// Class SyncPlayController.
    /// </summary>
    /// <remarks>
    /// Class is not thread-safe, external locking is required when accessing methods.
    /// </remarks>
    public class SyncPlayController : ISyncPlayController, ISyncPlayStateContext
    {
        /// <summary>
        /// The session manager.
        /// </summary>
        private readonly ISessionManager _sessionManager;

        /// <summary>
        /// The SyncPlay manager.
        /// </summary>
        private readonly ISyncPlayManager _syncPlayManager;

        /// <summary>
        /// The logger.
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// The group to manage.
        /// </summary>
        private readonly GroupInfo _group = new GroupInfo();

        /// <summary>
        /// Internal group state.
        /// </summary>
        /// <value>The group's state.</value>
        private ISyncPlayState State = new PausedGroupState();

        /// <inheritdoc />
        public GroupInfo GetGroup()
        {
            return _group;
        }

        /// <inheritdoc />
        public void SetState(ISyncPlayState state)
        {
            _logger.LogInformation("SetState: {0} -> {1}.", State.GetGroupState(), state.GetGroupState());
            this.State = state;
        }

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
            ISyncPlayManager syncPlayManager,
            ILogger logger)
        {
            _sessionManager = sessionManager;
            _syncPlayManager = syncPlayManager;
            _logger = logger;
        }

        /// <summary>
        /// Filters sessions of this group.
        /// </summary>
        /// <param name="from">The current session.</param>
        /// <param name="type">The filtering type.</param>
        /// <value>The array of sessions matching the filter.</value>
        private SessionInfo[] FilterSessions(SessionInfo from, SyncPlayBroadcastType type)
        {
            switch (type)
            {
                case SyncPlayBroadcastType.CurrentSession:
                    return new SessionInfo[] { from };
                case SyncPlayBroadcastType.AllGroup:
                    return _group.Participants.Values.Select(
                        session => session.Session).ToArray();
                case SyncPlayBroadcastType.AllExceptCurrentSession:
                    return _group.Participants.Values.Select(
                        session => session.Session).Where(
                        session => !session.Id.Equals(from.Id)).ToArray();
                case SyncPlayBroadcastType.AllReady:
                    return _group.Participants.Values.Where(
                        session => !session.IsBuffering).Select(
                        session => session.Session).ToArray();
                default:
                    return Array.Empty<SessionInfo>();
            }
        }

        /// <inheritdoc />
        public Task SendGroupUpdate<T>(SessionInfo from, SyncPlayBroadcastType type, GroupUpdate<T> message, CancellationToken cancellationToken)
        {
            IEnumerable<Task> GetTasks()
            {
                foreach (var session in FilterSessions(from, type))
                {
                    yield return _sessionManager.SendSyncPlayGroupUpdate(session.Id, message, cancellationToken);
                }
            }

            return Task.WhenAll(GetTasks());
        }

        /// <inheritdoc />
        public Task SendCommand(SessionInfo from, SyncPlayBroadcastType type, SendCommand message, CancellationToken cancellationToken)
        {
            IEnumerable<Task> GetTasks()
            {
                foreach (var session in FilterSessions(from, type))
                {
                    yield return _sessionManager.SendSyncPlayCommand(session.Id, message, cancellationToken);
                }
            }

            return Task.WhenAll(GetTasks());
        }

        /// <inheritdoc />
        public SendCommand NewSyncPlayCommand(SendCommandType type)
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

        /// <inheritdoc />
        public GroupUpdate<T> NewSyncPlayGroupUpdate<T>(GroupUpdateType type, T data)
        {
            return new GroupUpdate<T>()
            {
                GroupId = _group.GroupId.ToString(),
                Type = type,
                Data = data
            };
        }

        /// <inheritdoc />
        public string DateToUTCString(DateTime _date)
        {
            return _date.ToUniversalTime().ToString("o");
        }

        /// <inheritdoc />
        public long SanitizePositionTicks(long? positionTicks)
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

        /// <inheritdoc />
        public void CreateGroup(SessionInfo session, CancellationToken cancellationToken)
        {
            _group.AddSession(session);
            _syncPlayManager.AddSessionToGroup(session, this);

            State = new PausedGroupState();

            _group.PlayingItem = session.FullNowPlayingItem;
            // TODO: looks like new groups should mantain playstate (and not force to pause)
            // _group.IsPaused = session.PlayState.IsPaused;
            _group.PositionTicks = session.PlayState.PositionTicks ?? 0;
            _group.LastActivity = DateTime.UtcNow;

            var updateSession = NewSyncPlayGroupUpdate(GroupUpdateType.GroupJoined, DateToUTCString(DateTime.UtcNow));
            SendGroupUpdate(session, SyncPlayBroadcastType.CurrentSession, updateSession, cancellationToken);
            // TODO: looks like new groups should mantain playstate (and not force to pause)
            var pauseCommand = NewSyncPlayCommand(SendCommandType.Pause);
            SendCommand(session, SyncPlayBroadcastType.CurrentSession, pauseCommand, cancellationToken);
        }

        /// <inheritdoc />
        public void SessionJoin(SessionInfo session, JoinGroupRequest request, CancellationToken cancellationToken)
        {
            if (session.NowPlayingItem?.Id == _group.PlayingItem.Id)
            {
                _group.AddSession(session);
                _syncPlayManager.AddSessionToGroup(session, this);

                var updateSession = NewSyncPlayGroupUpdate(GroupUpdateType.GroupJoined, DateToUTCString(DateTime.UtcNow));
                SendGroupUpdate(session, SyncPlayBroadcastType.CurrentSession, updateSession, cancellationToken);

                var updateOthers = NewSyncPlayGroupUpdate(GroupUpdateType.UserJoined, session.UserName);
                SendGroupUpdate(session, SyncPlayBroadcastType.AllExceptCurrentSession, updateOthers, cancellationToken);

                // Syncing will happen client-side
                if (State.GetGroupState().Equals(GroupState.Playing))
                {
                    var playCommand = NewSyncPlayCommand(SendCommandType.Play);
                    SendCommand(session, SyncPlayBroadcastType.CurrentSession, playCommand, cancellationToken);
                }
                else
                {
                    var pauseCommand = NewSyncPlayCommand(SendCommandType.Pause);
                    SendCommand(session, SyncPlayBroadcastType.CurrentSession, pauseCommand, cancellationToken);
                }
            }
            else
            {
                var playRequest = new PlayRequest
                {
                    ItemIds = new Guid[] { _group.PlayingItem.Id },
                    StartPositionTicks = _group.PositionTicks
                };
                var update = NewSyncPlayGroupUpdate(GroupUpdateType.PrepareSession, playRequest);
                SendGroupUpdate(session, SyncPlayBroadcastType.CurrentSession, update, cancellationToken);
            }
        }

        /// <inheritdoc />
        public void SessionLeave(SessionInfo session, CancellationToken cancellationToken)
        {
            _group.RemoveSession(session);
            _syncPlayManager.RemoveSessionFromGroup(session, this);

            var updateSession = NewSyncPlayGroupUpdate(GroupUpdateType.GroupLeft, _group.PositionTicks);
            SendGroupUpdate(session, SyncPlayBroadcastType.CurrentSession, updateSession, cancellationToken);

            var updateOthers = NewSyncPlayGroupUpdate(GroupUpdateType.UserLeft, session.UserName);
            SendGroupUpdate(session, SyncPlayBroadcastType.AllExceptCurrentSession, updateOthers, cancellationToken);
        }

        /// <inheritdoc />
        public void HandleRequest(SessionInfo session, IPlaybackGroupRequest request, CancellationToken cancellationToken)
        {
            // The server's job is to maintain a consistent state for clients to reference
            // and notify clients of state changes. The actual syncing of media playback
            // happens client side. Clients are aware of the server's time and use it to sync.
            _logger.LogInformation("HandleRequest: {0}:{1}.", request.GetType(), State.GetGroupState());
            _ = request.Apply(this, State, session, cancellationToken);
            // TODO: do something with returned value
        }

        /// <inheritdoc />
        public GroupInfoDto GetInfo()
        {
            return new GroupInfoDto()
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
