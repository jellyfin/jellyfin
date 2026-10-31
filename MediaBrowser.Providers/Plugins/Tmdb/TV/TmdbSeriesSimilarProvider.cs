using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
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
    public TimeSpan? CacheDuration
    {
        get
        {
            var days = Plugin.Instance?.Configuration.SimilarItemsCacheDays ?? 0;
            return days > 0 ? TimeSpan.FromDays(days) : null;
        }
    }

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
        var recommendations = _tmdbClientManager
            .GetSeriesRecommendationsAsync(tmdbId, TmdbUtils.GetImageLanguagesParam(string.Empty), cancellationToken)
            .GetAsyncEnumerator(cancellationToken);

        await using (recommendations.ConfigureAwait(false))
        {
            while (true)
            {
                try
                {
                    if (!await recommendations.MoveNextAsync().ConfigureAwait(false))
                    {
                        yield break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get recommended TV shows from TMDb for {TmdbId}", tmdbId);
                    yield break;
                }

                yield return new SimilarItemReference
                {
                    ProviderName = providerName,
                    ProviderId = recommendations.Current.Id.ToString(CultureInfo.InvariantCulture)
                };
            }
        }
    }
}
