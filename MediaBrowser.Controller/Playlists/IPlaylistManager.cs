using MediaBrowser.Controller.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Playlists
{
    public interface IPlaylistManager
    {
        /// <summary>
        /// Gets the playlists.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <returns>IEnumerable&lt;Playlist&gt;.</returns>
        IEnumerable<Playlist> GetPlaylists(string userId);

        /// <summary>
        /// Creates the playlist.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <returns>Task&lt;Playlist&gt;.</returns>
        Task<Playlist> CreatePlaylist(PlaylistCreationOptions options);

        /// <summary>
        /// Adds to playlist.
        /// </summary>
        /// <param name="playlistId">The playlist identifier.</param>
        /// <param name="itemIds">The item ids.</param>
        /// <returns>Task.</returns>
        Task AddToPlaylist(string playlistId, IEnumerable<string> itemIds);

        /// <summary>
        /// Removes from playlist.
        /// </summary>
        /// <param name="playlistId">The playlist identifier.</param>
        /// <param name="entryIds">The entry ids.</param>
        /// <returns>Task.</returns>
        Task RemoveFromPlaylist(string playlistId, IEnumerable<string> entryIds);

        /// <summary>
        /// Gets the playlists folder.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <returns>Folder.</returns>
        Folder GetPlaylistsFolder(string userId);

    }
}
