using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.ScheduledTasks.Tasks;

/// <summary>
/// Deletes Path references from collections that no longer exists.
/// </summary>
public class CleanupCollectionPathsTask : IScheduledTask
{
    private readonly ILocalizationManager _localization;
    private readonly ICollectionManager _collectionManager;
    private readonly ILogger<CleanupCollectionPathsTask> _logger;
    private readonly IProviderManager _providerManager;
    private readonly IFileSystem _fileSystem;

    /// <summary>
    /// Initializes a new instance of the <see cref="CleanupCollectionPathsTask"/> class.
    /// </summary>
    /// <param name="localization">Instance of the <see cref="ILocalizationManager"/> interface.</param>
    /// <param name="collectionManager">Instance of the <see cref="ICollectionManager"/> interface.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="providerManager">The provider manager.</param>
    /// <param name="fileSystem">The filesystem.</param>
    public CleanupCollectionPathsTask(
        ILocalizationManager localization,
        ICollectionManager collectionManager,
        ILogger<CleanupCollectionPathsTask> logger,
        IProviderManager providerManager,
        IFileSystem fileSystem)
    {
        _localization = localization;
        _collectionManager = collectionManager;
        _logger = logger;
        _providerManager = providerManager;
        _fileSystem = fileSystem;
    }

    /// <inheritdoc />
    public string Name => _localization.GetLocalizedString("TaskCleanCollections");

    /// <inheritdoc />
    public string Key => "CleanCollections";

    /// <inheritdoc />
    public string Description => _localization.GetLocalizedString("TaskCleanCollectionsDescription");

    /// <inheritdoc />
    public string Category => _localization.GetLocalizedString("TasksMaintenanceCategory");

    /// <inheritdoc />
    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        var collectionsFolder = await _collectionManager.GetCollectionsFolder(false).ConfigureAwait(false);
        if (collectionsFolder is null)
        {
            _logger.LogDebug("There is no collection folder to be found");
            return;
        }

        var collections = collectionsFolder.Children.OfType<BoxSet>().ToArray();
        _logger.LogDebug("Found {CollectionLength} Boxsets", collections.Length);

        for (var index = 0; index < collections.Length; index++)
        {
            var collection = collections[index];
            _logger.LogDebug("Check Boxset {CollectionName}", collection.Name);
            var itemsToRemove = new List<LinkedChild>();
            foreach (var collectionLinkedChild in collection.LinkedChildren)
            {
                if (!File.Exists(collectionLinkedChild.Path))
                {
                    _logger.LogInformation("Item in boxset {CollectionName} cannot be found at {ItemPath}", collection.Name, collectionLinkedChild.Path);
                    itemsToRemove.Add(collectionLinkedChild);
                }
            }

            if (itemsToRemove.Count != 0)
            {
                _logger.LogDebug("Update Boxset {CollectionName}", collection.Name);
                collection.LinkedChildren = collection.LinkedChildren.Except(itemsToRemove).ToArray();
                await collection.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, cancellationToken)
                    .ConfigureAwait(false);

                _providerManager.QueueRefresh(
                    collection.Id,
                    new MetadataRefreshOptions(new DirectoryService(_fileSystem))
                    {
                        ForceSave = true
                    },
                    RefreshPriority.High);
            }

            progress.Report(100D / collections.Length * (index + 1));
        }
    }

    /// <inheritdoc />
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        return new[] { new TaskTriggerInfo() { Type = TaskTriggerInfo.TriggerStartup } };
    }
}
