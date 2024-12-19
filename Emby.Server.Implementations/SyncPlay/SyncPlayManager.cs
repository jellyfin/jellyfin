#nullable disable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using MediaBrowser.Controller.SyncPlay;
using MediaBrowser.Controller.SyncPlay.Requests;
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
        /// The map between users and counter of active sessions.
        /// </summary>
        private readonly ConcurrentDictionary<Guid, int> _activeUsers =
            new ConcurrentDictionary<Guid, int>();

        /// <summary>
        /// The map between sessions and groups.
        /// </summary>
        private readonly ConcurrentDictionary<string, Group> _sessionToGroupMap =
            new ConcurrentDictionary<string, Group>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// The groups.
        /// </summary>
        private readonly ConcurrentDictionary<Guid, Group> _groups =
            new ConcurrentDictionary<Guid, Group>();

        /// <summary>
        /// Lock used for accessing multiple groups at once.
        /// </summary>
        /// <remarks>
        /// This lock has priority on locks made on <see cref="Group"/>.
        /// </remarks>
        private readonly Lock _groupsLock = new();

        private bool _disposed = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="SyncPlayManager" /> class.
        /// </summary>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="userManager">The user manager.</param>
        /// <param name="sessionManager">The session manager.</param>
        /// <param name="libraryManager">The library manager.</param>
        public SyncPlayManager(
            ILoggerFactory loggerFactory,
            IUserManager userManager,
            ISessionManager sessionManager,
            ILibraryManager libraryManager)
        {
            _loggerFactory = loggerFactory;
            _userManager = userManager;
            _sessionManager = sessionManager;
            _libraryManager = libraryManager;
            _logger = loggerFactory.CreateLogger<SyncPlayManager>();
            _sessionManager.SessionEnded += OnSessionEnded;
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
            if (session is null)
            {
                throw new InvalidOperationException("Session is null!");
            }

            if (request is null)
            {
                throw new InvalidOperationException("Request is null!");
            }

            // Locking required to access list of groups.
            lock (_groupsLock)
            {
                // Make sure that session has not joined another group.
                if (_sessionToGroupMap.ContainsKey(session.Id))
                {
                    var leaveGroupRequest = new LeaveGroupRequest();
                    LeaveGroup(session, leaveGroupRequest, cancellationToken);
                }

                var group = new Group(_loggerFactory, _userManager, _sessionManager, _libraryManager);
                _groups[group.GroupId] = group;

                if (!_sessionToGroupMap.TryAdd(session.Id, group))
                {
                    throw new InvalidOperationException("Could not add session to group!");
                }

                UpdateSessionsCounter(session.UserId, 1);
                group.CreateGroup(session, request, cancellationToken);
            }
        }

        /// <inheritdoc />
        public void JoinGroup(SessionInfo session, JoinGroupRequest request, CancellationToken cancellationToken)
        {
            if (session is null)
            {
                throw new InvalidOperationException("Session is null!");
            }

            if (request is null)
            {
                throw new InvalidOperationException("Request is null!");
            }

            var user = _userManager.GetUserById(session.UserId);

            // Locking required to access list of groups.
            lock (_groupsLock)
            {
                _groups.TryGetValue(request.GroupId, out Group group);

                if (group is null)
                {
                    _logger.LogWarning("Session {SessionId} tried to join group {GroupId} that does not exist.", session.Id, request.GroupId);

                    var error = new GroupUpdate<string>(Guid.Empty, GroupUpdateType.GroupDoesNotExist, string.Empty);
                    _sessionManager.SendSyncPlayGroupUpdate(session.Id, error, CancellationToken.None);
                    return;
                }

                // Group lock required to let other requests end first.
                lock (group)
                {
                    if (!group.HasAccessToPlayQueue(user))
                    {
                        _logger.LogWarning("Session {SessionId} tried to join group {GroupId} but does not have access to some content of the playing queue.", session.Id, group.GroupId.ToString());

                        var error = new GroupUpdate<string>(group.GroupId, GroupUpdateType.LibraryAccessDenied, string.Empty);
                        _sessionManager.SendSyncPlayGroupUpdate(session.Id, error, CancellationToken.None);
                        return;
                    }

                    if (_sessionToGroupMap.TryGetValue(session.Id, out var existingGroup))
                    {
                        if (existingGroup.GroupId.Equals(request.GroupId))
                        {
                            // Restore session.
                            UpdateSessionsCounter(session.UserId, 1);
                            group.SessionJoin(session, request, cancellationToken);
                            return;
                        }

                        var leaveGroupRequest = new LeaveGroupRequest();
                        LeaveGroup(session, leaveGroupRequest, cancellationToken);
                    }

                    if (!_sessionToGroupMap.TryAdd(session.Id, group))
                    {
                        throw new InvalidOperationException("Could not add session to group!");
                    }

                    UpdateSessionsCounter(session.UserId, 1);
                    group.SessionJoin(session, request, cancellationToken);
                }
            }
        }

        /// <inheritdoc />
        public void LeaveGroup(SessionInfo session, LeaveGroupRequest request, CancellationToken cancellationToken)
        {
            if (session is null)
            {
                throw new InvalidOperationException("Session is null!");
            }

            if (request is null)
            {
                throw new InvalidOperationException("Request is null!");
            }

            // Locking required to access list of groups.
            lock (_groupsLock)
            {
                if (_sessionToGroupMap.TryGetValue(session.Id, out var group))
                {
                    // Group lock required to let other requests end first.
                    lock (group)
                    {
                        if (_sessionToGroupMap.TryRemove(session.Id, out var tempGroup))
                        {
                            if (!tempGroup.GroupId.Equals(group.GroupId))
                            {
                                throw new InvalidOperationException("Session was in wrong group!");
                            }
                        }
                        else
                        {
                            throw new InvalidOperationException("Could not remove session from group!");
                        }

                        UpdateSessionsCounter(session.UserId, -1);
                        group.SessionLeave(session, request, cancellationToken);

                        if (group.IsGroupEmpty())
                        {
                            _logger.LogInformation("Group {GroupId} is empty, removing it.", group.GroupId);
                            _groups.Remove(group.GroupId, out _);
                        }
                    }
                }
                else
                {
                    _logger.LogWarning("Session {SessionId} does not belong to any group.", session.Id);

                    var error = new GroupUpdate<string>(Guid.Empty, GroupUpdateType.NotInGroup, string.Empty);
                    _sessionManager.SendSyncPlayGroupUpdate(session.Id, error, CancellationToken.None);
                }
            }
        }

        /// <inheritdoc />
        public List<GroupInfoDto> ListGroups(SessionInfo session, ListGroupsRequest request)
        {
            if (session is null)
            {
                throw new InvalidOperationException("Session is null!");
            }

            if (request is null)
            {
                throw new InvalidOperationException("Request is null!");
            }

            var user = _userManager.GetUserById(session.UserId);
            List<GroupInfoDto> list = new List<GroupInfoDto>();

            lock (_groupsLock)
            {
                foreach (var (_, group) in _groups)
                {
                    // Locking required as group is not thread-safe.
                    lock (group)
                    {
                        if (group.HasAccessToPlayQueue(user))
                        {
                            list.Add(group.GetInfo());
                        }
                    }
                }
            }

            return list;
        }

        /// <inheritdoc />
        public void HandleRequest(SessionInfo session, IGroupPlaybackRequest request, CancellationToken cancellationToken)
        {
            if (session is null)
            {
                throw new InvalidOperationException("Session is null!");
            }

            if (request is null)
            {
                throw new InvalidOperationException("Request is null!");
            }

            if (_sessionToGroupMap.TryGetValue(session.Id, out var group))
            {
                // Group lock required as Group is not thread-safe.
                lock (group)
                {
                    // Make sure that session still belongs to this group.
                    if (_sessionToGroupMap.TryGetValue(session.Id, out var checkGroup) && !checkGroup.GroupId.Equals(group.GroupId))
                    {
                        // Drop request.
                        return;
                    }

                    // Drop request if group is empty.
                    if (group.IsGroupEmpty())
                    {
                        return;
                    }

                    // Apply requested changes to group.
                    group.HandleRequest(session, request, cancellationToken);
                }
            }
            else
            {
                _logger.LogWarning("Session {SessionId} does not belong to any group.", session.Id);

                var error = new GroupUpdate<string>(Guid.Empty, GroupUpdateType.NotInGroup, string.Empty);
                _sessionManager.SendSyncPlayGroupUpdate(session.Id, error, CancellationToken.None);
            }
        }

        /// <inheritdoc />
        public bool IsUserActive(Guid userId)
        {
            if (_activeUsers.TryGetValue(userId, out var sessionsCounter))
            {
                return sessionsCounter > 0;
            }

            return false;
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

            _sessionManager.SessionEnded -= OnSessionEnded;
            _disposed = true;
        }

        private void OnSessionEnded(object sender, SessionEventArgs e)
        {
            var session = e.SessionInfo;

            if (_sessionToGroupMap.TryGetValue(session.Id, out _))
            {
                var leaveGroupRequest = new LeaveGroupRequest();
                LeaveGroup(session, leaveGroupRequest, CancellationToken.None);
            }
        }

        private void UpdateSessionsCounter(Guid userId, int toAdd)
        {
            // Update sessions counter.
            var newSessionsCounter = _activeUsers.AddOrUpdate(
                userId,
                1,
                (_, sessionsCounter) => sessionsCounter + toAdd);

            // Should never happen.
            if (newSessionsCounter < 0)
            {
                throw new InvalidOperationException("Sessions counter is negative!");
            }

            // Clean record if user has no more active sessions.
            if (newSessionsCounter == 0)
            {
                _activeUsers.TryRemove(new KeyValuePair<Guid, int>(userId, newSessionsCounter));
            }
        }
    }
}
