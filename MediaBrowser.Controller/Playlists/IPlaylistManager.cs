using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Playlists;

namespace MediaBrowser.Controller.Playlists
{
    public interface IPlaylistManager
    {
        /// <summary>
        /// Gets the playlists.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <returns>IEnumerable&lt;Playlist&gt;.</returns>
        IEnumerable<Playlist> GetPlaylists(Guid userId);

        /// <summary>
        /// Creates the playlist.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <returns>Task&lt;Playlist&gt;.</returns>
        Task<PlaylistCreationResult> CreatePlaylist(PlaylistCreationRequest options);

        /// <summary>
        /// Adds to playlist.
        /// </summary>
        /// <param name="playlistId">The playlist identifier.</param>
        /// <param name="itemIds">The item ids.</param>
        /// <param name="userId">The user identifier.</param>
        /// <returns>Task.</returns>
        void AddToPlaylist(string playlistId, ICollection<Guid> itemIds, Guid userId);

        /// <summary>
        /// Removes from playlist.
        /// </summary>
        /// <param name="playlistId">The playlist identifier.</param>
        /// <param name="entryIds">The entry ids.</param>
        /// <returns>Task.</returns>
        void RemoveFromPlaylist(string playlistId, IEnumerable<string> entryIds);

        /// <summary>
        /// Gets the playlists folder.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <returns>Folder.</returns>
        Folder GetPlaylistsFolder(Guid userId);

        /// <summary>
        /// Moves the item.
        /// </summary>
        /// <param name="playlistId">The playlist identifier.</param>
        /// <param name="entryId">The entry identifier.</param>
        /// <param name="newIndex">The new index.</param>
        /// <returns>Task.</returns>
        void MoveItem(string playlistId, string entryId, int newIndex);
    }
}
