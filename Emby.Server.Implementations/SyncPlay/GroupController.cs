using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using MediaBrowser.Controller.SyncPlay;
using MediaBrowser.Model.SyncPlay;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.SyncPlay
{
    /// <summary>
    /// Class SyncPlayGroupController.
    /// </summary>
    /// <remarks>
    /// Class is not thread-safe, external locking is required when accessing methods.
    /// </remarks>
    public class SyncPlayGroupController : ISyncPlayGroupController, ISyncPlayStateContext
    {
        /// <summary>
        /// Gets the default ping value used for sessions.
        /// </summary>
        public long DefaultPing { get; } = 500;

        /// <summary>
        /// The logger.
        /// </summary>
        private readonly ILogger _logger;

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
        /// The SyncPlay manager.
        /// </summary>
        private readonly ISyncPlayManager _syncPlayManager;

        /// <summary>
        /// Internal group state.
        /// </summary>
        /// <value>The group's state.</value>
        private ISyncPlayState State;

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
        /// Gets or sets the runtime ticks of current playing item.
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
        /// Gets the participants.
        /// </summary>
        /// <value>The participants, or members of the group.</value>
        public Dictionary<string, GroupMember> Participants { get; } =
            new Dictionary<string, GroupMember>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Initializes a new instance of the <see cref="SyncPlayGroupController" /> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="userManager">The user manager.</param>
        /// <param name="sessionManager">The session manager.</param>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="syncPlayManager">The SyncPlay manager.</param>
        public SyncPlayGroupController(
            ILogger logger,
            IUserManager userManager,
            ISessionManager sessionManager,
            ILibraryManager libraryManager,
            ISyncPlayManager syncPlayManager)
        {
            _logger = logger;
            _userManager = userManager;
            _sessionManager = sessionManager;
            _libraryManager = libraryManager;
            _syncPlayManager = syncPlayManager;

            State = new IdleGroupState(_logger);
        }

        /// <summary>
        /// Checks if a session is in this group.
        /// </summary>
        /// <param name="sessionId">The session id to check.</param>
        /// <returns><c>true</c> if the session is in this group; <c>false</c> otherwise.</returns>
        private bool ContainsSession(string sessionId)
        {
            return Participants.ContainsKey(sessionId);
        }

        /// <summary>
        /// Adds the session to the group.
        /// </summary>
        /// <param name="session">The session.</param>
        private void AddSession(SessionInfo session)
        {
            Participants.TryAdd(
                session.Id,
                new GroupMember
                {
                    Session = session,
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
            Participants.Remove(session.Id);
        }

        /// <summary>
        /// Filters sessions of this group.
        /// </summary>
        /// <param name="from">The current session.</param>
        /// <param name="type">The filtering type.</param>
        /// <returns>The array of sessions matching the filter.</returns>
        private SessionInfo[] FilterSessions(SessionInfo from, SyncPlayBroadcastType type)
        {
            switch (type)
            {
                case SyncPlayBroadcastType.CurrentSession:
                    return new SessionInfo[] { from };
                case SyncPlayBroadcastType.AllGroup:
                    return Participants.Values.Select(
                        session => session.Session).ToArray();
                case SyncPlayBroadcastType.AllExceptCurrentSession:
                    return Participants.Values.Select(
                        session => session.Session).Where(
                        session => !session.Id.Equals(from.Id)).ToArray();
                case SyncPlayBroadcastType.AllReady:
                    return Participants.Values.Where(
                        session => !session.IsBuffering).Select(
                        session => session.Session).ToArray();
                default:
                    return Array.Empty<SessionInfo>();
            }
        }

        private bool HasAccessToItem(User user, BaseItem item)
        {
            var collections = _libraryManager.GetCollectionFolders(item)
                .Select(folder => folder.Id.ToString("N", CultureInfo.InvariantCulture));
            return collections.Intersect(user.GetPreference(PreferenceKind.EnabledFolders)).Any();
        }

        private bool HasAccessToQueue(User user, Guid[] queue)
        {
            if (queue == null || queue.Length == 0)
            {
                return true;
            }

            var items = queue.ToList()
                .Select(item => _libraryManager.GetItemById(item));

            // Find the highest rating value, which becomes the required minimum for the user.
            var MinParentalRatingAccessRequired = items
                .Select(item => item.InheritedParentalRatingValue)
                .Min();

            // Check ParentalRating access, user must have the minimum required access level.
            var hasParentalRatingAccess = !user.MaxParentalAgeRating.HasValue
                || MinParentalRatingAccessRequired <= user.MaxParentalAgeRating;

            // Check that user has access to all required folders.
            if (!user.HasPermission(PermissionKind.EnableAllFolders) && hasParentalRatingAccess)
            {
                // Get list of items that are not accessible.
                var blockedItems = items.Where(item => !HasAccessToItem(user, item));

                // We need the user to be able to access all items.
                return !blockedItems.Any();
            }

            return hasParentalRatingAccess;
        }

        private bool AllUsersHaveAccessToQueue(Guid[] queue)
        {
            if (queue == null || queue.Length == 0)
            {
                return true;
            }

            // Get list of users.
            var users = Participants.Values
                .Select(participant => _userManager.GetUserById(participant.Session.UserId));

            // Find problematic users.
            var usersWithNoAccess = users.Where(user => !HasAccessToQueue(user, queue));

            // All users must be able to access the queue.
            return !usersWithNoAccess.Any();
        }

        /// <inheritdoc />
        public bool IsGroupEmpty() => Participants.Count == 0;

        /// <inheritdoc />
        public void CreateGroup(SessionInfo session, NewGroupRequest request, CancellationToken cancellationToken)
        {
            GroupName = request.GroupName;
            AddSession(session);
            _syncPlayManager.AddSessionToGroup(session, this);

            var sessionIsPlayingAnItem = session.FullNowPlayingItem != null;

            RestartCurrentItem();

            if (sessionIsPlayingAnItem)
            {
                var playlist = session.NowPlayingQueue.Select(item => item.Id).ToArray();
                PlayQueue.SetPlaylist(playlist);
                PlayQueue.SetPlayingItemById(session.FullNowPlayingItem.Id);
                RunTimeTicks = session.FullNowPlayingItem.RunTimeTicks ?? 0;
                PositionTicks = session.PlayState.PositionTicks ?? 0;

                // Mantain playstate.
                var waitingState = new WaitingGroupState(_logger);
                waitingState.ResumePlaying = !session.PlayState.IsPaused;
                SetState(waitingState);
            }

            var updateSession = NewSyncPlayGroupUpdate(GroupUpdateType.GroupJoined, GetInfo());
            SendGroupUpdate(session, SyncPlayBroadcastType.CurrentSession, updateSession, cancellationToken);

            State.SessionJoined(this, State.GetGroupState(), session, cancellationToken);

            _logger.LogInformation("InitGroup: {0} created group {1}.", session.Id.ToString(), GroupId.ToString());
        }

        /// <inheritdoc />
        public void SessionJoin(SessionInfo session, JoinGroupRequest request, CancellationToken cancellationToken)
        {
            AddSession(session);
            _syncPlayManager.AddSessionToGroup(session, this);

            var updateSession = NewSyncPlayGroupUpdate(GroupUpdateType.GroupJoined, GetInfo());
            SendGroupUpdate(session, SyncPlayBroadcastType.CurrentSession, updateSession, cancellationToken);

            var updateOthers = NewSyncPlayGroupUpdate(GroupUpdateType.UserJoined, session.UserName);
            SendGroupUpdate(session, SyncPlayBroadcastType.AllExceptCurrentSession, updateOthers, cancellationToken);

            State.SessionJoined(this, State.GetGroupState(), session, cancellationToken);

            _logger.LogInformation("SessionJoin: {0} joined group {1}.", session.Id.ToString(), GroupId.ToString());
        }

        /// <inheritdoc />
        public void SessionRestore(SessionInfo session, JoinGroupRequest request, CancellationToken cancellationToken)
        {
            var updateSession = NewSyncPlayGroupUpdate(GroupUpdateType.GroupJoined, GetInfo());
            SendGroupUpdate(session, SyncPlayBroadcastType.CurrentSession, updateSession, cancellationToken);

            var updateOthers = NewSyncPlayGroupUpdate(GroupUpdateType.UserJoined, session.UserName);
            SendGroupUpdate(session, SyncPlayBroadcastType.AllExceptCurrentSession, updateOthers, cancellationToken);

            State.SessionJoined(this, State.GetGroupState(), session, cancellationToken);

            _logger.LogInformation("SessionRestore: {0} re-joined group {1}.", session.Id.ToString(), GroupId.ToString());
        }

        /// <inheritdoc />
        public void SessionLeave(SessionInfo session, CancellationToken cancellationToken)
        {
            State.SessionLeaving(this, State.GetGroupState(), session, cancellationToken);

            RemoveSession(session);
            _syncPlayManager.RemoveSessionFromGroup(session, this);

            var updateSession = NewSyncPlayGroupUpdate(GroupUpdateType.GroupLeft, GroupId.ToString());
            SendGroupUpdate(session, SyncPlayBroadcastType.CurrentSession, updateSession, cancellationToken);

            var updateOthers = NewSyncPlayGroupUpdate(GroupUpdateType.UserLeft, session.UserName);
            SendGroupUpdate(session, SyncPlayBroadcastType.AllExceptCurrentSession, updateOthers, cancellationToken);

            _logger.LogInformation("SessionLeave: {0} left group {1}.", session.Id.ToString(), GroupId.ToString());
        }

        /// <inheritdoc />
        public void HandleRequest(SessionInfo session, IPlaybackGroupRequest request, CancellationToken cancellationToken)
        {
            // The server's job is to maintain a consistent state for clients to reference
            // and notify clients of state changes. The actual syncing of media playback
            // happens client side. Clients are aware of the server's time and use it to sync.
            _logger.LogInformation("HandleRequest: {0} requested {1}, group {2} in {3} state.",
                session.Id.ToString(), request.GetRequestType(), GroupId.ToString(), State.GetGroupState());
            request.Apply(this, State, session, cancellationToken);
        }

        /// <inheritdoc />
        public GroupInfoDto GetInfo()
        {
            return new GroupInfoDto()
            {
                GroupId = GroupId.ToString(),
                GroupName = GroupName,
                State = State.GetGroupState(),
                Participants = Participants.Values.Select(session => session.Session.UserName).Distinct().ToList(),
                LastUpdatedAt = DateToUTCString(DateTime.UtcNow)
            };
        }

        /// <inheritdoc />
        public bool HasAccessToPlayQueue(User user)
        {
            var items = PlayQueue.GetPlaylist().Select(item => item.ItemId).ToArray();
            return HasAccessToQueue(user, items);
        }

        /// <inheritdoc />
        public void SetIgnoreGroupWait(SessionInfo session, bool ignoreGroupWait)
        {
            if (!ContainsSession(session.Id))
            {
                return;
            }

            Participants[session.Id].IgnoreGroupWait = ignoreGroupWait;
        }

        /// <inheritdoc />
        public void SetState(ISyncPlayState state)
        {
            _logger.LogInformation("SetState: {0} switching from {1} to {2}.", GroupId.ToString(), State.GetGroupState(), state.GetGroupState());
            this.State = state;
        }

        /// <inheritdoc />
        public Task SendGroupUpdate<T>(SessionInfo from, SyncPlayBroadcastType type, GroupUpdate<T> message, CancellationToken cancellationToken)
        {
            IEnumerable<Task> GetTasks()
            {
                foreach (var session in FilterSessions(from, type))
                {
                    yield return _sessionManager.SendSyncPlayGroupUpdate(session, message, cancellationToken);
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
                    yield return _sessionManager.SendSyncPlayCommand(session, message, cancellationToken);
                }
            }

            return Task.WhenAll(GetTasks());
        }

        /// <inheritdoc />
        public SendCommand NewSyncPlayCommand(SendCommandType type)
        {
            return new SendCommand()
            {
                GroupId = GroupId.ToString(),
                PlaylistItemId = PlayQueue.GetPlayingItemPlaylistId(),
                PositionTicks = PositionTicks,
                Command = type,
                When = DateToUTCString(LastActivity),
                EmittedAt = DateToUTCString(DateTime.UtcNow)
            };
        }

        /// <inheritdoc />
        public GroupUpdate<T> NewSyncPlayGroupUpdate<T>(GroupUpdateType type, T data)
        {
            return new GroupUpdate<T>()
            {
                GroupId = GroupId.ToString(),
                Type = type,
                Data = data
            };
        }

        /// <inheritdoc />
        public string DateToUTCString(DateTime dateTime)
        {
            return dateTime.ToUniversalTime().ToString("o");
        }

        /// <inheritdoc />
        public long SanitizePositionTicks(long? positionTicks)
        {
            var ticks = positionTicks ?? 0;
            ticks = ticks >= 0 ? ticks : 0;
            ticks = ticks > RunTimeTicks ? RunTimeTicks : ticks;
            return ticks;
        }

        /// <inheritdoc />
        public void UpdatePing(SessionInfo session, long ping)
        {
            if (Participants.TryGetValue(session.Id, out GroupMember value))
            {
                value.Ping = ping;
            }
        }

        /// <inheritdoc />
        public long GetHighestPing()
        {
            long max = long.MinValue;
            foreach (var session in Participants.Values)
            {
                max = Math.Max(max, session.Ping);
            }

            return max;
        }

        /// <inheritdoc />
        public void SetBuffering(SessionInfo session, bool isBuffering)
        {
            if (Participants.TryGetValue(session.Id, out GroupMember value))
            {
                value.IsBuffering = isBuffering;
            }
        }

        /// <inheritdoc />
        public void SetAllBuffering(bool isBuffering)
        {
            foreach (var session in Participants.Values)
            {
                session.IsBuffering = isBuffering;
            }
        }

        /// <inheritdoc />
        public bool IsBuffering()
        {
            foreach (var session in Participants.Values)
            {
                if (session.IsBuffering && !session.IgnoreGroupWait)
                {
                    return true;
                }
            }

            return false;
        }

        /// <inheritdoc />
        public bool SetPlayQueue(Guid[] playQueue, int playingItemPosition, long startPositionTicks)
        {
            // Ignore on empty queue or invalid item position.
            if (playQueue.Length < 1 || playingItemPosition >= playQueue.Length || playingItemPosition < 0)
            {
                return false;
            }

            // Check is participants can access the new playing queue.
            if (!AllUsersHaveAccessToQueue(playQueue))
            {
                return false;
            }

            PlayQueue.SetPlaylist(playQueue);
            PlayQueue.SetPlayingItemByIndex(playingItemPosition);
            var item = _libraryManager.GetItemById(PlayQueue.GetPlayingItemId());
            RunTimeTicks = item.RunTimeTicks ?? 0;
            PositionTicks = startPositionTicks;
            LastActivity = DateTime.UtcNow;

            return true;
        }

        /// <inheritdoc />
        public bool SetPlayingItem(string playlistItemId)
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
        public bool RemoveFromPlayQueue(string[] playlistItemIds)
        {
            var playingItemRemoved = PlayQueue.RemoveFromPlaylist(playlistItemIds);
            if (playingItemRemoved)
            {
                var itemId = PlayQueue.GetPlayingItemId();
                if (!itemId.Equals(Guid.Empty))
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
        public bool MoveItemInPlayQueue(string playlistItemId, int newIndex)
        {
            return PlayQueue.MovePlaylistItem(playlistItemId, newIndex);
        }

        /// <inheritdoc />
        public bool AddToPlayQueue(Guid[] newItems, string mode)
        {
            // Ignore on empty list.
            if (newItems.Length < 1)
            {
                return false;
            }

            // Check is participants can access the new playing queue.
            if (!AllUsersHaveAccessToQueue(newItems))
            {
                return false;
            }

            if (mode.Equals("next"))
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
            else
            {
                return false;
            }
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
            else
            {
                return false;
            }
        }

        /// <inheritdoc />
        public void SetRepeatMode(string mode) {
            PlayQueue.SetRepeatMode(mode);
        }

        /// <inheritdoc />
        public void SetShuffleMode(string mode) {
            PlayQueue.SetShuffleMode(mode);
        }

        /// <inheritdoc />
        public PlayQueueUpdate GetPlayQueueUpdate(PlayQueueUpdateReason reason)
        {
            var startPositionTicks = PositionTicks;

            if (State.GetGroupState().Equals(GroupState.Playing))
            {
                var currentTime = DateTime.UtcNow;
                var elapsedTime = currentTime - LastActivity;
                // Event may happen during the delay added to account for latency.
                startPositionTicks += elapsedTime.Ticks > 0 ? elapsedTime.Ticks : 0;
            }

            return new PlayQueueUpdate()
            {
                Reason = reason,
                LastUpdate = DateToUTCString(PlayQueue.LastChange),
                Playlist = PlayQueue.GetPlaylist(),
                PlayingItemIndex = PlayQueue.PlayingItemIndex,
                StartPositionTicks = startPositionTicks,
                ShuffleMode = PlayQueue.ShuffleMode,
                RepeatMode = PlayQueue.RepeatMode
            };
        }

    }
}
