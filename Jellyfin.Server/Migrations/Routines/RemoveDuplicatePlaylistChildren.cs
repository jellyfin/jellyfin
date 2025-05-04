using System;
using System.Linq;
using System.Threading;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Playlists;

namespace Jellyfin.Server.Migrations.Routines;

/// <summary>
/// Remove duplicate playlist entries.
/// </summary>
[JellyfinMigration("2025-04-20T19:00:00", nameof(RemoveDuplicatePlaylistChildren), "96C156A2-7A13-4B3B-A8B8-FB80C94D20C0")]
#pragma warning disable CS0618 // Type or member is obsolete
internal class RemoveDuplicatePlaylistChildren : IMigrationRoutine
#pragma warning restore CS0618 // Type or member is obsolete
{
    private readonly ILibraryManager _libraryManager;
    private readonly IPlaylistManager _playlistManager;

    public RemoveDuplicatePlaylistChildren(
        ILibraryManager libraryManager,
        IPlaylistManager playlistManager)
    {
        _libraryManager = libraryManager;
        _playlistManager = playlistManager;
    }

    /// <inheritdoc/>
    public void Perform()
    {
        var playlists = _libraryManager.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = [BaseItemKind.Playlist]
        })
        .Cast<Playlist>()
        .Where(p => !p.OpenAccess || !p.OwnerUserId.Equals(Guid.Empty))
        .ToArray();

        if (playlists.Length > 0)
        {
            foreach (var playlist in playlists)
            {
                var linkedChildren = playlist.LinkedChildren;
                if (linkedChildren.Length > 0)
                {
                    var nullItemChildren = linkedChildren.Where(c => c.ItemId is null);
                    var deduplicatedChildren = linkedChildren.DistinctBy(c => c.ItemId);
                    var newLinkedChildren = nullItemChildren.Concat(deduplicatedChildren);
                    playlist.LinkedChildren = linkedChildren;
                    playlist.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, CancellationToken.None).GetAwaiter().GetResult();
                    _playlistManager.SavePlaylistFile(playlist);
                }
            }
        }
    }
}
