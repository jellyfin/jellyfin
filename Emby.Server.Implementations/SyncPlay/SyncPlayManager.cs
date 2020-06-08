using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using MediaBrowser.Controller.SyncPlay;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.SyncPlay;

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
        /// The map between sessions and groups.
        /// </summary>
        private readonly Dictionary<string, ISyncPlayController> _sessionToGroupMap =
            new Dictionary<string, ISyncPlayController>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// The groups.
        /// </summary>
        private readonly Dictionary<Guid, ISyncPlayController> _groups =
            new Dictionary<Guid, ISyncPlayController>();

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

            _sessionManager.SessionEnded += OnSessionManagerSessionEnded;
            _sessionManager.PlaybackStopped += OnSessionManagerPlaybackStopped;
        }

        /// <summary>
        /// Gets all groups.
        /// </summary>
        /// <value>All groups.</value>
        public IEnumerable<ISyncPlayController> Groups => _groups.Values;

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

            _sessionManager.SessionEnded -= OnSessionManagerSessionEnded;
            _sessionManager.PlaybackStopped -= OnSessionManagerPlaybackStopped;

            _disposed = true;
        }

        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        private void OnSessionManagerSessionEnded(object sender, SessionEventArgs e)
        {
            var session = e.SessionInfo;
            if (!IsSessionInGroup(session))
            {
                return;
            }

            LeaveGroup(session, CancellationToken.None);
        }

        private void OnSessionManagerPlaybackStopped(object sender, PlaybackStopEventArgs e)
        {
            var session = e.Session;
            if (!IsSessionInGroup(session))
            {
                return;
            }

            LeaveGroup(session, CancellationToken.None);
        }

        private bool IsSessionInGroup(SessionInfo session)
        {
            return _sessionToGroupMap.ContainsKey(session.Id);
        }

        private bool HasAccessToItem(User user, Guid itemId)
        {
            var item = _libraryManager.GetItemById(itemId);

            // Check ParentalRating access
            var hasParentalRatingAccess = true;
            if (user.Policy.MaxParentalRating.HasValue)
            {
                hasParentalRatingAccess = item.InheritedParentalRatingValue <= user.Policy.MaxParentalRating;
            }

            if (!user.Policy.EnableAllFolders && hasParentalRatingAccess)
            {
                var collections = _libraryManager.GetCollectionFolders(item).Select(
                    folder => folder.Id.ToString("N", CultureInfo.InvariantCulture)
                );
                var intersect = collections.Intersect(user.Policy.EnabledFolders);
                return intersect.Any();
            }
            else
            {
                return hasParentalRatingAccess;
            }
        }

        private Guid? GetSessionGroup(SessionInfo session)
        {
            ISyncPlayController group;
            _sessionToGroupMap.TryGetValue(session.Id, out group);
            if (group != null)
            {
                return group.GetGroupId();
            }
            else
            {
                return null;
            }
        }

        /// <inheritdoc />
        public void NewGroup(SessionInfo session, CancellationToken cancellationToken)
        {
            var user = _userManager.GetUserById(session.UserId);

            if (user.Policy.SyncPlayAccess != SyncPlayAccess.CreateAndJoinGroups)
            {
                _logger.LogWarning("NewGroup: {0} does not have permission to create groups.", session.Id);

                var error = new GroupUpdate<string>()
                {
                    Type = GroupUpdateType.CreateGroupDenied
                };
                _sessionManager.SendSyncPlayGroupUpdate(session.Id.ToString(), error, CancellationToken.None);
                return;
            }

            lock (_groupsLock)
            {
                if (IsSessionInGroup(session))
                {
                    LeaveGroup(session, cancellationToken);
                }

                var group = new SyncPlayController(_sessionManager, this);
                _groups[group.GetGroupId()] = group;

                group.InitGroup(session, cancellationToken);
            }
        }

        /// <inheritdoc />
        public void JoinGroup(SessionInfo session, Guid groupId, JoinGroupRequest request, CancellationToken cancellationToken)
        {
            var user = _userManager.GetUserById(session.UserId);

            if (user.Policy.SyncPlayAccess == SyncPlayAccess.None)
            {
                _logger.LogWarning("JoinGroup: {0} does not have access to SyncPlay.", session.Id);

                var error = new GroupUpdate<string>()
                {
                    Type = GroupUpdateType.JoinGroupDenied
                };
                _sessionManager.SendSyncPlayGroupUpdate(session.Id.ToString(), error, CancellationToken.None);
                return;
            }

            lock (_groupsLock)
            {
                ISyncPlayController group;
                _groups.TryGetValue(groupId, out group);

                if (group == null)
                {
                    _logger.LogWarning("JoinGroup: {0} tried to join group {0} that does not exist.", session.Id, groupId);

                    var error = new GroupUpdate<string>()
                    {
                        Type = GroupUpdateType.GroupDoesNotExist
                    };
                    _sessionManager.SendSyncPlayGroupUpdate(session.Id.ToString(), error, CancellationToken.None);
                    return;
                }

                if (!HasAccessToItem(user, group.GetPlayingItemId()))
                {
                    _logger.LogWarning("JoinGroup: {0} does not have access to {1}.", session.Id, group.GetPlayingItemId());

                    var error = new GroupUpdate<string>()
                    {
                        GroupId = group.GetGroupId().ToString(),
                        Type = GroupUpdateType.LibraryAccessDenied
                    };
                    _sessionManager.SendSyncPlayGroupUpdate(session.Id.ToString(), error, CancellationToken.None);
                    return;
                }

                if (IsSessionInGroup(session))
                {
                    if (GetSessionGroup(session).Equals(groupId))
                    {
                        return;
                    }

                    LeaveGroup(session, cancellationToken);
                }

                group.SessionJoin(session, request, cancellationToken);
            }
        }

        /// <inheritdoc />
        public void LeaveGroup(SessionInfo session, CancellationToken cancellationToken)
        {
            // TODO: determine what happens to users that are in a group and get their permissions revoked
            lock (_groupsLock)
            {
                ISyncPlayController group;
                _sessionToGroupMap.TryGetValue(session.Id, out group);

                if (group == null)
                {
                    _logger.LogWarning("LeaveGroup: {0} does not belong to any group.", session.Id);

                    var error = new GroupUpdate<string>()
                    {
                        Type = GroupUpdateType.NotInGroup
                    };
                    _sessionManager.SendSyncPlayGroupUpdate(session.Id.ToString(), error, CancellationToken.None);
                    return;
                }

                group.SessionLeave(session, cancellationToken);

                if (group.IsGroupEmpty())
                {
                    _logger.LogInformation("LeaveGroup: removing empty group {0}.", group.GetGroupId());
                    _groups.Remove(group.GetGroupId(), out _);
                }
            }
        }

        /// <inheritdoc />
        public List<GroupInfoView> ListGroups(SessionInfo session, Guid filterItemId)
        {
            var user = _userManager.GetUserById(session.UserId);

            if (user.Policy.SyncPlayAccess == SyncPlayAccess.None)
            {
                return new List<GroupInfoView>();
            }

            // Filter by item if requested
            if (!filterItemId.Equals(Guid.Empty))
            {
                return _groups.Values.Where(
                    group => group.GetPlayingItemId().Equals(filterItemId) && HasAccessToItem(user, group.GetPlayingItemId())
                ).Select(
                    group => group.GetInfo()
                ).ToList();
            }
            // Otherwise show all available groups
            else
            {
                return _groups.Values.Where(
                    group => HasAccessToItem(user, group.GetPlayingItemId())
                ).Select(
                    group => group.GetInfo()
                ).ToList();
            }
        }

        /// <inheritdoc />
        public void HandleRequest(SessionInfo session, PlaybackRequest request, CancellationToken cancellationToken)
        {
            var user = _userManager.GetUserById(session.UserId);

            if (user.Policy.SyncPlayAccess == SyncPlayAccess.None)
            {
                _logger.LogWarning("HandleRequest: {0} does not have access to SyncPlay.", session.Id);

                var error = new GroupUpdate<string>()
                {
                    Type = GroupUpdateType.JoinGroupDenied
                };
                _sessionManager.SendSyncPlayGroupUpdate(session.Id.ToString(), error, CancellationToken.None);
                return;
            }

            lock (_groupsLock)
            {
                ISyncPlayController group;
                _sessionToGroupMap.TryGetValue(session.Id, out group);

                if (group == null)
                {
                    _logger.LogWarning("HandleRequest: {0} does not belong to any group.", session.Id);

                    var error = new GroupUpdate<string>()
                    {
                        Type = GroupUpdateType.NotInGroup
                    };
                    _sessionManager.SendSyncPlayGroupUpdate(session.Id.ToString(), error, CancellationToken.None);
                    return;
                }

                group.HandleRequest(session, request, cancellationToken);
            }
        }

        /// <inheritdoc />
        public void AddSessionToGroup(SessionInfo session, ISyncPlayController group)
        {
            if (IsSessionInGroup(session))
            {
                throw new InvalidOperationException("Session in other group already!");
            }

            _sessionToGroupMap[session.Id] = group;
        }

        /// <inheritdoc />
        public void RemoveSessionFromGroup(SessionInfo session, ISyncPlayController group)
        {
            if (!IsSessionInGroup(session))
            {
                throw new InvalidOperationException("Session not in any group!");
            }

            ISyncPlayController tempGroup;
            _sessionToGroupMap.Remove(session.Id, out tempGroup);

            if (!tempGroup.GetGroupId().Equals(group.GetGroupId()))
            {
                throw new InvalidOperationException("Session was in wrong group!");
            }
        }
    }
}
