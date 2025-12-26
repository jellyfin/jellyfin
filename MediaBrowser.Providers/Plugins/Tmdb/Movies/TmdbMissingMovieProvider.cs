using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Data.Events;
using MediaBrowser.Controller.BaseItemManager;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.IO;
using MediaBrowser.Providers.Plugins.Tmdb;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Tmdb.Movies.Providers
{
    /// <summary>
    /// Tmdb Missing Episode provider.
    /// </summary>
    public class TmdbMissingMovieProvider : IHostedService
    {
        /// <summary>
        /// The provider name.
        /// </summary>
        public static readonly string ProviderName = "Missing Movie Fetcher";

        private readonly TmdbClientManager _tmbdbClientManager;
        private readonly IBaseItemManager _baseItemManager;
        private readonly ICollectionManager _collectionManager;
        private readonly IProviderManager _providerManager;
        private readonly ILocalizationManager _localization;
        private readonly ILibraryManager _libraryManager;
        private readonly IFileSystem _fileSystem;
        private readonly ILogger<TmdbMissingMovieProvider> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="TmdbMissingMovieProvider"/> class.
        /// </summary>
        /// <param name="tmdbClientManager">Instance of the <see cref="TmdbClientManager"/> class.</param>
        /// <param name="baseItemManager">Instance of the <see cref="IBaseItemManager"/> interface.</param>
        /// <param name="collectionManager">Instance of the <see cref="ICollectionManager"/> interface.</param>
        /// <param name="providerManager">Instance of the <see cref="IProviderManager"/> interface.</param>
        /// <param name="localization">Instance of the <see cref="ILocalizationManager"/> interface.</param>
        /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
        /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
        /// <param name="logger">Instance of the <see cref="ILogger{TvdbMissingEpisodeProvider}"/> interface.</param>
        public TmdbMissingMovieProvider(
            TmdbClientManager tmdbClientManager,
            IBaseItemManager baseItemManager,
            ICollectionManager collectionManager,
            IProviderManager providerManager,
            ILocalizationManager localization,
            ILibraryManager libraryManager,
            IFileSystem fileSystem,
            ILogger<TmdbMissingMovieProvider> logger)
        {
            _tmbdbClientManager = tmdbClientManager;
            _baseItemManager = baseItemManager;
            _collectionManager = collectionManager;
            _providerManager = providerManager;
            _localization = localization;
            _libraryManager = libraryManager;
            _fileSystem = fileSystem;
            _logger = logger;
        }

        private static bool ShowMissingMovies => MediaBrowser.Providers.Plugins.Tmdb.Plugin.Instance?.Configuration.ShowMissing ?? false;

        private static Tuple<bool, bool> MovieExists(List<Movie> existingMovies, TMDbLib.Objects.Search.SearchMovie movieRecord)
        {
            // Check if the movie already exists in the box set
            var physicalMovieExists = existingMovies.Any(e => !e.IsVirtualItem && MovieEquals(e, movieRecord));
            var virtualMovieExists = existingMovies.Any(e => e.IsVirtualItem && MovieEquals(e, movieRecord));

            return Tuple.Create(physicalMovieExists, virtualMovieExists);
        }

        private static bool MovieEquals(Movie movie, TMDbLib.Objects.Search.SearchMovie otherMovieRecord)
        {
            return otherMovieRecord.Id.ToString(CultureInfo.InvariantCulture) == movie.GetProviderId(MetadataProvider.Tmdb.ToString());
        }

        private bool IsEnabledForLibrary(BaseItem item)
        {
            if (!ShowMissingMovies)
            {
                _logger.LogDebug("ShowMissingMovies is disabled, skipping {ItemName} [{ItemType}]", item.Name, item.GetType());
                return false;
            }

            if (!(item.GetType() == typeof(BoxSet) || item.GetType() == typeof(Movie)))
            {
                _logger.LogDebug("Item {ItemName} [{ItemType}] is not a BoxSet or Movie, skipping.", item.Name, item.GetType());
                return false;
            }

            return true;
        }

        // TODO use the new async events when provider manager is updated
        private void OnProviderManagerRefreshComplete(object? sender, GenericEventArgs<BaseItem> genericEventArgs)
        {
            if (!IsEnabledForLibrary(genericEventArgs.Argument))
            {
                _logger.LogDebug("{ProviderName} not enabled for {InputName}", ProviderName, genericEventArgs.Argument.Name);
                return;
            }

            _logger.LogDebug("{MethodName}: Try Refreshing for Item {Name} {Type}", nameof(OnProviderManagerRefreshComplete), genericEventArgs.Argument.Name, genericEventArgs.Argument.GetType());

            if (genericEventArgs.Argument is BoxSet boxSet)
            {
                _logger.LogDebug("{MethodName}: Refreshing BoxSet {BoxSetName}", nameof(OnProviderManagerRefreshComplete), boxSet.Name);
                HandleBoxSet(boxSet).GetAwaiter().GetResult();
            }
        }

        private async Task HandleBoxSet(BoxSet boxSet)
        {
            if (!boxSet.HasProviderId(MetadataProvider.Tmdb.ToString()))
            {
                _logger.LogWarning("BoxSet {BoxSetName} does not have a TMDB Collection Id, skipping.", boxSet.Name);
                return;
            }

            var tmdbCollectionId = boxSet.GetProviderId(MetadataProvider.Tmdb.ToString());
            _logger.LogDebug("BoxSet {BoxSetName} with TMDB Collection Id {TmdbCollectionId} was updated,", boxSet.Name, tmdbCollectionId);

            var children = boxSet.RecursiveChildren.ToList();
            var existingMovies = new List<Movie>();

            for (var i = 0; i < children.Count; i++)
            {
                var child = children[i];
                if (child is Movie movie)
                {
                    existingMovies.Add(movie);
                }
            }

            var allMovies = await GetCollectionMovies(
                int.Parse(tmdbCollectionId!, CultureInfo.InvariantCulture),
                boxSet.GetPreferredMetadataLanguage()).ConfigureAwait(false);

            foreach (var movieRecord in allMovies)
            {
                bool physicalMovieExists = MovieExists(existingMovies, movieRecord).Item1;
                bool virtualMovieExists = MovieExists(existingMovies, movieRecord).Item2;
                // Check if the movie already exists in the box set
                if (physicalMovieExists || virtualMovieExists)
                {
                    // Movie already exists, no action needed
                    _logger.LogDebug("Movie {MovieName} already exists in BoxSet {BoxSetName}", movieRecord.Title, boxSet.Name);

                    // Check if physical movie exists and if virtual movie exists
                    if (physicalMovieExists && virtualMovieExists)
                    {
                        _logger.LogDebug("Physical movie {MovieName} exists in BoxSet {BoxSetName}, removing virtual item.", movieRecord.Title, boxSet.Name);
                        DeleteVirtualItems(existingMovies.Where(e => MovieEquals(e, movieRecord) && e.IsVirtualItem).ToList());
                    }

                    continue;
                }

                // Movie does not exist, create a new virtual movie
                var virtualMovie = AddVirtualMovie(movieRecord.Title, movieRecord.Id.ToString(CultureInfo.InvariantCulture));
                if (virtualMovie is null)
                {
                    _logger.LogWarning("Failed to create virtual movie for {MovieName} in BoxSet {BoxSetName}", movieRecord.Title, boxSet.Name);
                    continue;
                }

                _logger.LogInformation("Created virtual movie {MovieName} for BoxSet {BoxSetName}", movieRecord.Title, boxSet.Name);
                // Add the virtual movie to the box set
                AddMovieToBoxSet(boxSet, virtualMovie);
            }

            // Delete orphaned movies
            var orphanedMovies = existingMovies
                .Where(e => e.IsVirtualItem)
                .Where(e => !allMovies.Any(movieRecord =>
                    MovieEquals(e, movieRecord)))
                .ToList();
            DeleteVirtualItems(orphanedMovies);
        }

        private void DeleteVirtualItems<T>(IReadOnlyList<T> existingVirtualItems)
            where T : BaseItem
        {
            var deleteOptions = new DeleteOptions
            {
                DeleteFileLocation = false
            };

            // Remove the virtual movie that matches the newly updated item
            for (var i = 0; i < existingVirtualItems.Count; i++)
            {
                var currentItem = existingVirtualItems[i];
                _logger.LogInformation("Delete VirtualItem {Name}", currentItem.Name);
                _libraryManager.DeleteItem(currentItem, deleteOptions);
            }
        }

        private void OnLibraryManagerItemUpdated(object? sender, ItemChangeEventArgs itemChangeEventArgs)
        {
            _logger.LogDebug(
                "{MethodName}: Refreshing Item {ItemName} [{Reason}]",
                nameof(OnLibraryManagerItemUpdated),
                itemChangeEventArgs.Item.Name,
                itemChangeEventArgs.UpdateReason);

            // Only interested in real movies
            if (itemChangeEventArgs.Item.IsVirtualItem
                || !(itemChangeEventArgs.Item is Movie))
            {
                _logger.LogDebug("Skip: Updated item is {ItemType}.", itemChangeEventArgs.Item.IsVirtualItem ? "Virtual" : "no Movie");
                return;
            }

            if (!IsEnabledForLibrary(itemChangeEventArgs.Item))
            {
                _logger.LogDebug("{ProviderName} not enabled for {InputName}", ProviderName, itemChangeEventArgs.Item.Name);
                return;
            }

            // Check if movie
            if (itemChangeEventArgs.Item.GetType() == typeof(Movie))
            {
                // Delete virtual item of the corresponding physical movie
                var movie = (Movie)itemChangeEventArgs.Item;

                // Step 1: Find tmdb collection id from movie
                if (!movie.TryGetProviderId(MetadataProvider.Tmdb.ToString(), out var tmdbId))
                {
                    // If the movie does not have a TMDB Id, skip it
                    {
                        _logger.LogWarning("Movie {MovieName} does not have a TMDB Collection Id, skipping.", movie.Name);
                        return;
                    }
                }

                // Step 2: Check if we have a virtual movie for the TMDB collection id
                var query = new InternalItemsQuery
                {
                    IsVirtualItem = true,
                    IncludeItemTypes = new[] { BaseItemKind.Movie },
                    GroupByPresentationUniqueKey = false,
                    Recursive = true,
                    DtoOptions = new DtoOptions(true)
                };

                var existingVirtualMovies = _libraryManager.GetItemList(query)
                    .Where(m => m.GetProviderId(MetadataProvider.Tmdb.ToString()) == tmdbId)
                    .ToList();
                if (existingVirtualMovies.Count > 0)
                {
                    _logger.LogDebug("Found {Count} virtual movies for TMDB Id {TmdbId}", existingVirtualMovies.Count, tmdbId);
                    // Step 3: Delete the virtual movie
                    DeleteVirtualItems(existingVirtualMovies);
                }
                else
                {
                    _logger.LogDebug("No virtual movies found for TMDB Id {TmdbId}", tmdbId);
                }
            }
        }

        private void OnLibraryManagerItemRemoved(object? sender, ItemChangeEventArgs itemChangeEventArgs)
        {
            _logger.LogDebug(
                "{MethodName}: Refreshing {ItemName} [{Reason}]",
                nameof(OnLibraryManagerItemRemoved),
                itemChangeEventArgs.Item.Name,
                itemChangeEventArgs.UpdateReason);

            if (!IsEnabledForLibrary(itemChangeEventArgs.Item))
            {
                _logger.LogDebug("{ProviderName} not enabled for {InputName}", ProviderName, itemChangeEventArgs.Item.Name);
                return;
            }

            // No action needed if the item is virtual
            if (itemChangeEventArgs.Item.IsVirtualItem)
            {
                _logger.LogDebug("Skip: {Message}.", itemChangeEventArgs.Item.IsVirtualItem ? "Updated item is Virtual" : "Update not enabled");
                return;
            }

            // Create a new virtual movie if the real one was deleted.
            if (itemChangeEventArgs.Item is Movie movie)
            {
                var tmdbId = string.Empty;
                if (!movie.TryGetProviderId(MetadataProvider.Tmdb.ToString(), out tmdbId))
                {
                    _logger.LogWarning("Movie {MovieName} does not have a TMDB Id, skipping.", movie.Name);
                    return;
                }

                _logger.LogInformation("Movie {MovieName} with TMDB Id {TmdbId} was removed, creating virtual movie.", movie.Name, tmdbId);

                Movie virtualMovie = AddVirtualMovie(movie.Name, tmdbId);

                if (virtualMovie is null)
                {
                    _logger.LogWarning("Failed to create virtual movie for {MovieName} with TMDB Id {TmdbId}", movie.Name, tmdbId);
                    return;
                }

                // Add the virtual movie to the box set
                if (movie.GetParent() is BoxSet boxSet)
                {
                    AddMovieToBoxSet(boxSet, virtualMovie);
                }
                else
                {
                    _logger.LogWarning("Parent of removed movie {MovieName} is not a BoxSet, skipping.", movie.Name);
                }
            }
        }

        private async Task<IReadOnlyList<TMDbLib.Objects.Search.SearchMovie>> GetCollectionMovies(int tmdbId, string acceptedLanguage)
        {
            try
            {
                // Fetch all movies
                var collectionInfo = await _tmbdbClientManager.GetCollectionAsync(
                    tmdbId,
                    acceptedLanguage,
                    TmdbUtils.GetImageLanguagesParam(acceptedLanguage),
                    null,
                    CancellationToken.None).ConfigureAwait(false);

                var allMovies = collectionInfo?.Parts;
                if (allMovies is null || allMovies.Count == 0)
                {
                    _logger.LogWarning("Unable to get movies from TMDB: Movie Query returned null for TMDB Collection Id: {TmdbId}", tmdbId);
                    return Array.Empty<TMDbLib.Objects.Search.SearchMovie>();
                }

                _logger.LogDebug("{MethodName}: For TMDB Id '{TmdbId}' found #{Count} movies", nameof(GetCollectionMovies), tmdbId, allMovies.Count);
                return allMovies;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unable to get movies from TMDB for Id '{TmdbId}'", tmdbId);
                return Array.Empty<TMDbLib.Objects.Search.SearchMovie>();
            }
        }

        private void AddMovieToBoxSet(BoxSet boxSet, Movie movie)
        {
            // Add the virtual movie to the box set
            _collectionManager.AddToCollectionAsync(boxSet.Id, new List<Guid> { movie.Id }).GetAwaiter().GetResult();
            _logger.LogDebug("Added virtual movie {MovieName} to BoxSet {BoxSetName}", movie.Name, boxSet.Name);
        }

        private Movie AddVirtualMovie(string name, string tmdbCollectionId)
        {
            // Create a new virtual movie
            var virtualMovie = new Movie
            {
                Id = _libraryManager.GetNewItemId(
                    name,
                    typeof(Movie)),
                IsVirtualItem = true,
                Name = name,
                ProviderIds = new Dictionary<string, string> { { MetadataProvider.Tmdb.ToString(), tmdbCollectionId } }
            };

            // Create a new movie with the same name and TMDb collection ID
            _libraryManager.CreateItem(virtualMovie, null);
            _logger.LogInformation(
                "Virtual movie created with ID {Id} and name {Name}",
                virtualMovie.Id,
                virtualMovie.Name);

            // Trigger a metadata refresh for the virtual movie
            _providerManager.QueueRefresh(virtualMovie.Id, new MetadataRefreshOptions(new DirectoryService(_fileSystem)), RefreshPriority.High);

            return virtualMovie;
        }

        /// <inheritdoc />
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _providerManager.RefreshCompleted += OnProviderManagerRefreshComplete;
            _libraryManager.ItemUpdated += OnLibraryManagerItemUpdated;
            _libraryManager.ItemRemoved += OnLibraryManagerItemRemoved;
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task StopAsync(CancellationToken cancellationToken)
        {
            _providerManager.RefreshCompleted -= OnProviderManagerRefreshComplete;
            _libraryManager.ItemUpdated -= OnLibraryManagerItemUpdated;
            _libraryManager.ItemRemoved -= OnLibraryManagerItemRemoved;
            return Task.CompletedTask;
        }
    }
}
