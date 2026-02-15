using System;
using System.Linq;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
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
/// Service to manage music video metadata.
/// </summary>
public class MusicVideoMetadataService : MetadataService<MusicVideo, MusicVideoInfo>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MusicVideoMetadataService"/> class.
    /// </summary>
    /// <param name="serverConfigurationManager">Instance of the <see cref="IServerConfigurationManager"/>.</param>
    /// <param name="logger">Instance of the <see cref="ILogger"/> interface.</param>
    /// <param name="providerManager">Instance of the <see cref="IProviderManager"/> interface.</param>
    /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="externalDataManager">Instance of the <see cref="IExternalDataManager"/> interface.</param>
    /// <param name="itemRepository">Instance of the <see cref="IItemRepository"/> interface.</param>
    public MusicVideoMetadataService(
        IServerConfigurationManager serverConfigurationManager,
        ILogger<MusicVideoMetadataService> logger,
        IProviderManager providerManager,
        IFileSystem fileSystem,
        ILibraryManager libraryManager,
        IExternalDataManager externalDataManager,
        IItemRepository itemRepository)
        : base(serverConfigurationManager, logger, providerManager, fileSystem, libraryManager, externalDataManager, itemRepository)
    {
    }

    /// <inheritdoc />
    protected override void MergeData(
        MetadataResult<MusicVideo> source,
        MetadataResult<MusicVideo> target,
        MetadataField[] lockedFields,
        bool replaceData,
        bool mergeMetadataSettings)
    {
        base.MergeData(source, target, lockedFields, replaceData, mergeMetadataSettings);

        var sourceItem = source.Item;
        var targetItem = target.Item;

        if (replaceData || string.IsNullOrEmpty(targetItem.Album))
        {
            targetItem.Album = sourceItem.Album;
        }

        if (replaceData || targetItem.Artists.Count == 0)
        {
            targetItem.Artists = sourceItem.Artists;
        }
        else
        {
            targetItem.Artists = targetItem.Artists.Concat(sourceItem.Artists).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        }
    }
}
