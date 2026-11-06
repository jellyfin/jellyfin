using System;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Providers.Manager;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Providers.Music;

/// <summary>
/// Service to manage artist metadata.
/// </summary>
public class ArtistMetadataService : MetadataService<MusicArtist, ArtistInfo>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ArtistMetadataService"/> class.
    /// </summary>
    /// <param name="serverConfigurationManager">Instance of the <see cref="IServerConfigurationManager"/>.</param>
    /// <param name="logger">Instance of the <see cref="ILogger"/> interface.</param>
    /// <param name="providerManager">Instance of the <see cref="IProviderManager"/> interface.</param>
    /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="externalDataManager">Instance of the <see cref="IExternalDataManager"/> interface.</param>
    /// <param name="itemRepository">Instance of the <see cref="IItemRepository"/> interface.</param>
    public ArtistMetadataService(
        IServerConfigurationManager serverConfigurationManager,
        ILogger<ArtistMetadataService> logger,
        IProviderManager providerManager,
        IFileSystem fileSystem,
        ILibraryManager libraryManager,
        IExternalDataManager externalDataManager,
        IItemRepository itemRepository)
        : base(serverConfigurationManager, logger, providerManager, fileSystem, libraryManager, externalDataManager, itemRepository)
    {
    }

    /// <inheritdoc />
    protected override bool EnableUpdatingGenresFromChildren => true;

    /// <inheritdoc />
    protected override IReadOnlyList<BaseItem> GetChildrenForMetadataUpdates(MusicArtist item)
    {
        return item.IsAccessedByName
            ? item.GetTaggedItems(new InternalItemsQuery
            {
                Recursive = true,
                IsFolder = false
            })
            : item.GetRecursiveChildren(i => i is IHasArtist && !i.IsFolder);
    }

    /// <inheritdoc />
    protected override ItemUpdateType UpdateMetadataFromChildren(MusicArtist item, IReadOnlyList<BaseItem> children, bool isFullRefresh, ItemUpdateType currentUpdateType)
    {
        var updateType = base.UpdateMetadataFromChildren(item, children, isFullRefresh, currentUpdateType);

        // don't update user-changeable metadata for locked items
        if (item.IsLocked || item.LockedFields.Contains(MetadataField.Name))
        {
            return updateType;
        }

        if (isFullRefresh || currentUpdateType > ItemUpdateType.None)
        {
            updateType |= SetSortNameFromSongs(item, children);
        }

        return updateType;
    }

    private static ItemUpdateType SetSortNameFromSongs(MusicArtist item, IReadOnlyList<BaseItem> children)
    {
        // Collect the sort tag (e.g. ID3 TSO2 / albumartistsort, TSOP / artistsort) the songs associate with this
        // artist, matching the artist name against the song's primary album artist first, then its primary artist.
        var sortNames = new List<string>();
        foreach (var child in children)
        {
            if (child is not Audio song)
            {
                continue;
            }

            if (!string.IsNullOrEmpty(song.SortAlbumArtist)
                && song.AlbumArtists.Count > 0
                && string.Equals(song.AlbumArtists[0], item.Name, StringComparison.OrdinalIgnoreCase))
            {
                sortNames.Add(song.SortAlbumArtist);
            }
            else if (!string.IsNullOrEmpty(song.SortArtist)
                && song.Artists.Count > 0
                && string.Equals(song.Artists[0], item.Name, StringComparison.OrdinalIgnoreCase))
            {
                sortNames.Add(song.SortArtist);
            }
        }

        var sortName = sortNames
            .GroupBy(i => i, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .FirstOrDefault();

        if (!string.IsNullOrEmpty(sortName)
            && !string.Equals(item.ForcedSortName, sortName, StringComparison.Ordinal))
        {
            item.ForcedSortName = sortName;
            return ItemUpdateType.MetadataEdit;
        }

        return ItemUpdateType.None;
    }
}
