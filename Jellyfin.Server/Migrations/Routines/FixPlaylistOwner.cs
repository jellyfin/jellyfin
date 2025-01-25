using System;
using System.Linq;
using System.Threading;

using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Playlists;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Migrations.Routines;

/// <summary>
/// Properly set playlist owner.
/// </summary>
internal class FixPlaylistOwner : IMigrationRoutine
{
    private readonly ILogger<FixPlaylistOwner> _logger;
    private readonly ILibraryManager _libraryManager;
    private readonly IPlaylistManager _playlistManager;

    public FixPlaylistOwner(
        ILogger<FixPlaylistOwner> logger,
        ILibraryManager libraryManager,
        IPlaylistManager playlistManager)
    {
        _logger = logger;
        _libraryManager = libraryManager;
        _playlistManager = playlistManager;
    }

    /// <inheritdoc/>
    public Guid Id => Guid.Parse("{615DFA9E-2497-4DBB-A472-61938B752C5B}");

    /// <inheritdoc/>
    public string Name => "FixPlaylistOwner";

    /// <inheritdoc/>
    public bool PerformOnNewInstall => false;

    /// <inheritdoc/>
    public void Perform()
    {
        var playlists = _libraryManager.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = new[] { BaseItemKind.Playlist }
        })
        .Cast<Playlist>()
        .Where(x => x.OwnerUserId.Equals(Guid.Empty))
        .ToArray();

        if (playlists.Length > 0)
        {
            foreach (var playlist in playlists)
            {
                var shares = playlist.Shares;
                if (shares.Count > 0)
                {
                    var firstEditShare = shares.First(x => x.CanEdit);
                    if (firstEditShare is not null)
                    {
                        playlist.OwnerUserId = firstEditShare.UserId;
                        playlist.Shares = shares.Where(x => x != firstEditShare).ToArray();
                        playlist.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, CancellationToken.None).GetAwaiter().GetResult();
                        _playlistManager.SavePlaylistFile(playlist);
                    }
                }
                else
                {
                    playlist.OpenAccess = true;
                    playlist.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, CancellationToken.None).GetAwaiter().GetResult();
                }
            }
        }
    }
}
