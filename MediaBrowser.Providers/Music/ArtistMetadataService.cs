using System.Collections.Generic;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
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
}
