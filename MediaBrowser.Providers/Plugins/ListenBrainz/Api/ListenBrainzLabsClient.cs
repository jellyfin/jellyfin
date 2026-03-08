using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Providers.Plugins.ListenBrainz.Api.Models;
using MediaBrowser.Providers.Plugins.ListenBrainz.Configuration;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Providers.Plugins.ListenBrainz.Api;

/// <summary>
/// Client for the ListenBrainz Labs API.
/// </summary>
public class ListenBrainzLabsClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ListenBrainzLabsClient> _logger;
    private readonly Lock _rateLimitLock = new();

    private DateTime _lastRequestTime = DateTime.MinValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListenBrainzLabsClient"/> class.
    /// </summary>
    /// <param name="httpClientFactory">The HTTP client factory.</param>
    /// <param name="logger">The logger.</param>
    public ListenBrainzLabsClient(
        IHttpClientFactory httpClientFactory,
        ILogger<ListenBrainzLabsClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Gets similar artists for the given MusicBrainz artist ID.
    /// </summary>
    /// <param name="artistMbid">The MusicBrainz artist ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of similar artist MusicBrainz IDs ordered by similarity score.</returns>
    public async Task<IReadOnlyList<Guid>> GetSimilarArtistsAsync(
        Guid artistMbid,
        CancellationToken cancellationToken)
    {
        var config = ListenBrainzPlugin.Instance?.Configuration;
        var baseUrl = config?.LabsServer ?? PluginConfiguration.DefaultLabsServer;
        var algorithm = config?.AlgorithmString ?? new PluginConfiguration().AlgorithmString;
        var rateLimit = config?.RateLimit ?? PluginConfiguration.DefaultRateLimit;

        // Enforce rate limit
        EnforceRateLimit(rateLimit);

        var url = $"{baseUrl}/similar-artists/json?artist_mbids={artistMbid}&algorithm={algorithm}";

        _logger.LogDebug("Fetching similar artists from ListenBrainz Labs: {Url}", url);

        try
        {
            var httpClient = _httpClientFactory.CreateClient(NamedClient.Default);
            var response = await httpClient.GetFromJsonAsync<List<SimilarArtistData>>(url, cancellationToken).ConfigureAwait(false);

            if (response is null || response.Count == 0)
            {
                _logger.LogDebug("No similar artists found for {ArtistMbid}", artistMbid);
                return [];
            }

            var similarMbids = response
                .Where(a => !a.ArtistMbid.Equals(artistMbid)) // Exclude the source artist
                .OrderByDescending(a => a.Score)
                .Select(a => a.ArtistMbid)
                .ToList();

            _logger.LogDebug("Found {Count} similar artists for {ArtistMbid}", similarMbids.Count, artistMbid);

            return similarMbids;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to fetch similar artists from ListenBrainz Labs for {ArtistMbid}", artistMbid);
            return [];
        }
    }

    private void EnforceRateLimit(double rateLimitSeconds)
    {
        lock (_rateLimitLock)
        {
            var timeSinceLastRequest = DateTime.UtcNow - _lastRequestTime;
            var requiredDelay = TimeSpan.FromSeconds(rateLimitSeconds) - timeSinceLastRequest;

            if (requiredDelay > TimeSpan.Zero)
            {
                Thread.Sleep(requiredDelay);
            }

            _lastRequestTime = DateTime.UtcNow;
        }
    }
}
