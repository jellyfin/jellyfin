using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Providers.Plugins.Tmdb.TV;

/// <summary>
/// TMDb-based similar items provider for TV series.
/// </summary>
public class TmdbSeriesSimilarProvider : IRemoteSimilarItemsProvider<Series>
{
    private readonly TmdbClientManager _tmdbClientManager;
    private readonly ILogger<TmdbSeriesSimilarProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TmdbSeriesSimilarProvider"/> class.
    /// </summary>
    /// <param name="tmdbClientManager">The TMDb client manager.</param>
    /// <param name="logger">The logger.</param>
    public TmdbSeriesSimilarProvider(TmdbClientManager tmdbClientManager, ILogger<TmdbSeriesSimilarProvider> logger)
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
        Series item,
        SimilarItemsQuery query,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (!item.TryGetProviderId(MetadataProvider.Tmdb, out var tmdbIdStr) || !int.TryParse(tmdbIdStr, CultureInfo.InvariantCulture, out var tmdbId))
        {
            yield break;
        }

        var providerName = MetadataProvider.Tmdb.ToString();
        var page = 1;
        var totalPages = 1;

        while (page <= totalPages && !cancellationToken.IsCancellationRequested)
        {
            IReadOnlyList<TMDbLib.Objects.Search.SearchTv> pageResults;
            try
            {
                (pageResults, totalPages) = await _tmdbClientManager
                    .GetSeriesSimilarPageAsync(tmdbId, page, TmdbUtils.GetImageLanguagesParam(string.Empty), cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get similar TV shows from TMDb for {TmdbId} page {Page}", tmdbId, page);
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
