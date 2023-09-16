#pragma warning disable CS1591

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
        Task AddToPlaylistAsync(Guid playlistId, IReadOnlyCollection<Guid> itemIds, Guid userId);

        /// <summary>
        /// Removes from playlist.
        /// </summary>
        /// <param name="playlistId">The playlist identifier.</param>
        /// <param name="entryIds">The entry ids.</param>
        /// <returns>Task.</returns>
        Task RemoveFromPlaylistAsync(string playlistId, IEnumerable<string> entryIds);

        /// <summary>
        /// Gets the playlists folder.
        /// </summary>
        /// <returns>Folder.</returns>
        Folder GetPlaylistsFolder();

        /// <summary>
        /// Gets the playlists folder for a user.
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
        Task MoveItemAsync(string playlistId, string entryId, int newIndex);

        /// <summary>
        /// Removed all playlists of a user.
        /// If the playlist is shared, ownership is transferred.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <returns>Task.</returns>
        Task RemovePlaylistsAsync(Guid userId);

        /// <summary>
        /// Saves a playlist.
        /// </summary>
        /// <param name="item">The playlist.</param>
        void SavePlaylistFile(Playlist item);
    }
}
