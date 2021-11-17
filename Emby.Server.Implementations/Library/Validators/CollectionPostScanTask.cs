using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Querying;
using Jellyfin.Data.Enums;
using Microsoft.Extensions.Logging;
using MediaBrowser.Model.Entities;

namespace Emby.Server.Implementations.Library.Validators
{
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
            var boxSets = _libraryManager.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = new[] { nameof(BoxSet) },
                CollapseBoxSetItems = false,
                Recursive = true
            });

            var collectionNameMoviesMap = new Dictionary<string, List<Movie>>();

            foreach (var library in _libraryManager.RootFolder.Children.ToList()) {
                if (!_libraryManager.GetLibraryOptions(library).AutoCollection) {
                    continue;
                }

                var movies = _libraryManager.GetItemList(new InternalItemsQuery
                {
                    MediaTypes = new string[] { MediaType.Video }, 
                    IncludeItemTypes = new[] { nameof(Movie) },
                    IsVirtualItem = false,
                    OrderBy = new[] { (ItemSortBy.SortName, SortOrder.Ascending) },
                    SourceTypes = new[] { SourceType.Library },
                    Parent = library,
                    Recursive = true
                });

                foreach (var m in movies)
                {
                    if (m is Movie movie && !string.IsNullOrEmpty(movie.CollectionName))
                    {
                        if (collectionNameMoviesMap.TryGetValue(movie.CollectionName, out var movieList))
                        {
                            if (!movieList.Any(m => m.Id == movie.Id))
                            {
                                movieList.Add(movie);
                            }
                        }
                        else
                        {
                            collectionNameMoviesMap[movie.CollectionName] = new List<Movie> { movie };
                        }
                    }
                }
            }

            var numComplete = 0;
            var count = collectionNameMoviesMap.Count;

            foreach (var (collectionName, movieList) in collectionNameMoviesMap)
            {
                try
                {
                    var boxSet = boxSets.FirstOrDefault(b => b?.Name == collectionName) as BoxSet;
                    if (boxSet == null)
                    {
                        // won't automatically create collection if only one movie in it
                        if (movieList.Count >= 2)
                        {
                            boxSet = await _collectionManager.CreateCollectionAsync(new CollectionCreationOptions
                            {
                                Name = collectionName,
                                IsLocked = true
                            });

                            await _collectionManager.AddToCollectionAsync(boxSet.Id, movieList.Select(m => m.Id));
                        }
                    }
                    else
                    {
                        await _collectionManager.AddToCollectionAsync(boxSet.Id, movieList.Select(m => m.Id));
                    }

                    numComplete++;
                    double percent = numComplete;
                    percent /= count;
                    percent *= 100;

                    progress.Report(percent);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error refreshing {CollectionName} with {@MovieIds}", collectionName, movieList);
                }
            }

            progress.Report(100);
        }
    }
}