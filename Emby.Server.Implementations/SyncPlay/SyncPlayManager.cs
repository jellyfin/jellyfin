using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using MediaBrowser.Controller.SyncPlay;
using MediaBrowser.Model.SyncPlay;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.SyncPlay
{
    /// <summary>
    /// Class SyncPlayManager.
    /// </summary>
    public class SyncPlayManager : ISyncPlayManager, IDisposable
    {
        /// <summary>
        /// The logger.
        /// </summary>
        private readonly ILogger<SyncPlayManager> _logger;

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
        /// The map between sessions and groups.
        /// </summary>
        private readonly Dictionary<string, IGroupController> _sessionToGroupMap =
            new Dictionary<string, IGroupController>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// The groups.
        /// </summary>
        private readonly Dictionary<Guid, IGroupController> _groups =
            new Dictionary<Guid, IGroupController>();

        /// <summary>
        /// Lock used for accesing any group.
        /// </summary>
        private readonly object _groupsLock = new object();

        private bool _disposed = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="SyncPlayManager" /> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="userManager">The user manager.</param>
        /// <param name="sessionManager">The session manager.</param>
        /// <param name="libraryManager">The library manager.</param>
        public SyncPlayManager(
            ILogger<SyncPlayManager> logger,
            IUserManager userManager,
            ISessionManager sessionManager,
            ILibraryManager libraryManager)
        {
            _logger = logger;
            _userManager = userManager;
            _sessionManager = sessionManager;
            _libraryManager = libraryManager;
            _sessionManager.SessionStarted += OnSessionManagerSessionStarted;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc />
        public void NewGroup(SessionInfo session, NewGroupRequest request, CancellationToken cancellationToken)
        {
            // TODO: create abstract class for GroupRequests to avoid explicit request type here.
            if (!IsRequestValid(session, GroupRequestType.NewGroup, request))
            {
                return;
            }

            lock (_groupsLock)
            {
                if (IsSessionInGroup(session))
                {
                    LeaveGroup(session, cancellationToken);
                }

                var group = new GroupController(_logger, _userManager, _sessionManager, _libraryManager);
                _groups[group.GroupId] = group;

                AddSessionToGroup(session, group);
                group.CreateGroup(session, request, cancellationToken);
            }
        }

        /// <inheritdoc />
        public void JoinGroup(SessionInfo session, Guid groupId, JoinGroupRequest request, CancellationToken cancellationToken)
        {
            // TODO: create abstract class for GroupRequests to avoid explicit request type here.
            if (!IsRequestValid(session, GroupRequestType.JoinGroup, request))
            {
                return;
            }

            var user = _userManager.GetUserById(session.UserId);

            lock (_groupsLock)
            {
                _groups.TryGetValue(groupId, out IGroupController group);

                if (group == null)
                {
                    _logger.LogWarning("JoinGroup: {SessionId} tried to join group {GroupId} that does not exist.", session.Id, groupId);

                    var error = new GroupUpdate<string>(Guid.Empty, GroupUpdateType.GroupDoesNotExist, string.Empty);
                    _sessionManager.SendSyncPlayGroupUpdate(session, error, CancellationToken.None);
                    return;
                }

                if (!group.HasAccessToPlayQueue(user))
                {
                    _logger.LogWarning("JoinGroup: {SessionId} does not have access to some content from the playing queue of group {GroupId}.", session.Id, group.GroupId.ToString());

                    var error = new GroupUpdate<string>(group.GroupId, GroupUpdateType.LibraryAccessDenied, string.Empty);
                    _sessionManager.SendSyncPlayGroupUpdate(session, error, CancellationToken.None);
                    return;
                }

                if (IsSessionInGroup(session))
                {
                    if (GetSessionGroup(session).Equals(groupId))
                    {
                        group.SessionRestore(session, request, cancellationToken);
                        return;
                    }

                    LeaveGroup(session, cancellationToken);
                }

                AddSessionToGroup(session, group);
                group.SessionJoin(session, request, cancellationToken);
            }
        }

        /// <inheritdoc />
        public void LeaveGroup(SessionInfo session, CancellationToken cancellationToken)
        {
            // TODO: create abstract class for GroupRequests to avoid explicit request type here.
            if (!IsRequestValid(session, GroupRequestType.LeaveGroup))
            {
                return;
            }

            // TODO: determine what happens to users that are in a group and get their permissions revoked.
            lock (_groupsLock)
            {
                _sessionToGroupMap.TryGetValue(session.Id, out var group);

                if (group == null)
                {
                    _logger.LogWarning("LeaveGroup: {SessionId} does not belong to any group.", session.Id);

                    var error = new GroupUpdate<string>(Guid.Empty, GroupUpdateType.NotInGroup, string.Empty);
                    _sessionManager.SendSyncPlayGroupUpdate(session, error, CancellationToken.None);
                    return;
                }

                RemoveSessionFromGroup(session, group);
                group.SessionLeave(session, cancellationToken);

                if (group.IsGroupEmpty())
                {
                    _logger.LogInformation("LeaveGroup: removing empty group {GroupId}.", group.GroupId);
                    _groups.Remove(group.GroupId, out _);
                }
            }
        }

        /// <inheritdoc />
        public List<GroupInfoDto> ListGroups(SessionInfo session)
        {
            // TODO: create abstract class for GroupRequests to avoid explicit request type here.
            if (!IsRequestValid(session, GroupRequestType.ListGroups))
            {
                return new List<GroupInfoDto>();
            }

            var user = _userManager.GetUserById(session.UserId);

            lock (_groupsLock)
            {
                return _groups
                    .Values
                    .Where(group => group.HasAccessToPlayQueue(user))
                    .Select(group => group.GetInfo())
                    .ToList();
            }
        }

        /// <inheritdoc />
        public void HandleRequest(SessionInfo session, IGroupPlaybackRequest request, CancellationToken cancellationToken)
        {
            // TODO: create abstract class for GroupRequests to avoid explicit request type here.
            if (!IsRequestValid(session, GroupRequestType.Playback, request))
            {
                return;
            }

            lock (_groupsLock)
            {
                _sessionToGroupMap.TryGetValue(session.Id, out var group);

                if (group == null)
                {
                    _logger.LogWarning("HandleRequest: {SessionId} does not belong to any group.", session.Id);

                    var error = new GroupUpdate<string>(Guid.Empty, GroupUpdateType.NotInGroup, string.Empty);
                    _sessionManager.SendSyncPlayGroupUpdate(session, error, CancellationToken.None);
                    return;
                }

                group.HandleRequest(session, request, cancellationToken);
            }
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

            _sessionManager.SessionStarted -= OnSessionManagerSessionStarted;
            _disposed = true;
        }

        private void OnSessionManagerSessionStarted(object sender, SessionEventArgs e)
        {
            var session = e.SessionInfo;
            lock (_groupsLock)
            {
                if (!IsSessionInGroup(session))
                {
                    return;
                }

                var groupId = GetSessionGroup(session);
                var request = new JoinGroupRequest(groupId);
                JoinGroup(session, groupId, request, CancellationToken.None);
            }
        }

        /// <summary>
        /// Checks if a given session has joined a group.
        /// </summary>
        /// <remarks>
        /// Not thread-safe, call only under groups-lock.
        /// </remarks>
        /// <param name="session">The session.</param>
        /// <returns><c>true</c> if the session has joined a group, <c>false</c> otherwise.</returns>
        private bool IsSessionInGroup(SessionInfo session)
        {
            return _sessionToGroupMap.ContainsKey(session.Id);
        }

        /// <summary>
        /// Gets the group joined by the given session, if any.
        /// </summary>
        /// <remarks>
        /// Not thread-safe, call only under groups-lock.
        /// </remarks>
        /// <param name="session">The session.</param>
        /// <returns>The group identifier if the session has joined a group, an empty identifier otherwise.</returns>
        private Guid GetSessionGroup(SessionInfo session)
        {
            _sessionToGroupMap.TryGetValue(session.Id, out var group);
            return group?.GroupId ?? Guid.Empty;
        }

        /// <summary>
        /// Maps a session to a group.
        /// </summary>
        /// <remarks>
        /// Not thread-safe, call only under groups-lock.
        /// </remarks>
        /// <param name="session">The session.</param>
        /// <param name="group">The group.</param>
        /// <exception cref="InvalidOperationException">Thrown when the user is in another group already.</exception>
        private void AddSessionToGroup(SessionInfo session, IGroupController group)
        {
            if (session == null)
            {
                throw new InvalidOperationException("Session is null!");
            }

            if (IsSessionInGroup(session))
            {
                throw new InvalidOperationException("Session in other group already!");
            }

            _sessionToGroupMap[session.Id] = group ?? throw new InvalidOperationException("Group is null!");
        }

        /// <summary>
        /// Unmaps a session from a group.
        /// </summary>
        /// <remarks>
        /// Not thread-safe, call only under groups-lock.
        /// </remarks>
        /// <param name="session">The session.</param>
        /// <param name="group">The group.</param>
        /// <exception cref="InvalidOperationException">Thrown when the user is not found in the specified group.</exception>
        private void RemoveSessionFromGroup(SessionInfo session, IGroupController group)
        {
            if (session == null)
            {
                throw new InvalidOperationException("Session is null!");
            }

            if (group == null)
            {
                throw new InvalidOperationException("Group is null!");
            }

            if (!IsSessionInGroup(session))
            {
                throw new InvalidOperationException("Session not in any group!");
            }

            _sessionToGroupMap.Remove(session.Id, out var tempGroup);
            if (!tempGroup.GroupId.Equals(group.GroupId))
            {
                throw new InvalidOperationException("Session was in wrong group!");
            }
        }

        /// <summary>
        /// Checks if a given session is allowed to make a given request.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="requestType">The request type.</param>
        /// <param name="request">The request.</param>
        /// <param name="checkRequest">Whether to check if request is null.</param>
        /// <returns><c>true</c> if the request is valid, <c>false</c> otherwise. Will return <c>false</c> also when session is null.</returns>
        private bool IsRequestValid<T>(SessionInfo session, GroupRequestType requestType, T request, bool checkRequest = true)
        {
            if (session == null || (request == null && checkRequest))
            {
                return false;
            }

            var user = _userManager.GetUserById(session.UserId);

            if (user.SyncPlayAccess == SyncPlayAccess.None)
            {
                _logger.LogWarning("IsRequestValid: {SessionId} does not have access to SyncPlay. Requested {RequestType}.", session.Id, requestType);

                // TODO: rename to a more generic error. Next PR will fix this.
                var error = new GroupUpdate<string>(Guid.Empty, GroupUpdateType.JoinGroupDenied, string.Empty);
                _sessionManager.SendSyncPlayGroupUpdate(session, error, CancellationToken.None);
                return false;
            }

            if (requestType.Equals(GroupRequestType.NewGroup) && user.SyncPlayAccess != SyncPlayAccess.CreateAndJoinGroups)
            {
                _logger.LogWarning("IsRequestValid: {SessionId} does not have permission to create groups.", session.Id);

                var error = new GroupUpdate<string>(Guid.Empty, GroupUpdateType.CreateGroupDenied, string.Empty);
                _sessionManager.SendSyncPlayGroupUpdate(session, error, CancellationToken.None);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks if a given session is allowed to make a given type of request.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="requestType">The request type.</param>
        /// <returns><c>true</c> if the request is valid, <c>false</c> otherwise. Will return <c>false</c> also when session is null.</returns>
        private bool IsRequestValid(SessionInfo session, GroupRequestType requestType)
        {
            return IsRequestValid(session, requestType, session, false);
        }
    }
}
