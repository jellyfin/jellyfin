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

namespace MediaBrowser.Providers.People;

/// <summary>
/// Service to manage person metadata.
/// </summary>
public class PersonMetadataService : MetadataService<Person, PersonLookupInfo>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PersonMetadataService"/> class.
    /// </summary>
    /// <param name="serverConfigurationManager">Instance of the <see cref="IServerConfigurationManager"/>.</param>
    /// <param name="logger">Instance of the <see cref="ILogger"/> interface.</param>
    /// <param name="providerManager">Instance of the <see cref="IProviderManager"/> interface.</param>
    /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="externalDataManager">Instance of the <see cref="IExternalDataManager"/> interface.</param>
    /// <param name="itemRepository">Instance of the <see cref="IItemRepository"/> interface.</param>
    public PersonMetadataService(
        IServerConfigurationManager serverConfigurationManager,
        ILogger<PersonMetadataService> logger,
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
        MetadataResult<Person> source,
        MetadataResult<Person> target,
        MetadataField[] lockedFields,
        bool replaceData,
        bool mergeMetadataSettings)
    {
        // A provider matches a person by a fuzzy name search, so the name it returns can be another
        // spelling or another person entirely.
        var name = target.Item.Name;

        base.MergeData(source, target, lockedFields, replaceData, mergeMetadataSettings);

        if (!string.IsNullOrEmpty(name))
        {
            target.Item.Name = name;
        }
    }
}
