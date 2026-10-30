using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Server.Migrations.Stages;
using Jellyfin.Server.ServerSetupApp;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Migrations.Routines;

/// <summary>
/// Turns automatic movie version grouping off for libraries that predate it.
/// </summary>
/// <remarks>
/// The option defaults to on for new libraries, but existing libraries have it unset, so they would pick up
/// the default and start merging copies of the same movie on the next scan without anyone asking for it.
/// </remarks>
[JellyfinMigration("2026-10-24T12:00:00", nameof(KeepMovieVersionGroupingOffForExistingLibraries), Stage = JellyfinMigrationStageTypes.AppInitialisation)]
internal class KeepMovieVersionGroupingOffForExistingLibraries : IAsyncMigrationRoutine
{
    private readonly ILibraryManager _libraryManager;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="KeepMovieVersionGroupingOffForExistingLibraries"/> class.
    /// </summary>
    /// <param name="libraryManager">The library manager.</param>
    /// <param name="startupLogger">The startup logger for Startup UI integration.</param>
    /// <param name="logger">The logger.</param>
    public KeepMovieVersionGroupingOffForExistingLibraries(
        ILibraryManager libraryManager,
        IStartupLogger<KeepMovieVersionGroupingOffForExistingLibraries> startupLogger,
        ILogger<KeepMovieVersionGroupingOffForExistingLibraries> logger)
    {
        _libraryManager = libraryManager;
        _logger = startupLogger.With(logger);
    }

    /// <inheritdoc />
    public Task PerformAsync(CancellationToken cancellationToken)
    {
        foreach (var virtualFolder in _libraryManager.GetVirtualFolders(false))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var options = virtualFolder.LibraryOptions;
            if (options is null || !options.EnableAutomaticMovieVersionGrouping)
            {
                continue;
            }

            // Some virtual folders don't have a proper item id.
            if (!Guid.TryParse(virtualFolder.ItemId, out var folderId))
            {
                continue;
            }

            var collectionFolder = _libraryManager.GetItemById<CollectionFolder>(folderId);
            if (collectionFolder is null)
            {
                _logger.LogWarning("Could not find collection folder for virtual folder '{LibraryName}' with id '{FolderId}'. Skipping.", virtualFolder.Name, folderId);
                continue;
            }

            options.EnableAutomaticMovieVersionGrouping = false;
            collectionFolder.UpdateLibraryOptions(options);
            _logger.LogInformation("Kept automatic movie version grouping off for existing library '{LibraryName}'.", virtualFolder.Name);
        }

        return Task.CompletedTask;
    }
}
