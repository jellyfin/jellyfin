using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Providers.Manager;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Providers.BoxSets;

/// <summary>
/// Service to manage boxset metadata.
/// </summary>
public class BoxSetMetadataService : MetadataService<BoxSet, BoxSetInfo>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BoxSetMetadataService"/> class.
    /// </summary>
    /// <param name="serverConfigurationManager">Instance of the <see cref="IServerConfigurationManager"/>.</param>
    /// <param name="logger">Instance of the <see cref="ILogger"/> interface.</param>
    /// <param name="providerManager">Instance of the <see cref="IProviderManager"/> interface.</param>
    /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="externalDataManager">Instance of the <see cref="IExternalDataManager"/> interface.</param>
    /// <param name="itemRepository">Instance of the <see cref="IItemRepository"/> interface.</param>
    public BoxSetMetadataService(
        IServerConfigurationManager serverConfigurationManager,
        ILogger<BoxSetMetadataService> logger,
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
    protected override bool EnableUpdatingPremiereDateFromChildren => true;

    /// <inheritdoc />
    protected override IReadOnlyList<BaseItem> GetChildrenForMetadataUpdates(BoxSet item)
    {
        return item.GetLinkedChildren();
    }

    /// <inheritdoc />
    protected override void MergeData(MetadataResult<BoxSet> source, MetadataResult<BoxSet> target, MetadataField[] lockedFields, bool replaceData, bool mergeMetadataSettings)
    {
        base.MergeData(source, target, lockedFields, replaceData, mergeMetadataSettings);

        var sourceItem = source.Item;
        var targetItem = target.Item;

        if (mergeMetadataSettings)
        {
            if (replaceData || targetItem.LinkedChildren.Length == 0)
            {
                targetItem.LinkedChildren = sourceItem.LinkedChildren;
            }
            else
            {
                targetItem.LinkedChildren = sourceItem.LinkedChildren.Concat(targetItem.LinkedChildren).Distinct().ToArray();
            }
        }
    }

    /// <inheritdoc />
    protected override ItemUpdateType BeforeSaveInternal(BoxSet item, bool isFullRefresh, ItemUpdateType updateType)
    {
        var updatedType = base.BeforeSaveInternal(item, isFullRefresh, updateType);

        var libraryFolderIds = item.GetLibraryFolderIds();

        var itemLibraryFolderIds = item.LibraryFolderIds;
        if (itemLibraryFolderIds is null || !libraryFolderIds.SequenceEqual(itemLibraryFolderIds))
        {
            item.LibraryFolderIds = libraryFolderIds;
            updatedType |= ItemUpdateType.MetadataImport;
        }

        return updatedType;
    }
}
