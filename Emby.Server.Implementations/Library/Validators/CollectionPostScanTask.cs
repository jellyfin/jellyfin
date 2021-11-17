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
using System.Diagnostics;

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
            var movies = _libraryManager.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = new[] { nameof(Movie) },
                IsVirtualItem = false,
                OrderBy = new[] { (ItemSortBy.SortName, SortOrder.Ascending) },
                Recursive = true
            });

            var boxSets = _libraryManager.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = new[] { nameof(BoxSet) },
                CollapseBoxSetItems = false,
                Recursive = true
            });

            var numComplete = 0;
            var count = movies.Count;

            var collectionNameMoviesMap = new Dictionary<string, List<Movie>>();
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

                numComplete++;
                double percent = numComplete;
                percent /= count * 2;
                percent *= 100;

                progress.Report(percent);
            }

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
                            var movieIds = FliterMoviesByOption(movieList);
                            if (movieIds.Count >= 2) {
                                // at least 2 movies have AutoCollection option enable
                                boxSet = await _collectionManager.CreateCollectionAsync(new CollectionCreationOptions
                                {
                                    Name = collectionName,
                                    IsLocked = true
                                });

                                await _collectionManager.AddToCollectionAsync(boxSet.Id, movieIds);
                            }
                        }
                    }
                    else
                    {
                        var movieIds = FliterMoviesByOption(movieList);
                        await _collectionManager.AddToCollectionAsync(boxSet.Id, movieIds);
                    }

                    numComplete++;
                    double percent = numComplete;
                    percent /= count * 2;
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

        private List<Guid> FliterMoviesByOption(List<Movie> movieList) {
            List<Guid> movieIds = new List<Guid>();
            foreach (var movie in movieList)
            {
                if (_libraryManager.GetLibraryOptions(movie).AutoCollection)
                {
                    movieIds.Add(movie.Id);
                }
            }
            return movieIds;
        }
    }
}