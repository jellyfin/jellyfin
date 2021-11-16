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

namespace Emby.Server.Implementations.Library.Validators
{
    /// <summary>
    /// Class CollectionPostScanTask.
    /// </summary>
    public class CollectionPostScanTask : ILibraryPostScanTask
    {
        /// <summary>
        /// The _library manager.
        /// </summary>
        private readonly ILibraryManager _libraryManager;

        /// <summary>
        /// The collection manager.
        /// </summary>
        private readonly ICollectionManager _collectionManager;

        /// <summary>
        /// The logger.
        /// </summary>
        private readonly ILogger<CollectionPostScanTask> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="CollectionPostScanTask" /> class.
        /// </summary>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="collectionManager">The collection manager.</param>
        /// <param name="logger">The logger.</param>
        public CollectionPostScanTask(
            ILibraryManager libraryManager,
            ILogger<CollectionPostScanTask> logger,
            ICollectionManager collectionManager)
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
                OrderBy = new List<ValueTuple<string, SortOrder>>
                {
                new ValueTuple<string, SortOrder>(ItemSortBy.SortName, SortOrder.Ascending)
                },
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
                var movie = m as Movie;
                if (movie != null && movie.CollectionName != null)
                {
                    var movieList = new List<Movie>();
                    if (collectionNameMoviesMap.TryGetValue(movie.CollectionName, out movieList))
                    {
                        if (!movieList.Any(m => m.Id == movie.Id))
                        {
                            movieList.Add(movie);
                            collectionNameMoviesMap[movie.CollectionName] = movieList;
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

            foreach (var pair in collectionNameMoviesMap)
            {
                try
                {
                    var collectionName = pair.Key;
                    var movieList = pair.Value;

                    var boxSet = boxSets.FirstOrDefault(b => b != null ? b.Name == collectionName : false) as BoxSet;
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
                    percent /= count * 2;
                    percent *= 100;

                    progress.Report(percent);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error refreshing {0}, {1}", pair.Key, pair.Value.ToString());
                }
            }

            progress.Report(100);
        }
    }
}
