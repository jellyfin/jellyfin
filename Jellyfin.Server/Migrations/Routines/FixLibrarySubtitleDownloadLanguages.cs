using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Server.ServerSetupApp;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Migrations.Routines;

/// <summary>
/// Migration to fix broken library subtitle download languages.
/// </summary>
[JellyfinMigration("2026-02-06T20:00:00", nameof(FixLibrarySubtitleDownloadLanguages))]
internal class FixLibrarySubtitleDownloadLanguages : IAsyncMigrationRoutine
{
    private readonly ILocalizationManager _localizationManager;
    private readonly ILibraryManager _libraryManager;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FixLibrarySubtitleDownloadLanguages"/> class.
    /// </summary>
    /// <param name="localizationManager">The Localization manager.</param>
    /// <param name="startupLogger">The startup logger for Startup UI integration.</param>
    /// <param name="libraryManager">The Library manager.</param>
    /// <param name="logger">The logger.</param>
    public FixLibrarySubtitleDownloadLanguages(
        ILocalizationManager localizationManager,
        IStartupLogger<FixLibrarySubtitleDownloadLanguages> startupLogger,
        ILibraryManager libraryManager,
        ILogger<FixLibrarySubtitleDownloadLanguages> logger)
    {
        _localizationManager = localizationManager;
        _libraryManager = libraryManager;
        _logger = startupLogger.With(logger);
    }

    /// <inheritdoc />
    public Task PerformAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting to fix library subtitle download languages.");

        var virtualFolders = _libraryManager.GetVirtualFolders(false);

        foreach (var virtualFolder in virtualFolders)
        {
            var options = virtualFolder.LibraryOptions;
            if (options.SubtitleDownloadLanguages is null || options.SubtitleDownloadLanguages.Length == 0)
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

            var fixedLanguages = new List<string>();

            foreach (var language in options.SubtitleDownloadLanguages)
            {
                var foundLanguage = _localizationManager.FindLanguageInfo(language)?.ThreeLetterISOLanguageName;
                if (foundLanguage is not null)
                {
                    // Converted ISO 639-2/B to T (ger to deu)
                    if (!string.Equals(foundLanguage, language, StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogInformation("Converted '{Language}' to '{ResolvedLanguage}' in library '{LibraryName}'.", language, foundLanguage, virtualFolder.Name);
                    }

                    if (fixedLanguages.Contains(foundLanguage, StringComparer.OrdinalIgnoreCase))
                    {
                        _logger.LogInformation("Language '{Language}' already exists for library '{LibraryName}'. Skipping duplicate.", foundLanguage, virtualFolder.Name);
                        continue;
                    }

                    fixedLanguages.Add(foundLanguage);
                }
                else
                {
                    _logger.LogInformation("Could not resolve language '{Language}' in library '{LibraryName}'. Skipping.", language, virtualFolder.Name);
                }
            }

            options.SubtitleDownloadLanguages = [.. fixedLanguages];
            collectionFolder.UpdateLibraryOptions(options);
        }

        _logger.LogInformation("Library subtitle download languages fixed.");

        return Task.CompletedTask;
    }
}
