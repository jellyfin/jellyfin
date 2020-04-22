using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using MediaBrowser.Controller.Syncplay;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Syncplay;

namespace Emby.Server.Implementations.Syncplay
{
    /// <summary>
    /// Class SyncplayManager.
    /// </summary>
    public class SyncplayManager : ISyncplayManager, IDisposable
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
        private readonly ConcurrentDictionary<string, ISyncplayController> _sessionToGroupMap =
            new ConcurrentDictionary<string, ISyncplayController>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// The groups.
        /// </summary>
        private readonly ConcurrentDictionary<string, ISyncplayController> _groups =
            new ConcurrentDictionary<string, ISyncplayController>(StringComparer.OrdinalIgnoreCase);

        private bool _disposed = false;

        public SyncplayManager(
            ILogger<SyncplayManager> logger,
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
        public IEnumerable<ISyncplayController> Groups => _groups.Values;

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
            if (!IsSessionInGroup(session)) return;
            LeaveGroup(session);
        }

        private void OnSessionManagerPlaybackStopped(object sender, PlaybackStopEventArgs e)
        {
            var session = e.Session;
            if (!IsSessionInGroup(session)) return;
            LeaveGroup(session);
        }

        private bool IsSessionInGroup(SessionInfo session)
        {
            return _sessionToGroupMap.ContainsKey(session.Id);
        }

        private bool HasAccessToItem(User user, Guid itemId)
        {
            var item = _libraryManager.GetItemById(itemId);
            var hasParentalRatingAccess = user.Policy.MaxParentalRating.HasValue ? item.InheritedParentalRatingValue <= user.Policy.MaxParentalRating : true;

            if (!user.Policy.EnableAllFolders && hasParentalRatingAccess)
            {
                var collections = _libraryManager.GetCollectionFolders(item).Select(
                    folder => folder.Id.ToString("N", CultureInfo.InvariantCulture)
                );
                var intersect = collections.Intersect(user.Policy.EnabledFolders);
                return intersect.Count() > 0;
            }
            else
            {
                return hasParentalRatingAccess;
            }
        }

