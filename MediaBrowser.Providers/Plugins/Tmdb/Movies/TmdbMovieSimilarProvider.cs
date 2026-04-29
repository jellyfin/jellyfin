using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;
using Movie = MediaBrowser.Controller.Entities.Movies.Movie;

namespace MediaBrowser.Providers.Plugins.Tmdb.Movies;

/// <summary>
/// TMDb-based similar items provider for movies.
/// </summary>
public class TmdbMovieSimilarProvider : IRemoteSimilarItemsProvider<Movie>
{
    private readonly TmdbClientManager _tmdbClientManager;
    private readonly ILogger<TmdbMovieSimilarProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TmdbMovieSimilarProvider"/> class.
    /// </summary>
    /// <param name="tmdbClientManager">The TMDb client manager.</param>
    /// <param name="logger">The logger.</param>
    public TmdbMovieSimilarProvider(TmdbClientManager tmdbClientManager, ILogger<TmdbMovieSimilarProvider> logger)
    {
        _tmdbClientManager = tmdbClientManager;
        _logger = logger;
    }

    /// <inheritdoc/>
    public string Name => TmdbUtils.ProviderName;

    /// <inheritdoc/>
    public MetadataPluginType Type => MetadataPluginType.SimilarityProvider;

    /// <inheritdoc/>
    public TimeSpan? CacheDuration => TimeSpan.FromDays(7);

    /// <inheritdoc/>
    public async IAsyncEnumerable<SimilarItemReference> GetSimilarItemsAsync(
        Movie item,
        SimilarItemsQuery query,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (!item.TryGetProviderId(MetadataProvider.Tmdb, out var tmdbIdStr) || !int.TryParse(tmdbIdStr, CultureInfo.InvariantCulture, out var tmdbId))
        {
            yield break;
        }

        var providerName = MetadataProvider.Tmdb.ToString();
        var page = 0;
        var totalPages = 1;

        while (page <= totalPages && !cancellationToken.IsCancellationRequested)
        {
            IReadOnlyList<TMDbLib.Objects.Search.SearchMovie> pageResults;
            try
            {
                (pageResults, totalPages) = await _tmdbClientManager
                    .GetMovieSimilarPageAsync(tmdbId, page, TmdbUtils.GetImageLanguagesParam(string.Empty), cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get similar movies from TMDb for {TmdbId} page {Page}", tmdbId, page);
                yield break;
            }

            if (pageResults.Count == 0)
            {
                yield break;
            }

            foreach (var similar in pageResults)
            {
                yield return new SimilarItemReference
                {
                    ProviderName = providerName,
                    ProviderId = similar.Id.ToString(CultureInfo.InvariantCulture)
                };
            }

            page++;
        }
    }
}
