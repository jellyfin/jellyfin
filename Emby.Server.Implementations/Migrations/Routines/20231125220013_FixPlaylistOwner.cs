using System;
using System.Linq;
using System.Threading;

using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Playlists;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.Migrations.Routines;

/// <summary>
/// Properly set playlist owner.
/// </summary>
public partial class FixPlaylistOwner : IPostStartupMigrationRoutine
{
    private readonly ILogger<RemoveDuplicateExtras> _logger;
    private readonly ILibraryManager _libraryManager;
    private readonly IPlaylistManager _playlistManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="FixPlaylistOwner"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="libraryManager">The library manager.</param>
    /// <param name="playlistManager">The playlist manager.</param>
    public FixPlaylistOwner(
        ILogger<RemoveDuplicateExtras> logger,
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

    /// <inheritdoc />
    public string Timestamp => "20231125220013";

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
                if (shares.Length > 0)
                {
                    var firstEditShare = shares.First(x => x.CanEdit);
                    if (firstEditShare is not null && Guid.TryParse(firstEditShare.UserId, out var guid))
                    {
                        playlist.OwnerUserId = guid;
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
