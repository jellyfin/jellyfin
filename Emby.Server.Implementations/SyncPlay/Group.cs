#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Entities;
using Jellyfin.Extensions;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using MediaBrowser.Controller.SyncPlay;
using MediaBrowser.Controller.SyncPlay.GroupStates;
using MediaBrowser.Controller.SyncPlay.Queue;
using MediaBrowser.Controller.SyncPlay.Requests;
using MediaBrowser.Model.SyncPlay;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.SyncPlay
{
    /// <summary>
    /// Class Group.
    /// </summary>
    /// <remarks>
    /// Class is not thread-safe, external locking is required when accessing methods.
    /// </remarks>
    public class Group : IGroupStateContext
    {
        /// <summary>
        /// The logger.
        /// </summary>
        private readonly ILogger<Group> _logger;

        /// <summary>
        /// The logger factory.
        /// </summary>
        private readonly ILoggerFactory _loggerFactory;

        /// <summary>
        /// The user manager.
        /// </summary>
        private readonly IUserManager _userManager;

        /// <summary>
        /// The session manager.
        /// </summary>
        private readonly ISessionManager _sessionManager;

        /// <summary>
        /// The library manager.
        /// </summary>
        private readonly ILibraryManager _libraryManager;

        /// <summary>
        /// The participants, or members of the group.
        /// </summary>
        private readonly Dictionary<string, GroupMember> _participants =
            new Dictionary<string, GroupMember>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// The internal group state.
        /// </summary>
        private IGroupState _state;

        /// <summary>
        /// Initializes a new instance of the <see cref="Group" /> class.
        /// </summary>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="userManager">The user manager.</param>
        /// <param name="sessionManager">The session manager.</param>
        /// <param name="libraryManager">The library manager.</param>
        public Group(
            ILoggerFactory loggerFactory,
            IUserManager userManager,
            ISessionManager sessionManager,
            ILibraryManager libraryManager)
        {
            _loggerFactory = loggerFactory;
            _userManager = userManager;
            _sessionManager = sessionManager;
            _libraryManager = libraryManager;
            _logger = loggerFactory.CreateLogger<Group>();

            _state = new IdleGroupState(loggerFactory);
        }

        /// <summary>
        /// Gets the default ping value used for sessions.
        /// </summary>
        /// <value>The default ping.</value>
        public long DefaultPing { get; } = 500;

        /// <summary>
        /// Gets the maximum time offset error accepted for dates reported by clients, in milliseconds.
        /// </summary>
        /// <value>The maximum time offset error.</value>
        public long TimeSyncOffset { get; } = 2000;

        /// <summary>
        /// Gets the maximum offset error accepted for position reported by clients, in milliseconds.
        /// </summary>
        /// <value>The maximum offset error.</value>
        public long MaxPlaybackOffset { get; } = 500;

        /// <summary>
        /// Gets the group identifier.
        /// </summary>
        /// <value>The group identifier.</value>
        public Guid GroupId { get; } = Guid.NewGuid();

        /// <summary>
        /// Gets the group name.
        /// </summary>
        /// <value>The group name.</value>
        public string GroupName { get; private set; }

        /// <summary>
        /// Gets the group identifier.
        /// </summary>
        /// <value>The group identifier.</value>
        public PlayQueueManager PlayQueue { get; } = new PlayQueueManager();

        /// <summary>
        /// Gets the runtime ticks of current playing item.
        /// </summary>
        /// <value>The runtime ticks of current playing item.</value>
        public long RunTimeTicks { get; private set; }

        /// <summary>
        /// Gets or sets the position ticks.
        /// </summary>
        /// <value>The position ticks.</value>
        public long PositionTicks { get; set; }

        /// <summary>
        /// Gets or sets the last activity.
        /// </summary>
        /// <value>The last activity.</value>
        public DateTime LastActivity { get; set; }

        /// <summary>
        /// Adds the session to the group.
        /// </summary>
        /// <param name="session">The session.</param>
        private void AddSession(SessionInfo session)
        {
            _participants.TryAdd(
                session.Id,
                new GroupMember(session)
                {
                    Ping = DefaultPing,
                    IsBuffering = false
                });
        }

        /// <summary>
        /// Removes the session from the group.
        /// </summary>
        /// <param name="session">The session.</param>
        private void RemoveSession(SessionInfo session)
        {
            _participants.Remove(session.Id);
        }

        /// <summary>
        /// Filters sessions of this group.
        /// </summary>
        /// <param name="fromId">The current session identifier.</param>
        /// <param name="type">The filtering type.</param>
        /// <returns>The list of sessions matching the filter.</returns>
        private IEnumerable<string> FilterSessions(string fromId, SyncPlayBroadcastType type)
        {
            return type switch
            {
                SyncPlayBroadcastType.CurrentSession => new string[] { fromId },
                SyncPlayBroadcastType.AllGroup => _participants
                    .Values
                    .Select(member => member.SessionId),
                SyncPlayBroadcastType.AllExceptCurrentSession => _participants
                    .Values
                    .Select(member => member.SessionId)
                    .Where(sessionId => !sessionId.Equals(fromId, StringComparison.OrdinalIgnoreCase)),
                SyncPlayBroadcastType.AllReady => _participants
                    .Values
                    .Where(member => !member.IsBuffering)
                    .Select(member => member.SessionId),
                _ => Enumerable.Empty<string>()
            };
        }

        /// <summary>
        /// Checks if a given user can access all items of a given queue, that is,
        /// the user has the required minimum parental access and has access to all required folders.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="queue">The queue.</param>
        /// <returns><c>true</c> if the user can access all the items in the queue, <c>false</c> otherwise.</returns>
        private bool HasAccessToQueue(User user, IReadOnlyList<Guid> queue)
        {
            // Check if queue is empty.
            if (queue is null || queue.Count == 0)
            {
                return true;
            }

            foreach (var itemId in queue)
            {
                var item = _libraryManager.GetItemById(itemId);
                if (!item.IsVisibleStandalone(user))
                {
                    return false;
                }
            }

            return true;
        }

        private bool AllUsersHaveAccessToQueue(IReadOnlyList<Guid> queue)
        {
            // Check if queue is empty.
            if (queue is null || queue.Count == 0)
            {
                return true;
            }

            // Get list of users.
            var users = _participants
                .Values
                .Select(participant => _userManager.GetUserById(participant.UserId));

            // Find problematic users.
            var usersWithNoAccess = users.Where(user => !HasAccessToQueue(user, queue));

            // All users must be able to access the queue.
            return !usersWithNoAccess.Any();
        }

        /// <summary>
        /// Checks if the group is empty.
        /// </summary>
        /// <returns><c>true</c> if the group is empty, <c>false</c> otherwise.</returns>
        public bool IsGroupEmpty() => _participants.Count == 0;

        /// <summary>
        /// Initializes the group with the session's info.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="request">The request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public void CreateGroup(SessionInfo session, NewGroupRequest request, CancellationToken cancellationToken)
        {
            GroupName = request.GroupName;
            AddSession(session);

            var sessionIsPlayingAnItem = session.FullNowPlayingItem is not null;

            RestartCurrentItem();

            if (sessionIsPlayingAnItem)
            {
                var playlist = session.NowPlayingQueue.Select(item => item.Id).ToList();
                PlayQueue.Reset();
                PlayQueue.SetPlaylist(playlist);
                PlayQueue.SetPlayingItemById(session.FullNowPlayingItem.Id);
                RunTimeTicks = session.FullNowPlayingItem.RunTimeTicks ?? 0;
                PositionTicks = session.PlayState.PositionTicks ?? 0;

                // Maintain playstate.
                var waitingState = new WaitingGroupState(_loggerFactory)
                {
                    ResumePlaying = !session.PlayState.IsPaused
                };
                SetState(waitingState);
            }

            var updateSession = NewSyncPlayGroupUpdate(GroupUpdateType.GroupJoined, GetInfo());
            SendGroupUpdate(session, SyncPlayBroadcastType.CurrentSession, updateSession, cancellationToken);

            _state.SessionJoined(this, _state.Type, session, cancellationToken);

            _logger.LogInformation("Session {SessionId} created group {GroupId}.", session.Id, GroupId.ToString());
        }

        /// <summary>
        /// Adds the session to the group.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="request">The request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public void SessionJoin(SessionInfo session, JoinGroupRequest request, CancellationToken cancellationToken)
        {
            AddSession(session);

            var updateSession = NewSyncPlayGroupUpdate(GroupUpdateType.GroupJoined, GetInfo());
            SendGroupUpdate(session, SyncPlayBroadcastType.CurrentSession, updateSession, cancellationToken);

            var updateOthers = NewSyncPlayGroupUpdate(GroupUpdateType.UserJoined, session.UserName);
            SendGroupUpdate(session, SyncPlayBroadcastType.AllExceptCurrentSession, updateOthers, cancellationToken);

            _state.SessionJoined(this, _state.Type, session, cancellationToken);

            _logger.LogInformation("Session {SessionId} joined group {GroupId}.", session.Id, GroupId.ToString());
        }

        /// <summary>
        /// Removes the session from the group.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="request">The request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public void SessionLeave(SessionInfo session, LeaveGroupRequest request, CancellationToken cancellationToken)
        {
            _state.SessionLeaving(this, _state.Type, session, cancellationToken);

            RemoveSession(session);

            var updateSession = NewSyncPlayGroupUpdate(GroupUpdateType.GroupLeft, GroupId.ToString());
            SendGroupUpdate(session, SyncPlayBroadcastType.CurrentSession, updateSession, cancellationToken);

            var updateOthers = NewSyncPlayGroupUpdate(GroupUpdateType.UserLeft, session.UserName);
            SendGroupUpdate(session, SyncPlayBroadcastType.AllExceptCurrentSession, updateOthers, cancellationToken);

            _logger.LogInformation("Session {SessionId} left group {GroupId}.", session.Id, GroupId.ToString());
        }

        /// <summary>
        /// Handles the requested action by the session.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="request">The requested action.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public void HandleRequest(SessionInfo session, IGroupPlaybackRequest request, CancellationToken cancellationToken)
        {
            // The server's job is to maintain a consistent state for clients to reference
            // and notify clients of state changes. The actual syncing of media playback
            // happens client side. Clients are aware of the server's time and use it to sync.
            _logger.LogInformation("Session {SessionId} requested {RequestType} in group {GroupId} that is {StateType}.", session.Id, request.Action, GroupId.ToString(), _state.Type);

            // Apply requested changes to this group given its current state.
            // Every request has a slightly different outcome depending on the group's state.
            // There are currently four different group states that accomplish different goals:
            // - Idle: in this state no media is playing and clients should be idle (playback is stopped).
            // - Waiting: in this state the group is waiting for all the clients to be ready to start the playback,
            //      that is, they've either finished loading the media for the first time or they've finished buffering.
            //      Once all clients report to be ready the group's state can change to Playing or Paused.
            // - Playing: clients have some media loaded and playback is unpaused.
            // - Paused: clients have some media loaded but playback is currently paused.
            request.Apply(this, _state, session, cancellationToken);
        }

        /// <summary>
        /// Gets the info about the group for the clients.
        /// </summary>
        /// <returns>The group info for the clients.</returns>
        public GroupInfoDto GetInfo()
        {
            var participants = _participants.Values.Select(session => session.UserName).Distinct().ToList();
            return new GroupInfoDto(GroupId, GroupName, _state.Type, participants, DateTime.UtcNow);
        }

        /// <summary>
        /// Checks if a user has access to all content in the play queue.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns><c>true</c> if the user can access the play queue; <c>false</c> otherwise.</returns>
        public bool HasAccessToPlayQueue(User user)
        {
            var items = PlayQueue.GetPlaylist().Select(item => item.ItemId).ToList();
            return HasAccessToQueue(user, items);
        }

        /// <inheritdoc />
        public void SetIgnoreGroupWait(SessionInfo session, bool ignoreGroupWait)
        {
            if (_participants.TryGetValue(session.Id, out GroupMember value))
            {
                value.IgnoreGroupWait = ignoreGroupWait;
            }
        }

        /// <inheritdoc />
        public void SetState(IGroupState state)
        {
            _logger.LogInformation("Group {GroupId} switching from {FromStateType} to {ToStateType}.", GroupId.ToString(), _state.Type, state.Type);
            this._state = state;
        }

        /// <inheritdoc />
        public Task SendGroupUpdate<T>(SessionInfo from, SyncPlayBroadcastType type, GroupUpdate<T> message, CancellationToken cancellationToken)
        {
            IEnumerable<Task> GetTasks()
            {
                foreach (var sessionId in FilterSessions(from.Id, type))
                {
                    yield return _sessionManager.SendSyncPlayGroupUpdate(sessionId, message, cancellationToken);
                }
            }

            return Task.WhenAll(GetTasks());
        }

        /// <inheritdoc />
        public Task SendCommand(SessionInfo from, SyncPlayBroadcastType type, SendCommand message, CancellationToken cancellationToken)
        {
            IEnumerable<Task> GetTasks()
            {
                foreach (var sessionId in FilterSessions(from.Id, type))
                {
                    yield return _sessionManager.SendSyncPlayCommand(sessionId, message, cancellationToken);
                }
            }

            return Task.WhenAll(GetTasks());
        }

        /// <inheritdoc />
        public SendCommand NewSyncPlayCommand(SendCommandType type)
        {
            return new SendCommand(
                GroupId,
                PlayQueue.GetPlayingItemPlaylistId(),
                LastActivity,
                type,
                PositionTicks,
                DateTime.UtcNow);
        }

        /// <inheritdoc />
        public GroupUpdate<T> NewSyncPlayGroupUpdate<T>(GroupUpdateType type, T data)
        {
            return new GroupUpdate<T>(GroupId, type, data);
        }

        /// <inheritdoc />
        public long SanitizePositionTicks(long? positionTicks)
        {
            var ticks = positionTicks ?? 0;
            return Math.Clamp(ticks, 0, RunTimeTicks);
        }

        /// <inheritdoc />
        public void UpdatePing(SessionInfo session, long ping)
        {
            if (_participants.TryGetValue(session.Id, out GroupMember value))
            {
                value.Ping = ping;
            }
        }

        /// <inheritdoc />
        public long GetHighestPing()
        {
            long max = long.MinValue;
            foreach (var session in _participants.Values)
            {
                max = Math.Max(max, session.Ping);
            }

            return max;
        }

        /// <inheritdoc />
        public void SetBuffering(SessionInfo session, bool isBuffering)
        {
            if (_participants.TryGetValue(session.Id, out GroupMember value))
            {
                value.IsBuffering = isBuffering;
            }
        }

        /// <inheritdoc />
        public void SetAllBuffering(bool isBuffering)
        {
            foreach (var session in _participants.Values)
            {
                session.IsBuffering = isBuffering;
            }
        }

        /// <inheritdoc />
        public bool IsBuffering()
        {
            foreach (var session in _participants.Values)
            {
                if (session.IsBuffering && !session.IgnoreGroupWait)
                {
                    return true;
                }
            }

            return false;
        }

        /// <inheritdoc />
        public bool SetPlayQueue(IReadOnlyList<Guid> playQueue, int playingItemPosition, long startPositionTicks)
        {
            // Ignore on empty queue or invalid item position.
            if (playQueue.Count == 0 || playingItemPosition >= playQueue.Count || playingItemPosition < 0)
            {
                return false;
            }

            // Check if participants can access the new playing queue.
            if (!AllUsersHaveAccessToQueue(playQueue))
            {
                return false;
            }

            PlayQueue.Reset();
            PlayQueue.SetPlaylist(playQueue);
            PlayQueue.SetPlayingItemByIndex(playingItemPosition);
            var item = _libraryManager.GetItemById(PlayQueue.GetPlayingItemId());
            RunTimeTicks = item.RunTimeTicks ?? 0;
            PositionTicks = startPositionTicks;
            LastActivity = DateTime.UtcNow;

            return true;
        }

        /// <inheritdoc />
        public bool SetPlayingItem(Guid playlistItemId)
        {
            var itemFound = PlayQueue.SetPlayingItemByPlaylistId(playlistItemId);

            if (itemFound)
            {
                var item = _libraryManager.GetItemById(PlayQueue.GetPlayingItemId());
                RunTimeTicks = item.RunTimeTicks ?? 0;
            }
            else
            {
                RunTimeTicks = 0;
            }

            RestartCurrentItem();

            return itemFound;
        }

        /// <inheritdoc />
        public void ClearPlayQueue(bool clearPlayingItem)
        {
            PlayQueue.ClearPlaylist(clearPlayingItem);
            if (clearPlayingItem)
            {
                RestartCurrentItem();
            }
        }

        /// <inheritdoc />
        public bool RemoveFromPlayQueue(IReadOnlyList<Guid> playlistItemIds)
        {
            var playingItemRemoved = PlayQueue.RemoveFromPlaylist(playlistItemIds);
            if (playingItemRemoved)
            {
                var itemId = PlayQueue.GetPlayingItemId();
                if (!itemId.IsEmpty())
                {
                    var item = _libraryManager.GetItemById(itemId);
                    RunTimeTicks = item.RunTimeTicks ?? 0;
                }
                else
                {
                    RunTimeTicks = 0;
                }

                RestartCurrentItem();
            }

            return playingItemRemoved;
        }

        /// <inheritdoc />
        public bool MoveItemInPlayQueue(Guid playlistItemId, int newIndex)
        {
            return PlayQueue.MovePlaylistItem(playlistItemId, newIndex);
        }

        /// <inheritdoc />
        public bool AddToPlayQueue(IReadOnlyList<Guid> newItems, GroupQueueMode mode)
        {
            // Ignore on empty list.
            if (newItems.Count == 0)
            {
                return false;
            }

            // Check if participants can access the new playing queue.
            if (!AllUsersHaveAccessToQueue(newItems))
            {
                return false;
            }

            if (mode.Equals(GroupQueueMode.QueueNext))
            {
                PlayQueue.QueueNext(newItems);
            }
            else
            {
                PlayQueue.Queue(newItems);
            }

            return true;
        }

        /// <inheritdoc />
        public void RestartCurrentItem()
        {
            PositionTicks = 0;
            LastActivity = DateTime.UtcNow;
        }

        /// <inheritdoc />
        public bool NextItemInQueue()
        {
            var update = PlayQueue.Next();
            if (update)
            {
                var item = _libraryManager.GetItemById(PlayQueue.GetPlayingItemId());
                RunTimeTicks = item.RunTimeTicks ?? 0;
                RestartCurrentItem();
                return true;
            }

            return false;
        }

        /// <inheritdoc />
        public bool PreviousItemInQueue()
        {
            var update = PlayQueue.Previous();
            if (update)
            {
                var item = _libraryManager.GetItemById(PlayQueue.GetPlayingItemId());
                RunTimeTicks = item.RunTimeTicks ?? 0;
                RestartCurrentItem();
                return true;
            }

            return false;
        }

        /// <inheritdoc />
        public void SetRepeatMode(GroupRepeatMode mode)
        {
            PlayQueue.SetRepeatMode(mode);
        }

        /// <inheritdoc />
        public void SetShuffleMode(GroupShuffleMode mode)
        {
            PlayQueue.SetShuffleMode(mode);
        }

        /// <inheritdoc />
        public PlayQueueUpdate GetPlayQueueUpdate(PlayQueueUpdateReason reason)
        {
            var startPositionTicks = PositionTicks;
            var isPlaying = _state.Type.Equals(GroupStateType.Playing);

            if (isPlaying)
            {
                var currentTime = DateTime.UtcNow;
                var elapsedTime = currentTime - LastActivity;
                // Elapsed time is negative if event happens
                // during the delay added to account for latency.
                // In this phase clients haven't started the playback yet.
                // In other words, LastActivity is in the future,
                // when playback unpause is supposed to happen.
                // Adjust ticks only if playback actually started.
                startPositionTicks += Math.Max(elapsedTime.Ticks, 0);
            }

            return new PlayQueueUpdate(
                reason,
                PlayQueue.LastChange,
                PlayQueue.GetPlaylist(),
                PlayQueue.PlayingItemIndex,
                startPositionTicks,
                isPlaying,
                PlayQueue.ShuffleMode,
                PlayQueue.RepeatMode);
        }
    }
}
