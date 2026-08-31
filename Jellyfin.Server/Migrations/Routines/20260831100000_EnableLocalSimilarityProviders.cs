using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Server.Migrations.Stages;
using Jellyfin.Server.ServerSetupApp;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Migrations.Routines;

/// <summary>
/// Enables the local similarity providers on libraries that predate the similar items settings.
/// </summary>
[JellyfinMigration("2026-08-31T10:00:00", nameof(EnableLocalSimilarityProviders), Stage = JellyfinMigrationStageTypes.AppInitialisation)]
internal class EnableLocalSimilarityProviders : IAsyncMigrationRoutine
{
    private readonly ILibraryManager _libraryManager;
    private readonly IProviderManager _providerManager;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EnableLocalSimilarityProviders"/> class.
    /// </summary>
    /// <param name="libraryManager">The library manager.</param>
    /// <param name="providerManager">The provider manager.</param>
    /// <param name="startupLogger">The startup logger for Startup UI integration.</param>
    /// <param name="logger">The logger.</param>
    public EnableLocalSimilarityProviders(
        ILibraryManager libraryManager,
        IProviderManager providerManager,
        IStartupLogger<EnableLocalSimilarityProviders> startupLogger,
        ILogger<EnableLocalSimilarityProviders> logger)
    {
        _libraryManager = libraryManager;
        _providerManager = providerManager;
        _logger = startupLogger.With(logger);
    }

    /// <inheritdoc />
    public Task PerformAsync(CancellationToken cancellationToken)
    {
        // Libraries created before similar items became configurable have an empty provider list,
        // which the library editor renders as "everything unchecked" instead of falling back to the
        // defaults it uses for new libraries. Seed the local providers so they stay enabled.
        var localProvidersByType = GetLocalProvidersByItemType();
        if (localProvidersByType.Count == 0)
        {
            return Task.CompletedTask;
        }

        foreach (var virtualFolder in _libraryManager.GetVirtualFolders(false))
        {
            cancellationToken.ThrowIfCancellationRequested();

            EnableLocalProviders(virtualFolder, localProvidersByType);
        }

        return Task.CompletedTask;
    }

    private void EnableLocalProviders(VirtualFolderInfo virtualFolder, Dictionary<string, string[]> localProvidersByType)
    {
        var options = virtualFolder.LibraryOptions;
        if (options?.TypeOptions is null || options.TypeOptions.Length == 0)
        {
            return;
        }

        // Some virtual folders don't have a proper item id.
        if (!Guid.TryParse(virtualFolder.ItemId, out var folderId))
        {
            return;
        }

        var collectionFolder = _libraryManager.GetItemById<CollectionFolder>(folderId);
        if (collectionFolder is null)
        {
            _logger.LogWarning("Could not find collection folder for virtual folder '{LibraryName}' with id '{FolderId}'. Skipping.", virtualFolder.Name, folderId);
            return;
        }

        var changed = false;
        foreach (var typeOptions in options.TypeOptions)
        {
            changed |= EnableLocalProviders(typeOptions, localProvidersByType, virtualFolder.Name);
        }

        if (changed)
        {
            collectionFolder.UpdateLibraryOptions(options);
        }
    }

    private bool EnableLocalProviders(TypeOptions typeOptions, Dictionary<string, string[]> localProvidersByType, string libraryName)
    {
        if (typeOptions.Type is null || !localProvidersByType.TryGetValue(typeOptions.Type, out var localProviders))
        {
            return false;
        }

        var enabled = typeOptions.SimilarItemProviders ?? [];
        var missing = localProviders.Where(name => !enabled.Contains(name, StringComparer.OrdinalIgnoreCase)).ToArray();
        if (missing.Length == 0)
        {
            return false;
        }

        // Local providers rank ahead of remote ones, and the enabled list doubles as the
        // priority order when no explicit order was saved.
        typeOptions.SimilarItemProviders = [.. missing, .. enabled];
        if (typeOptions.SimilarItemProviderOrder is { Length: > 0 } order)
        {
            typeOptions.SimilarItemProviderOrder = [.. missing, .. order];
        }

        _logger.LogInformation("Enabled local similarity providers {Providers} for '{ItemType}' in library '{LibraryName}'.", missing, typeOptions.Type, libraryName);
        return true;
    }

    private Dictionary<string, string[]> GetLocalProvidersByItemType()
    {
        var result = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        foreach (var summary in _providerManager.GetAllMetadataPlugins())
        {
            var names = summary.Plugins
                .Where(p => p.Type == MetadataPluginType.LocalSimilarityProvider)
                .Select(p => p.Name)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (names.Length > 0)
            {
                result[summary.ItemType] = names;
            }
        }

        return result;
    }
}
