#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Playlists;

namespace MediaBrowser.Controller.Playlists
{
    public interface IPlaylistManager
    {
        /// <summary>
        /// Gets the playlist.
        /// </summary>
        /// <param name="playlistId">The playlist identifier.</param>
        /// <param name="userId">The user identifier.</param>
        /// <returns>Playlist.</returns>
        Playlist GetPlaylistForUser(Guid playlistId, Guid userId);

        /// <summary>
        /// Creates the playlist.
        /// </summary>
        /// <param name="request">The <see cref="PlaylistCreationRequest"/>.</param>
        /// <returns>The created playlist.</returns>
        Task<PlaylistCreationResult> CreatePlaylist(PlaylistCreationRequest request);

        /// <summary>
        /// Updates a playlist.
        /// </summary>
        /// <param name="request">The <see cref="PlaylistUpdateRequest"/>.</param>
        /// <returns>Task.</returns>
        Task UpdatePlaylist(PlaylistUpdateRequest request);

        /// <summary>
        /// Gets all playlists a user has access to.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <returns>IEnumerable&lt;Playlist&gt;.</returns>
        IEnumerable<Playlist> GetPlaylists(Guid userId);

        /// <summary>
        /// Adds a share to the playlist.
        /// </summary>
        /// <param name="request">The <see cref="PlaylistUserUpdateRequest"/>.</param>
        /// <returns>Task.</returns>
        Task AddUserToShares(PlaylistUserUpdateRequest request);

        /// <summary>
        /// Removes a share from the playlist.
        /// </summary>
        /// <param name="playlistId">The playlist identifier.</param>
        /// <param name="userId">The user identifier.</param>
        /// <param name="share">The share.</param>
        /// <returns>Task.</returns>
        Task RemoveUserFromShares(Guid playlistId, Guid userId, PlaylistUserPermissions share);

        /// <summary>
        /// Adds to playlist.
        /// </summary>
        /// <param name="playlistId">The playlist identifier.</param>
        /// <param name="itemIds">The item ids.</param>
        /// <param name="userId">The user identifier.</param>
        /// <returns>Task.</returns>
        Task AddItemToPlaylistAsync(Guid playlistId, IReadOnlyCollection<Guid> itemIds, Guid userId);

        /// <summary>
        /// Removes from playlist.
        /// </summary>
        /// <param name="playlistId">The playlist identifier.</param>
        /// <param name="entryIds">The entry ids.</param>
        /// <returns>Task.</returns>
        Task RemoveItemFromPlaylistAsync(string playlistId, IEnumerable<string> entryIds);

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
        /// <param name="callingUserId">The calling user.</param>
        /// <returns>Task.</returns>
        Task MoveItemAsync(string playlistId, string entryId, int newIndex, Guid callingUserId);

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
