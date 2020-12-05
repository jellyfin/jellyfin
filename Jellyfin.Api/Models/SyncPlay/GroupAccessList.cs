using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Api.Models.SyncPlay.Dtos;
using MediaBrowser.Controller.Session;

namespace Jellyfin.Api.Models.SyncPlay
{
    /// <summary>
    /// Class that manages the access to a group. Access control includes list of users allowed to join, playback and playlist access.
    /// </summary>
    public class GroupAccessList
    {
        /// <summary>
        /// The list of users that manage the group.
        /// </summary>
        private readonly HashSet<Guid> _administrators;

        /// <summary>
        /// The access list.
        /// </summary>
        private readonly Dictionary<Guid, UserGroupAccessDto> _accessList;

        /// <summary>
        /// Initializes a new instance of the <see cref="GroupAccessList" /> class.
        /// </summary>
        public GroupAccessList()
        {
            _administrators = new HashSet<Guid>();
            _accessList = new Dictionary<Guid, UserGroupAccessDto>();
        }

        /// <summary>
        /// Gets or sets a value indicating whether new members will have playback access.
        /// </summary>
        /// <value><c>true</c> if new members will have playback access; <c>false</c> otherwise.</value>
        public bool OpenPlaybackAccess { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether new members will have playlist access.
        /// </summary>
        /// <value><c>true</c> if new members will have playlist access; <c>false</c> otherwise.</value>
        public bool OpenPlaylistAccess { get; set; }

        /// <summary>
        /// Gets the list of request types that are always allowed.
        /// </summary>
        /// <value>The allowed requests list.</value>
        public IReadOnlyList<PlaybackRequestType> AllowedRequests { get; } = new List<PlaybackRequestType>
        {
            PlaybackRequestType.Buffer,
            PlaybackRequestType.Ready,
            PlaybackRequestType.Ping,
            PlaybackRequestType.IgnoreWait,
            PlaybackRequestType.WebRTC
        };

        /// <summary>
        /// Gets the list of request types that make up the Playback access list.
        /// </summary>
        /// <value>The playback requests list.</value>
        public IReadOnlyList<PlaybackRequestType> PlaybackRequests { get; } = new List<PlaybackRequestType>
        {
            PlaybackRequestType.SetPlaylistItem,
            PlaybackRequestType.Unpause,
            PlaybackRequestType.Pause,
            PlaybackRequestType.Stop,
            PlaybackRequestType.Seek,
            PlaybackRequestType.NextItem,
            PlaybackRequestType.PreviousItem
        };

        /// <summary>
        /// Gets the list of request types that make up the Playlist access list.
        /// </summary>
        /// <value>The playlists request list.</value>
        public IReadOnlyList<PlaybackRequestType> PlaylistRequests { get; } = new List<PlaybackRequestType>
        {
            PlaybackRequestType.Play,
            PlaybackRequestType.RemoveFromPlaylist,
            PlaybackRequestType.MovePlaylistItem,
            PlaybackRequestType.Queue,
            PlaybackRequestType.SetRepeatMode,
            PlaybackRequestType.SetShuffleMode
        };

        /// <summary>
        /// Adds a user to the list of administrators.
        /// </summary>
        /// <param name="userId">The user id.</param>
        public void AddAdministrator(Guid userId)
        {
            _administrators.Add(userId);
        }

        /// <summary>
        /// Removes a user from the list of administrators.
        /// </summary>
        /// <param name="userId">The user id.</param>
        public void RemoveAdministrator(Guid userId)
        {
            _administrators.Remove(userId);
        }

        /// <summary>
        /// Checks if a user is an administrator of this group.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <returns><c>true</c> if the user is administrator; <c>false</c> otherwise.</returns>
        public bool IsAdministrator(Guid userId)
        {
            return _administrators.Contains(userId);
        }

        /// <summary>
        /// Gets the administrators of the group.
        /// </summary>
        /// <returns>The list of user identifiers.</returns>
        public IReadOnlyList<Guid> GetAdministrators()
        {
            return _administrators.ToList();
        }

        /// <summary>
        /// Creates the permissions for a user if these are not already set.
        /// </summary>
        /// <param name="userId">The user id.</param>
        public void TouchPermissions(Guid userId)
        {
            if (!_accessList.ContainsKey(userId))
            {
                _accessList[userId] = new UserGroupAccessDto(OpenPlaybackAccess, OpenPlaylistAccess);
            }
        }

        /// <summary>
        /// Sets the permissions for a user.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="playbackAccess">The playback access.</param>
        /// <param name="playlistAccess">The playlist access.</param>
        public void SetPermissions(Guid userId, bool playbackAccess, bool playlistAccess)
        {
            _accessList[userId] = new UserGroupAccessDto(playbackAccess, playlistAccess);
        }

        /// <summary>
        /// Clears the permissions set for a user.
        /// </summary>
        /// <param name="userId">The user id.</param>
        public void ClearPermissions(Guid userId)
        {
            _accessList.Remove(userId);
        }

        /// <summary>
        /// Gets the access list.
        /// </summary>
        /// <returns>The read-only access list.</returns>
        public IReadOnlyDictionary<Guid, UserGroupAccessDto> GetAccessList()
        {
            return _accessList;
        }

        /// <summary>
        /// Checks whether the requested action by the session is allowed.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="request">The requested action.</param>
        /// <returns><c>true</c> if the request is allowed; <c>false</c> otherwise.</returns>
        public bool CheckRequest(SessionInfo session, IGroupPlaybackRequest request)
        {
            if (IsAdministrator(session.UserId))
            {
                return true;
            }

            bool isAlwaysAllowedRequest = AllowedRequests.Contains(request.Action);

            if (isAlwaysAllowedRequest)
            {
                return true;
            }
            else if (_accessList.TryGetValue(session.UserId, out UserGroupAccessDto? userAccess))
            {
                bool needsPlaybackAccess = PlaybackRequests.Contains(request.Action);
                bool needsPlaylistAccess = PlaylistRequests.Contains(request.Action);
                bool playbackCheckPassed = !needsPlaybackAccess || userAccess.PlaybackAccess;
                bool playlistCheckPassed = !needsPlaylistAccess || userAccess.PlaylistAccess;

                return playbackCheckPassed && playlistCheckPassed;
            }
            else
            {
                return false;
            }
        }
    }
}
