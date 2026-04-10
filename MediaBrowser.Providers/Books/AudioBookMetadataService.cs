using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
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

namespace MediaBrowser.Providers.Books;

/// <summary>
/// Service to manage audiobook metadata.
/// </summary>
public class AudioBookMetadataService : MetadataService<AudioBook, BookInfo>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AudioBookMetadataService"/> class.
    /// </summary>
    /// <param name="serverConfigurationManager">Instance of the <see cref="IServerConfigurationManager"/>.</param>
    /// <param name="logger">Instance of the <see cref="ILogger"/> interface.</param>
    /// <param name="providerManager">Instance of the <see cref="IProviderManager"/> interface.</param>
    /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="externalDataManager">Instance of the <see cref="IExternalDataManager"/> interface.</param>
    /// <param name="itemRepository">Instance of the <see cref="IItemRepository"/> interface.</param>
    public AudioBookMetadataService(
        IServerConfigurationManager serverConfigurationManager,
        ILogger<AudioBookMetadataService> logger,
        IProviderManager providerManager,
        IFileSystem fileSystem,
        ILibraryManager libraryManager,
        IExternalDataManager externalDataManager,
        IItemRepository itemRepository)
        : base(serverConfigurationManager, logger, providerManager, fileSystem, libraryManager, externalDataManager, itemRepository)
    {
    }

    /// <inheritdoc />
    protected override bool EnableUpdatingPremiereDateFromChildren => true;

    /// <inheritdoc />
    protected override bool EnableUpdatingGenresFromChildren => true;

    /// <inheritdoc />
    protected override bool EnableUpdatingStudiosFromChildren => true;

    /// <inheritdoc />
    protected override IReadOnlyList<BaseItem> GetChildrenForMetadataUpdates(AudioBook item)
        => item.GetRecursiveChildren(i => i is Audio);

    /// <inheritdoc />
    protected override ItemUpdateType UpdateMetadataFromChildren(AudioBook item, IReadOnlyList<BaseItem> children, bool isFullRefresh, ItemUpdateType currentUpdateType)
    {
        var updateType = base.UpdateMetadataFromChildren(item, children, isFullRefresh, currentUpdateType);

        if (item.IsLocked)
        {
            return updateType;
        }

        if (isFullRefresh || currentUpdateType > ItemUpdateType.None)
        {
            // Propagate Overview from first child track
            if (!item.LockedFields.Contains(MetadataField.Overview))
            {
                var firstTrack = children.FirstOrDefault(c => !string.IsNullOrEmpty(c.Overview));
                if (firstTrack is not null && !string.Equals(item.Overview, firstTrack.Overview, StringComparison.Ordinal))
                {
                    item.Overview = firstTrack.Overview;
                    updateType |= ItemUpdateType.MetadataEdit;
                }
            }
        }

        return updateType;
    }

    /// <inheritdoc />
    protected override Task AfterMetadataRefresh(AudioBook item, MetadataRefreshOptions refreshOptions, CancellationToken cancellationToken)
    {
        base.AfterMetadataRefresh(item, refreshOptions, cancellationToken);

        SetPeopleFromChildren(item);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override void MergeData(
        MetadataResult<AudioBook> source,
        MetadataResult<AudioBook> target,
        MetadataField[] lockedFields,
        bool replaceData,
        bool mergeMetadataSettings)
    {
        base.MergeData(source, target, lockedFields, replaceData, mergeMetadataSettings);
    }

    private void SetPeopleFromChildren(AudioBook item)
    {
        if (item.IsLocked)
        {
            return;
        }

        var children = item.GetRecursiveChildren(i => i is Audio);
        var childPeople = new List<PersonInfo>();
        foreach (var child in children)
        {
            foreach (var person in LibraryManager.GetPeople(child))
            {
                if (!childPeople.Exists(p => string.Equals(p.Name, person.Name, StringComparison.OrdinalIgnoreCase) && p.Type == person.Type))
                {
                    childPeople.Add(person);
                }
            }
        }

        if (childPeople.Count == 0)
        {
            return;
        }

        var existingPeople = LibraryManager.GetPeople(item);
        if (existingPeople.Count == childPeople.Count
            && existingPeople.All(e => childPeople.Exists(c => string.Equals(c.Name, e.Name, StringComparison.OrdinalIgnoreCase) && c.Type == e.Type)))
        {
            return;
        }

        LibraryManager.UpdatePeople(item, childPeople);
    }
}
