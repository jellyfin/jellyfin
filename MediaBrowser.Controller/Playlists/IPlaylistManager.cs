#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.IO;
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
        /// <param name="position">Optional. 0-based index where to place the items or at the end if null.</param>
        /// <param name="userId">The user identifier.</param>
        /// <returns>Task.</returns>
        Task AddItemToPlaylistAsync(Guid playlistId, IReadOnlyCollection<Guid> itemIds, int? position, Guid userId);

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

        /// <summary>
        /// Removes all LinkedChild entries whose underlying library item can no longer be resolved.
        /// </summary>
        /// <param name="playlistId">The playlist to clean up.</param>
        /// <returns>Task.</returns>
        Task PurgeBrokenItemsAsync(Guid playlistId);

        /// <summary>
        /// Reorders all items in a playlist to match the supplied entry-id sequence.
        /// Entry IDs not present in the playlist are ignored; entries present in the playlist
        /// but absent from the supplied list are moved to the end in their original relative order.
        /// </summary>
        /// <param name="playlistId">The playlist identifier.</param>
        /// <param name="orderedEntryIds">The desired order, expressed as PlaylistItemId (ItemId "N" format) strings.</param>
        /// <param name="callingUserId">The user requesting the change.</param>
        /// <returns>Task.</returns>
        Task ReorderItemsAsync(Guid playlistId, IReadOnlyList<string> orderedEntryIds, Guid callingUserId);

        /// <summary>
        /// Exports the playlist as an extended M3U8 string using absolute file paths.
        /// </summary>
        /// <param name="playlistId">The playlist identifier.</param>
        /// <returns>Extended M3U8 content as a string.</returns>
        Task<string> ExportAsM3u8Async(Guid playlistId);

        /// <summary>
        /// Exports the playlist as a portable Jellyfin JSON export, using provider IDs
        /// (IMDB, TMDB, TVDB, etc.) so it can be re-imported on any server with the same content.
        /// </summary>
        /// <param name="playlistId">The playlist identifier.</param>
        /// <returns>A <see cref="PlaylistExportDto"/> describing all exported items.</returns>
        Task<PlaylistExportDto> ExportAsJsonAsync(Guid playlistId);

        /// <summary>
        /// Creates a new playlist by importing an uploaded playlist file.
        /// Supports M3U/M3U8/PLS/WPL/ZPL (matched by file path) and Jellyfin JSON exports
        /// (matched by provider IDs, with a title+year fallback).
        /// Items that cannot be matched against the local library are silently skipped.
        /// </summary>
        /// <param name="fileStream">The uploaded file content.</param>
        /// <param name="fileName">The original filename including extension, used to detect the format.</param>
        /// <param name="userId">The user who will own the new playlist.</param>
        /// <param name="nameOverride">Optional name override; if null, the filename without extension is used.</param>
        /// <returns>The creation result containing the new playlist ID.</returns>
        Task<PlaylistCreationResult> ImportPlaylistAsync(Stream fileStream, string fileName, Guid userId, string? nameOverride = null);

        /// <summary>
        /// Creates a copy of an existing playlist owned by the calling user.
        /// </summary>
        /// <param name="sourcePlaylistId">The playlist to copy.</param>
        /// <param name="userId">The user who will own the clone.</param>
        /// <param name="newName">Optional name for the clone; defaults to "{original name} (Copy)".</param>
        /// <returns>The creation result containing the new playlist ID.</returns>
        Task<PlaylistCreationResult> ClonePlaylistAsync(Guid sourcePlaylistId, Guid userId, string? newName = null);

        /// <summary>
        /// Randomises the stored item order for a playlist using a Fisher-Yates shuffle.
        /// </summary>
        /// <param name="playlistId">The playlist identifier.</param>
        /// <param name="callingUserId">The user requesting the shuffle.</param>
        /// <returns>Task.</returns>
        Task ShuffleItemsAsync(Guid playlistId, Guid callingUserId);

        /// <summary>
        /// Returns the PlaylistItemIds of all duplicate entries.
        /// The first occurrence of each item is canonical and is not included in the returned list.
        /// </summary>
        /// <param name="playlistId">The playlist identifier.</param>
        /// <returns>PlaylistItemId strings for entries that are duplicates of an earlier entry.</returns>
        IReadOnlyList<string> GetDuplicateEntryIds(Guid playlistId);

        /// <summary>
        /// Removes all duplicate entries from the playlist, keeping the first occurrence of each item.
        /// </summary>
        /// <param name="playlistId">The playlist identifier.</param>
        /// <returns>Task.</returns>
        Task RemoveDuplicatesAsync(Guid playlistId);
    }
}
