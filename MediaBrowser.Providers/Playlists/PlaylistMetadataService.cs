using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Providers.Manager;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Providers.Playlists;

/// <summary>
/// Service to manage playlist metadata.
/// </summary>
public class PlaylistMetadataService : MetadataService<Playlist, ItemLookupInfo>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PlaylistMetadataService"/> class.
    /// </summary>
    /// <param name="serverConfigurationManager">Instance of the <see cref="IServerConfigurationManager"/>.</param>
    /// <param name="logger">Instance of the <see cref="ILogger"/> interface.</param>
    /// <param name="providerManager">Instance of the <see cref="IProviderManager"/> interface.</param>
    /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="externalDataManager">Instance of the <see cref="IExternalDataManager"/> interface.</param>
    /// <param name="itemRepository">Instance of the <see cref="IItemRepository"/> interface.</param>
    public PlaylistMetadataService(
        IServerConfigurationManager serverConfigurationManager,
        ILogger<PlaylistMetadataService> logger,
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
    protected override bool EnableUpdatingOfficialRatingFromChildren => true;

    /// <inheritdoc />
    protected override bool EnableUpdatingStudiosFromChildren => true;

    /// <inheritdoc />
    protected override IReadOnlyList<BaseItem> GetChildrenForMetadataUpdates(Playlist item)
        => item.GetLinkedChildren();

    /// <inheritdoc />
    protected override void MergeData(MetadataResult<Playlist> source, MetadataResult<Playlist> target, MetadataField[] lockedFields, bool replaceData, bool mergeMetadataSettings)
    {
        base.MergeData(source, target, lockedFields, replaceData, mergeMetadataSettings);

        var sourceItem = source.Item;
        var targetItem = target.Item;

        if (mergeMetadataSettings)
        {
            targetItem.PlaylistMediaType = sourceItem.PlaylistMediaType;

            if (replaceData || targetItem.LinkedChildren.Length == 0)
            {
                targetItem.LinkedChildren = sourceItem.LinkedChildren;
            }
            else
            {
                targetItem.LinkedChildren = sourceItem.LinkedChildren.Concat(targetItem.LinkedChildren).DistinctBy(i => i.Path).ToArray();
            }

            if (replaceData || targetItem.Shares.Count == 0)
            {
                targetItem.Shares = sourceItem.Shares;
            }
            else
            {
                targetItem.Shares = sourceItem.Shares.Concat(targetItem.Shares).DistinctBy(s => s.UserId).ToArray();
            }
        }
    }
}
