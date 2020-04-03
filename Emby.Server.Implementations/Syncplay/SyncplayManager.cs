using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using MediaBrowser.Controller.Syncplay;
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
        /// The session manager.
        /// </summary>
        private readonly ISessionManager _sessionManager;

        /// <summary>
        /// The map between users and groups.
        /// </summary>
        private readonly ConcurrentDictionary<string, ISyncplayController> _userToGroupMap =
            new ConcurrentDictionary<string, ISyncplayController>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// The groups.
        /// </summary>
        private readonly ConcurrentDictionary<string, ISyncplayController> _groups =
        new ConcurrentDictionary<string, ISyncplayController>(StringComparer.OrdinalIgnoreCase);

        private bool _disposed = false;

        public SyncplayManager(
            ILogger<SyncplayManager> logger,
            ISessionManager sessionManager)
        {
            _logger = logger;
            _sessionManager = sessionManager;

            _sessionManager.SessionEnded += _sessionManager_SessionEnded;
            _sessionManager.PlaybackStopped += _sessionManager_PlaybackStopped;
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

            _sessionManager.SessionEnded -= _sessionManager_SessionEnded;
            _sessionManager.PlaybackStopped -= _sessionManager_PlaybackStopped;

            _disposed = true;
        }

        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        void _sessionManager_SessionEnded(object sender, SessionEventArgs e)
        {
            var user = e.SessionInfo;
            if (!IsUserInGroup(user)) return;
            LeaveGroup(user);
        }

        void _sessionManager_PlaybackStopped(object sender, PlaybackStopEventArgs e)
        {
            var user = e.Session;
            if (!IsUserInGroup(user)) return;
            LeaveGroup(user);
        }

        private bool IsUserInGroup(SessionInfo user)
        {
            return _userToGroupMap.ContainsKey(user.Id);
        }

        private Guid? GetUserGroup(SessionInfo user)
        {
            ISyncplayController group;
            _userToGroupMap.TryGetValue(user.Id, out group);
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
        public void NewGroup(SessionInfo user)
        {
            if (IsUserInGroup(user))
            {
                LeaveGroup(user);
            }

            var group = new SyncplayController(_logger, _sessionManager, this);
            _groups[group.GetGroupId().ToString()] = group;

            group.InitGroup(user);
        }

        /// <inheritdoc />
        public void JoinGroup(SessionInfo user, string groupId)
        {
            if (IsUserInGroup(user))
            {
                if (GetUserGroup(user).Equals(groupId)) return;
                LeaveGroup(user);
            }

            ISyncplayController group;
            _groups.TryGetValue(groupId, out group);

            if (group == null)
            {
                _logger.LogError("Syncplaymanager JoinGroup: " + groupId + " does not exist.");

                var update = new SyncplayGroupUpdate<string>();
                update.Type = SyncplayGroupUpdateType.NotInGroup;
                _sessionManager.SendSyncplayGroupUpdate(user.Id.ToString(), update, CancellationToken.None);
                return;
            }
            group.UserJoin(user);
        }

        /// <inheritdoc />
        public void LeaveGroup(SessionInfo user)
        {
            ISyncplayController group;
            _userToGroupMap.TryGetValue(user.Id, out group);

            if (group == null)
            {
                _logger.LogWarning("Syncplaymanager HandleRequest: " + user.Id + " not in group.");

                var update = new SyncplayGroupUpdate<string>();
                update.Type = SyncplayGroupUpdateType.NotInGroup;
                _sessionManager.SendSyncplayGroupUpdate(user.Id.ToString(), update, CancellationToken.None);
                return;
            }
            group.UserLeave(user);

            if (group.IsGroupEmpty())
            {
                _groups.Remove(group.GetGroupId().ToString(), out _);
            }
        }

        /// <inheritdoc />
        public List<GroupInfoView> ListGroups(SessionInfo user)
        {
            // Filter by playing item if the user is viewing something already
            if (user.NowPlayingItem != null)
            {
                return _groups.Values.Where(
                    group => group.GetPlayingItemId().Equals(user.FullNowPlayingItem.Id)
                ).Select(
                    group => group.GetInfo()
                ).ToList();
            }
            // Otherwise show all available groups
            else
            {
                return _groups.Values.Select(
                    group => group.GetInfo()
                ).ToList();
            }
        }

        /// <inheritdoc />
        public void HandleRequest(SessionInfo user, SyncplayRequestInfo request)
        {
            ISyncplayController group;
            _userToGroupMap.TryGetValue(user.Id, out group);

            if (group == null)
            {
                _logger.LogWarning("Syncplaymanager HandleRequest: " + user.Id + " not in group.");

                var update = new SyncplayGroupUpdate<string>();
                update.Type = SyncplayGroupUpdateType.NotInGroup;
                _sessionManager.SendSyncplayGroupUpdate(user.Id.ToString(), update, CancellationToken.None);
                return;
            }
            group.HandleRequest(user, request);
        }
        
        /// <inheritdoc />
        public void MapUserToGroup(SessionInfo user, ISyncplayController group)
        {
            if (IsUserInGroup(user))
            {
                throw new InvalidOperationException("User in other group already!");
            }
            _userToGroupMap[user.Id] = group;
        }

        /// <inheritdoc />
        public void UnmapUserFromGroup(SessionInfo user, ISyncplayController group)
        {
            if (!IsUserInGroup(user))
            {
                throw new InvalidOperationException("User not in any group!");
            }

            ISyncplayController tempGroup;
            _userToGroupMap.Remove(user.Id, out tempGroup);

            if (!tempGroup.GetGroupId().Equals(group.GetGroupId()))
            {
                throw new InvalidOperationException("User was in wrong group!");
            }
        }
    }
}
