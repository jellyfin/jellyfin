using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Enums;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.Library.Validators;

/// <summary>
/// Class CollectionPostScanTask.
/// </summary>
public class CollectionPostScanTask : ILibraryPostScanTask
{
    private readonly ILibraryManager _libraryManager;
    private readonly ICollectionManager _collectionManager;
    private readonly ILogger<CollectionPostScanTask> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CollectionPostScanTask" /> class.
    /// </summary>
    /// <param name="libraryManager">The library manager.</param>
    /// <param name="collectionManager">The collection manager.</param>
    /// <param name="logger">The logger.</param>
    public CollectionPostScanTask(
        ILibraryManager libraryManager,
        ICollectionManager collectionManager,
        ILogger<CollectionPostScanTask> logger)
    {
        _libraryManager = libraryManager;
        _collectionManager = collectionManager;
        _logger = logger;
    }

    /// <summary>
    /// Runs the specified progress.
    /// </summary>
    /// <param name="progress">The progress.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Task.</returns>
    public async Task Run(IProgress<double> progress, CancellationToken cancellationToken)
    {
        var collectionGroups = new Dictionary<string, CollectionGroup>();

        foreach (var library in _libraryManager.RootFolder.Children)
        {
            if (!_libraryManager.GetLibraryOptions(library).AutomaticallyAddToCollection)
            {
                continue;
            }

            var startIndex = 0;
            var pagesize = 1000;

            while (true)
            {
                var movies = _libraryManager.GetItemList(new InternalItemsQuery
                {
                    MediaTypes = [MediaType.Video],
                    IncludeItemTypes = [BaseItemKind.Movie],
                    IsVirtualItem = false,
                    OrderBy = [(ItemSortBy.SortName, SortOrder.Ascending)],
                    Parent = library,
                    StartIndex = startIndex,
                    Limit = pagesize,
                    Recursive = true
                });

                foreach (var m in movies)
                {
                    if (m is not Movie movie
                        || string.IsNullOrEmpty(movie.CollectionName)
                        || movie.PrimaryVersionId.HasValue)
                    {
                        continue;
                    }

                    var tmdbCollectionId = movie.TryGetProviderId(MetadataProvider.TmdbCollection, out var id) ? id : null;

                    var key = string.IsNullOrEmpty(tmdbCollectionId)
                        ? "name=" + movie.CollectionName
                        : "id=" + tmdbCollectionId;

                    if (!collectionGroups.TryGetValue(key, out var group))
                    {
                        group = new CollectionGroup(movie.CollectionName, tmdbCollectionId);
                        collectionGroups[key] = group;
                    }

                    group.MovieIds.Add(movie.Id);
                }

                if (movies.Count < pagesize)
                {
                    break;
                }

                startIndex += pagesize;
            }
        }

        var numComplete = 0;
        var count = collectionGroups.Count;

        if (count == 0)
        {
            progress.Report(100);
            return;
        }

        var boxSets = _libraryManager.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = [BaseItemKind.BoxSet],
            CollapseBoxSetItems = false,
            Recursive = true
        });

        foreach (var group in collectionGroups.Values)
        {
            try
            {
                BoxSet? boxSet = null;

                // Prefer the stable TMDB collection id. The box set stores it under the Tmdb
                // provider key, while movies store it under TmdbCollection
                if (!string.IsNullOrEmpty(group.TmdbCollectionId))
                {
                    boxSet = boxSets
                        .OfType<BoxSet>()
                        .FirstOrDefault(b =>
                            b.TryGetProviderId(MetadataProvider.Tmdb, out var id)
                            && string.Equals(id, group.TmdbCollectionId, StringComparison.Ordinal));
                }

                // Fall back to name for legacy box sets that don't have an id yet.
                boxSet ??= boxSets.FirstOrDefault(b => b?.Name == group.Name) as BoxSet;

                if (boxSet is null)
                {
                    // won't automatically create collection if only one movie in it
                    if (group.MovieIds.Count >= 2)
                    {
                        var options = new CollectionCreationOptions
                        {
                            Name = group.Name,
                        };

                        // Stamp the stable collection id so future scans can match this box set by id
                        // even after the user renames it
                        if (!string.IsNullOrEmpty(group.TmdbCollectionId))
                        {
                            options.SetProviderId(MetadataProvider.Tmdb, group.TmdbCollectionId);
                        }

                        boxSet = await _collectionManager.CreateCollectionAsync(options).ConfigureAwait(false);

                        await _collectionManager.AddToCollectionAsync(boxSet.Id, group.MovieIds).ConfigureAwait(false);
                    }
                }
                else
                {
                    await _collectionManager.AddToCollectionAsync(boxSet.Id, group.MovieIds).ConfigureAwait(false);
                }

                numComplete++;
                double percent = numComplete;
                percent /= count;
                percent *= 100;

                progress.Report(percent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing {CollectionName} with {@MovieIds}", group.Name, group.MovieIds);
            }
        }

        progress.Report(100);
    }

    private sealed class CollectionGroup
    {
        public CollectionGroup(string name, string? tmdbCollectionId)
        {
            Name = name;
            TmdbCollectionId = tmdbCollectionId;
        }

        public string Name { get; }

        public string? TmdbCollectionId { get; }

        public HashSet<Guid> MovieIds { get; } = new();
    }
}
