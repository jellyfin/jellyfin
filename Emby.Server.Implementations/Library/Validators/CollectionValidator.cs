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
using Microsoft.Extensions.Logging;
using Jellyfin.Data.Enums;

namespace Emby.Server.Implementations.Library.Validators
{
    /// <summary>
    /// Class CollectionValidator.
    /// </summary>
    public class CollectionValidator
    {
        /// <summary>
        /// The library manager.
        /// </summary>
        private readonly ILibraryManager _libraryManager;

        /// <summary>
        /// The collection manager.
        /// </summary>
        private readonly ICollectionManager _collectionManager;

        /// <summary>
        /// The logger.
        /// </summary>
        private readonly ILogger<CollectionValidator> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="CollectionValidator" /> class.
        /// </summary>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="collectionManager">The collection manager.</param>
        /// <param name="logger">The logger.</param>
        public CollectionValidator(ILibraryManager libraryManager, ICollectionManager collectionManager, ILogger<CollectionValidator> logger)
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
            }).Select(m => m as Movie).ToList();

            var boxSets = _libraryManager.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = new[] { nameof(BoxSet) },
                CollapseBoxSetItems = false,
                Recursive = true
            }).Select(b => b as BoxSet).ToList();

            var numComplete = 0;
            var count = movies.Count;

            var map = new Dictionary<string, List<Movie>>();
            foreach (var movie in movies)
            {
                if (movie != null && movie.CollectionName != null)
                {
                    var movieList = new List<Movie>();
                    if (map.TryGetValue(movie.CollectionName, out movieList))
                    {
                        if (!movieList.Where(m => m.Id == movie.Id).Any())
                        {
                            movieList.Add(movie);
                            map[movie.CollectionName] = movieList;
                        }
                    }
                    else
                    {
                        map[movie.CollectionName] = new List<Movie> { movie };
                    }

                }

                numComplete++;
                double percent = numComplete;
                percent /= count * 2;
                percent *= 100;

                progress.Report(percent);
            }

            foreach (var pair in map)
            {
                try
                {
                    var collectionName = pair.Key;
                    var movieList = pair.Value;

                    var boxSet = boxSets.FirstOrDefault(b => b != null ? b.Name == collectionName : false);
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

                            AddMovieToCollection(boxSet.Id, boxSet, movieList);
                        }
                    }
                    else
                    {
                        AddMovieToCollection(boxSet.Id, boxSet, movieList);
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

        private async void AddMovieToCollection(Guid boxSetId, BoxSet boxSet, List<Movie> movieList)
        {

            var movieIds = new List<Guid>();
            foreach (var movie in movieList)
            {
                if (!boxSet.ContainsLinkedChildByItemId(movie.Id))
                {
                    movieIds.Add(movie.Id);
                }
            }
            if (movieIds.Any()) {
                await _collectionManager.AddToCollectionAsync(boxSetId, movieIds);
            }
        }
    }
}
