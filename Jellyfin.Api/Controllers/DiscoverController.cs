// Jellyfin.Api.Controllers.DiscoverController.cs
// This file is part of the Discover Feature for Jellyfin.
// It provides an API controller to search for movies and series using TMDb.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Controllers;
using Jellyfin.Api.Models;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Enums;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Providers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers
{
    /// <summary>
    /// Controller for discovering new media.
    /// </summary>
    [ApiController]
    // Use relative route to avoid double prefixing with base controller
    [Route("Discover")]
    [Authorize]
    public class DiscoverController : BaseJellyfinApiController
    {
        private readonly IProviderManager _providerManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="DiscoverController"/> class.
        /// </summary>
        /// <param name="providerManager">The provider manager.</param>
        public DiscoverController(IProviderManager providerManager)
        {
            _providerManager = providerManager;
        }

        /// <summary>
        /// Searches for movies and series based on a query string.
        /// </summary>
        /// <param name="query"><see cref="string"/> query to search for movies and series.</param>
        /// <param name="maxResults">Maximum number of results to fetch for each type (movies/series).</param>
        /// <param name="sortBy">Optional. Specify one or more sort orders, comma delimited. Options: Popularity, Name, ProductionYear.</param>
        /// <param name="sortOrder">Sort Order - Ascending, Descending.</param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/> to cancel the operation.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        [HttpGet("Search")]
        // TODO: Remove Console.WriteLine statements in production
        public async Task<IActionResult> Search(
            [FromQuery] string query,
            [FromQuery] int maxResults = 50,
            [FromQuery] string sortBy = "Popularity",
            [FromQuery] string sortOrder = "Descending",
            CancellationToken cancellationToken = default)
        {
            Console.WriteLine($"[DiscoverController] Received /Discover/Search with query: '{query}'");
            if (string.IsNullOrWhiteSpace(query))
            {
                Console.WriteLine("[DiscoverController] Query is empty or whitespace, returning 400");
                return BadRequest("Query cannot be empty");
            }

            // Helper to fetch multiple pages from TMDb
            async Task<List<RemoteSearchResult>> FetchAllResults<TItem, TInfo>(string providerName, string name, int pageSize)
                where TItem : MediaBrowser.Controller.Entities.BaseItem, new()
                where TInfo : ItemLookupInfo, new()
            {
                var allResults = new List<RemoteSearchResult>();
                int page = 1;
                while (allResults.Count < pageSize)
                {
                    var searchQuery = new RemoteSearchQuery<TInfo>
                    {
                        SearchInfo = new TInfo { Name = name },
                        SearchProviderName = providerName
                    };
                    var results = await _providerManager.GetRemoteSearchResults<TItem, TInfo>(searchQuery, cancellationToken).ConfigureAwait(false);
                    if (results == null || !results.Any())
                    {
                        break;
                    }

                    allResults.AddRange(results);
                    if (results.Count() < 20)
                    {
                        break;
                    }

                    page++;
                    // TODO: If you add real paging support, set the page in SearchInfo here
                }

                return allResults.Take(pageSize).ToList();
            }

            // Fetch up to maxResults movies and series
            var movies = await FetchAllResults<Movie, MovieInfo>("TheMovieDb", query, maxResults).ConfigureAwait(false);
            var series = await FetchAllResults<Series, SeriesInfo>("TheMovieDb", query, maxResults).ConfigureAwait(false);

            // Deduplicate by TMDb ID (or fallback to name if missing)
            movies = movies
                .GroupBy(m => m.ProviderIds != null && m.ProviderIds.TryGetValue("Tmdb", out var id) ? id : m.Name)
                .Select(g => g.First())
                .ToList();

            series = series
                .GroupBy(s => s.ProviderIds != null && s.ProviderIds.TryGetValue("Tmdb", out var id) ? id : s.Name)
                .Select(g => g.First())
                .ToList();

            // Map Movie Results to DiscoverItemDto to match Jellyfin's structure
            var mappedMovies = new List<DiscoverItemDto>();
            foreach (var m in movies)
            {
                string id = m.Name + "_movie";
                if (m.ProviderIds != null && m.ProviderIds.TryGetValue("Tmdb", out var tmdbId))
                {
                    id = tmdbId;
                }

                if (!string.IsNullOrWhiteSpace(m.ImageUrl))
                {
                    mappedMovies.Add(new DiscoverItemDto
                    {
                        Id = id,
                        Name = m.Name,
                        ProductionYear = m.ProductionYear,
                        Type = "Movie",
                        PrimaryImageTag = m.ImageUrl,
                        Overview = m.Overview,
                        Popularity = m.Popularity
                    });
                }
            }

            // Map Series Results to DiscoverItemDto to match Jellyfin's structure
            var mappedSeries = new List<DiscoverItemDto>();
            foreach (var s in series)
            {
                string id = s.Name + "_series";
                if (s.ProviderIds != null && s.ProviderIds.TryGetValue("Tmdb", out var tmdbId))
                {
                    id = tmdbId;
                }

                if (!string.IsNullOrWhiteSpace(s.ImageUrl))
                {
                    mappedSeries.Add(new DiscoverItemDto
                    {
                        Id = id,
                        Name = s.Name,
                        ProductionYear = s.ProductionYear,
                        Type = "Series",
                        PrimaryImageTag = s.ImageUrl,
                        Overview = s.Overview,
                        Popularity = s.Popularity
                    });
                }
            }

            // Parse sortBy and sortOrder (comma delimited, like ItemsController)
            var sortFields = (sortBy ?? "Popularity").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            // Use the SortOrder enum for robust comparison
            var descending = SortOrder.Descending.ToString().Equals(sortOrder, StringComparison.OrdinalIgnoreCase);

            // Helper for sorting by multiple fields using ItemSortBy enum
            IEnumerable<DiscoverItemDto> SortItems(IEnumerable<DiscoverItemDto> items)
            {
                // If any sort field is 'Random', shuffle the items and return
                if (sortFields.Any(f => f.Equals("Random", StringComparison.OrdinalIgnoreCase)))
                {
                    // Fast non-cryptographic shuffle using xorshift
                    var list = items.ToList();
                    int n = list.Count;
                    // Seed with current time ticks for new order each request
                    uint x = (uint)DateTime.UtcNow.Ticks;
                    for (int i = n - 1; i > 0; i--)
                    {
                        // Xorshift32
                        x ^= x << 13;
                        x ^= x >> 17;
                        x ^= x << 5;
                        int k = (int)(x % (uint)(i + 1));
                        var value = list[k];
                        list[k] = list[i];
                        list[i] = value;
                    }

                    return list;
                }

                IOrderedEnumerable<DiscoverItemDto>? ordered = null;
                foreach (var (field, idx) in sortFields.Select((f, i) => (f, i)))
                {
                    // Only allow supported sort fields
                    ItemSortBy sortEnum;
                    if (!Enum.TryParse<ItemSortBy>(field, true, out sortEnum) ||
                        !(sortEnum == ItemSortBy.Name || sortEnum == ItemSortBy.ProductionYear))
                    {
                        // Fallback to Default (used for Popularity)
                        sortEnum = ItemSortBy.Default;
                    }

                    Func<DiscoverItemDto, object?> keySelector = sortEnum switch
                    {
                        ItemSortBy.Name => x => x.Name ?? string.Empty,
                        ItemSortBy.ProductionYear => x => x.ProductionYear ?? 0,
                        ItemSortBy.Default => x => x.Popularity ?? double.MinValue,
                        _ => x => x.Popularity ?? double.MinValue // Fallback
                    };
                    if (idx == 0)
                    {
                        ordered = descending
                            ? items.OrderByDescending(keySelector)
                            : items.OrderBy(keySelector);
                    }
                    else if (ordered != null)
                    {
                        ordered = descending
                            ? ordered.ThenByDescending(keySelector)
                            : ordered.ThenBy(keySelector);
                    }
                }

                return ordered ?? items.OrderBy(x => 0); // fallback: no sort
            }

            // Always limit to maxResults items for both movies and series
            mappedMovies = SortItems(mappedMovies).Take(maxResults).ToList();
            mappedSeries = SortItems(mappedSeries).Take(maxResults).ToList();

            var result = new
            {
                Movies = mappedMovies,
                Series = mappedSeries
            };

            Console.WriteLine($"[DiscoverController] mappedMovies: {System.Text.Json.JsonSerializer.Serialize(mappedMovies)}");
            Console.WriteLine($"[DiscoverController] mappedSeries: {System.Text.Json.JsonSerializer.Serialize(mappedSeries)}");
            Console.WriteLine($"[DiscoverController] result object: {System.Text.Json.JsonSerializer.Serialize(result)}");
            Console.WriteLine($"[DiscoverController] Returning {mappedMovies.Count} movies and {mappedSeries.Count} series");
            return new JsonResult(result);
        }
    }
}
