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
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.ScheduledTasks.Tasks;

/// <summary>
/// Deletes path references from collections and playlists that no longer exists.
/// </summary>
public class CleanupCollectionAndPlaylistPathsTask : IScheduledTask
{
    private readonly ILocalizationManager _localization;
    private readonly ICollectionManager _collectionManager;
    private readonly IPlaylistManager _playlistManager;
    private readonly ILogger<CleanupCollectionAndPlaylistPathsTask> _logger;
    private readonly IProviderManager _providerManager;
    private readonly IFileSystem _fileSystem;

    /// <summary>
    /// Initializes a new instance of the <see cref="CleanupCollectionAndPlaylistPathsTask"/> class.
    /// </summary>
    /// <param name="localization">Instance of the <see cref="ILocalizationManager"/> interface.</param>
    /// <param name="collectionManager">Instance of the <see cref="ICollectionManager"/> interface.</param>
    /// <param name="playlistManager">Instance of the <see cref="IPlaylistManager"/> interface.</param>
    /// <param name="logger">Instance of the <see cref="ILogger"/> interface.</param>
    /// <param name="providerManager">Instance of the <see cref="IProviderManager"/> interface.</param>
    /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
    public CleanupCollectionAndPlaylistPathsTask(
        ILocalizationManager localization,
        ICollectionManager collectionManager,
        IPlaylistManager playlistManager,
        ILogger<CleanupCollectionAndPlaylistPathsTask> logger,
        IProviderManager providerManager,
        IFileSystem fileSystem)
    {
        _localization = localization;
        _collectionManager = collectionManager;
        _playlistManager = playlistManager;
        _logger = logger;
        _providerManager = providerManager;
        _fileSystem = fileSystem;
    }

    /// <inheritdoc />
    public string Name => _localization.GetLocalizedString("TaskCleanCollectionsAndPlaylists");

    /// <inheritdoc />
    public string Key => "CleanCollectionsAndPlaylists";

    /// <inheritdoc />
    public string Description => _localization.GetLocalizedString("TaskCleanCollectionsAndPlaylistsDescription");

    /// <inheritdoc />
    public string Category => _localization.GetLocalizedString("TasksMaintenanceCategory");

    /// <inheritdoc />
    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        var collectionsFolder = await _collectionManager.GetCollectionsFolder(false).ConfigureAwait(false);
        if (collectionsFolder is null)
        {
            _logger.LogDebug("There is no collections folder to be found");
        }
        else
        {
            var collections = collectionsFolder.Children.OfType<BoxSet>().ToArray();
            _logger.LogDebug("Found {CollectionLength} boxsets", collections.Length);

            for (var index = 0; index < collections.Length; index++)
            {
                var collection = collections[index];
                _logger.LogDebug("Checking boxset {CollectionName}", collection.Name);

                CleanupLinkedChildren(collection, cancellationToken);
                progress.Report(50D / collections.Length * (index + 1));
            }
        }

        var playlistsFolder = _playlistManager.GetPlaylistsFolder();
        if (playlistsFolder is null)
        {
            _logger.LogDebug("There is no playlists folder to be found");
            return;
        }

        var playlists = playlistsFolder.Children.OfType<Playlist>().ToArray();
        _logger.LogDebug("Found {PlaylistLength} playlists", playlists.Length);

        for (var index = 0; index < playlists.Length; index++)
        {
            var playlist = playlists[index];
            _logger.LogDebug("Checking playlist {PlaylistName}", playlist.Name);

            CleanupLinkedChildren(playlist, cancellationToken);
            progress.Report(50D / playlists.Length * (index + 1));
        }
    }

    private void CleanupLinkedChildren<T>(T folder, CancellationToken cancellationToken)
        where T : Folder
    {
        List<LinkedChild>? itemsToRemove = null;
        foreach (var linkedChild in folder.LinkedChildren)
        {
            var path = linkedChild.Path;
            if (!File.Exists(path) && !Directory.Exists(path))
            {
                _logger.LogInformation("Item in {FolderName} cannot be found at {ItemPath}", folder.Name, path);
                (itemsToRemove ??= new List<LinkedChild>()).Add(linkedChild);
            }
        }

        if (itemsToRemove is not null)
        {
            _logger.LogDebug("Updating {FolderName}", folder.Name);
            folder.LinkedChildren = folder.LinkedChildren.Except(itemsToRemove).ToArray();
            _providerManager.SaveMetadataAsync(folder, ItemUpdateType.MetadataEdit);
            folder.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, cancellationToken);
        }
    }

    /// <inheritdoc />
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        return [new TaskTriggerInfo() { Type = TaskTriggerInfoType.StartupTrigger }];
    }
}
