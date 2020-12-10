namespace Jellyfin.Api.Models.SyncPlay.Dtos
{
    /// <summary>
    /// Class UserGroupAccessDto.
    /// </summary>
    public class UserGroupAccessDto
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UserGroupAccessDto"/> class.
        /// </summary>
        /// <param name="playbackAccess">Whether the user has playback access.</param>
        /// <param name="playlistAccess">Whether the user has playlist access.</param>
        public UserGroupAccessDto(bool playbackAccess, bool playlistAccess)
        {
            PlaybackAccess = playbackAccess;
            PlaylistAccess = playlistAccess;
        }

        /// <summary>
        /// Gets a value indicating whether the user has playback access.
        /// </summary>
        /// <value><c>true</c> if user has access to playback; <c>false</c> otherwise.</value>
        public bool PlaybackAccess { get; }

        /// <summary>
        /// Gets a value indicating whether the user has playlist access.
        /// </summary>
        /// <value><c>true</c> if user has access to playlist; <c>false</c> otherwise.</value>
        public bool PlaylistAccess { get; }
    }
}