        private Guid? GetSessionGroup(SessionInfo session)
        {
            ISyncplayController group;
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
        public void NewGroup(SessionInfo session)
        {
            var user = _userManager.GetUserById(session.UserId);

            if (user.Policy.SyncplayAccess != SyncplayAccess.CreateAndJoinGroups)
            {
                _logger.LogWarning("Syncplaymanager NewGroup: {0} does not have permission to create groups.", session.Id);

                var error = new GroupUpdate<string>()
                {
                    Type = GroupUpdateType.CreateGroupDenied
                };
                _sessionManager.SendSyncplayGroupUpdate(session.Id.ToString(), error, CancellationToken.None);
                return;
            }

            if (IsSessionInGroup(session))
            {
                LeaveGroup(session);
            }

            var group = new SyncplayController(_logger, _sessionManager, this);
            _groups[group.GetGroupId().ToString()] = group;

            group.InitGroup(session);
        }

        /// <inheritdoc />
        public void JoinGroup(SessionInfo session, string groupId, JoinGroupRequest request)
        {
            var user = _userManager.GetUserById(session.UserId);

            if (user.Policy.SyncplayAccess == SyncplayAccess.None)
            {
                _logger.LogWarning("Syncplaymanager JoinGroup: {0} does not have access to Syncplay.", session.Id);

                var error = new GroupUpdate<string>()
                {
                    Type = GroupUpdateType.JoinGroupDenied
                };
                _sessionManager.SendSyncplayGroupUpdate(session.Id.ToString(), error, CancellationToken.None);
                return;
            }

            ISyncplayController group;
            _groups.TryGetValue(groupId, out group);

            if (group == null)
            {
                _logger.LogWarning("Syncplaymanager JoinGroup: {0} tried to join group {0} that does not exist.", session.Id, groupId);

                var error = new GroupUpdate<string>()
                {
                    Type = GroupUpdateType.GroupNotJoined
                };
                _sessionManager.SendSyncplayGroupUpdate(session.Id.ToString(), error, CancellationToken.None);
                return;
            }

            if (!HasAccessToItem(user, group.GetPlayingItemId()))
            {
                _logger.LogWarning("Syncplaymanager JoinGroup: {0} does not have access to {1}.", session.Id, group.GetPlayingItemId());

                var error = new GroupUpdate<string>()
                {
                    GroupId = group.GetGroupId().ToString(),
                    Type = GroupUpdateType.LibraryAccessDenied
                };
                _sessionManager.SendSyncplayGroupUpdate(session.Id.ToString(), error, CancellationToken.None);
                return;
            }

            if (IsSessionInGroup(session))
            {
                if (GetSessionGroup(session).Equals(groupId)) return;
                LeaveGroup(session);
            }

            group.SessionJoin(session, request);
        }

        /// <inheritdoc />
        public void LeaveGroup(SessionInfo session)
        {
            // TODO: determine what happens to users that are in a group and get their permissions revoked

            ISyncplayController group;
            _sessionToGroupMap.TryGetValue(session.Id, out group);

            if (group == null)
            {
                _logger.LogWarning("Syncplaymanager LeaveGroup: {0} does not belong to any group.", session.Id);

                var error = new GroupUpdate<string>()
                {
                    Type = GroupUpdateType.NotInGroup
                };
                _sessionManager.SendSyncplayGroupUpdate(session.Id.ToString(), error, CancellationToken.None);
                return;
            }
            group.SessionLeave(session);

            if (group.IsGroupEmpty())
            {
                _groups.Remove(group.GetGroupId().ToString(), out _);
            }
        }

        /// <inheritdoc />
        public List<GroupInfoView> ListGroups(SessionInfo session)
        {
            var user = _userManager.GetUserById(session.UserId);

            if (user.Policy.SyncplayAccess == SyncplayAccess.None)
            {
                return new List<GroupInfoView>();
            }

            // Filter by playing item if the user is viewing something already
            if (session.NowPlayingItem != null)
            {
                return _groups.Values.Where(
                    group => group.GetPlayingItemId().Equals(session.FullNowPlayingItem.Id) && HasAccessToItem(user, group.GetPlayingItemId())
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
        public void HandleRequest(SessionInfo session, PlaybackRequest request)
        {
            var user = _userManager.GetUserById(session.UserId);

            if (user.Policy.SyncplayAccess == SyncplayAccess.None)
            {
                _logger.LogWarning("Syncplaymanager HandleRequest: {0} does not have access to Syncplay.", session.Id);

                var error = new GroupUpdate<string>()
                {
                    Type = GroupUpdateType.JoinGroupDenied
                };
                _sessionManager.SendSyncplayGroupUpdate(session.Id.ToString(), error, CancellationToken.None);
                return;
            }

            ISyncplayController group;
            _sessionToGroupMap.TryGetValue(session.Id, out group);

            if (group == null)
            {
                _logger.LogWarning("Syncplaymanager HandleRequest: {0} does not belong to any group.", session.Id);

                var error = new GroupUpdate<string>()
                {
                    Type = GroupUpdateType.NotInGroup
                };
                _sessionManager.SendSyncplayGroupUpdate(session.Id.ToString(), error, CancellationToken.None);
                return;
            }
            group.HandleRequest(session, request);
        }

        /// <inheritdoc />
        public void AddSessionToGroup(SessionInfo session, ISyncplayController group)
        {
            if (IsSessionInGroup(session))
            {
                throw new InvalidOperationException("Session in other group already!");
            }
            _sessionToGroupMap[session.Id] = group;
        }

        /// <inheritdoc />
        public void RemoveSessionFromGroup(SessionInfo session, ISyncplayController group)
        {
            if (!IsSessionInGroup(session))
            {
                throw new InvalidOperationException("Session not in any group!");
            }

            ISyncplayController tempGroup;
            _sessionToGroupMap.Remove(session.Id, out tempGroup);

            if (!tempGroup.GetGroupId().Equals(group.GetGroupId()))
            {
                throw new InvalidOperationException("Session was in wrong group!");
            }
        }
    }
}
