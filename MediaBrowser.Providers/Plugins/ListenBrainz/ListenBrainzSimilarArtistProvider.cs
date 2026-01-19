using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Providers.Plugins.ListenBrainz.Api;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Providers.Plugins.ListenBrainz;

/// <summary>
/// ListenBrainz-based similar items provider for music artists.
/// </summary>
public class ListenBrainzSimilarArtistProvider : IRemoteSimilarItemsProvider<MusicArtist>
{
    private readonly ListenBrainzLabsClient _labsClient;
    private readonly ILogger<ListenBrainzSimilarArtistProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListenBrainzSimilarArtistProvider"/> class.
    /// </summary>
    /// <param name="labsClient">The ListenBrainz Labs API client.</param>
    /// <param name="logger">The logger.</param>
    public ListenBrainzSimilarArtistProvider(
        ListenBrainzLabsClient labsClient,
        ILogger<ListenBrainzSimilarArtistProvider> logger)
    {
        _labsClient = labsClient;
        _logger = logger;
    }

    /// <inheritdoc/>
    public string Name => "ListenBrainz";

    /// <inheritdoc/>
    public MetadataPluginType Type => MetadataPluginType.SimilarityProvider;

    /// <inheritdoc/>
    public TimeSpan? CacheDuration => TimeSpan.FromDays(14);

    /// <inheritdoc/>
    public async IAsyncEnumerable<SimilarItemReference> GetSimilarItemsAsync(
        MusicArtist item,
        SimilarItemsQuery query,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(query);

        if (!item.TryGetProviderId(MetadataProvider.MusicBrainzArtist, out var mbidStr) || !Guid.TryParse(mbidStr, out var mbid))
        {
            _logger.LogDebug("No MusicBrainz Artist ID found for {ArtistName}", item.Name);
            yield break;
        }

        IReadOnlyList<Guid> similarMbids;
        try
        {
            similarMbids = await _labsClient.GetSimilarArtistsAsync(mbid, cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to fetch similar artists from ListenBrainz for {ArtistMbid}", mbid);
            yield break;
        }

        var providerName = MetadataProvider.MusicBrainzArtist.ToString();

        foreach (var similarMbid in similarMbids)
        {
            yield return new SimilarItemReference
            {
                ProviderName = providerName,
                ProviderId = similarMbid.ToString()
            };
        }
    }
}
