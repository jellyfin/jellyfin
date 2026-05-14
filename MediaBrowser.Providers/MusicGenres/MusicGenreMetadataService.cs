using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.IO;
using MediaBrowser.Providers.Manager;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MediaBrowser.Providers.MusicGenres;

/// <summary>
/// Service to manage music genre metadata.
/// </summary>
public class MusicGenreMetadataService : MetadataService<MusicGenre, ItemLookupInfo>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MusicGenreMetadataService"/> class.
    /// </summary>
    /// <param name="metadataConfig">Instance of the <see cref="IOptions{MetadataConfiguration}"/>.</param>
    /// <param name="logger">Instance of the <see cref="ILogger"/> interface.</param>
    /// <param name="providerManager">Instance of the <see cref="IProviderManager"/> interface.</param>
    /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="externalDataManager">Instance of the <see cref="IExternalDataManager"/> interface.</param>
    /// <param name="itemRepository">Instance of the <see cref="IItemRepository"/> interface.</param>
    public MusicGenreMetadataService(
        IOptions<MetadataConfiguration> metadataConfig,
        ILogger<MusicGenreMetadataService> logger,
        IProviderManager providerManager,
        IFileSystem fileSystem,
        ILibraryManager libraryManager,
        IExternalDataManager externalDataManager,
        IItemRepository itemRepository)
        : base(metadataConfig, logger, providerManager, fileSystem, libraryManager, externalDataManager, itemRepository)
    {
    }
}
