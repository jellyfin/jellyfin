using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Jellyfin.Extensions;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using MediaBrowser.Providers.Music;
using MetaBrainz.MusicBrainz;
using MetaBrainz.MusicBrainz.Interfaces.Entities;
using MetaBrainz.MusicBrainz.Interfaces.Searches;

namespace MediaBrowser.Providers.Plugins.MusicBrainz;

/// <summary>
/// MusicBrainz artist provider.
/// </summary>
public class MusicBrainzArtistProvider : IRemoteMetadataProvider<MusicArtist, ArtistInfo>, IDisposable
{
    private readonly Query _musicBrainzQuery;

    /// <summary>
    /// Initializes a new instance of the <see cref="MusicBrainzArtistProvider"/> class.
    /// </summary>
    public MusicBrainzArtistProvider()
    {
        MusicBrainz.Plugin.Instance!.ConfigurationChanged += (_, _) =>
            {
                Query.DefaultServer = MusicBrainz.Plugin.Instance.Configuration.Server;
                Query.DelayBetweenRequests = MusicBrainz.Plugin.Instance.Configuration.RateLimit;
            };

        _musicBrainzQuery = new Query();
    }

    /// <inheritdoc />
    public string Name => "MusicBrainz";

    /// <inheritdoc />
    public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(ArtistInfo searchInfo, CancellationToken cancellationToken)
    {
        var artistId = searchInfo.GetMusicBrainzArtistId();

        if (!string.IsNullOrWhiteSpace(artistId))
        {
            var artistResult = await _musicBrainzQuery.LookupArtistAsync(new Guid(artistId), Include.Aliases, null, null, cancellationToken).ConfigureAwait(false);
            return GetResultFromResponse(artistResult).SingleItemAsEnumerable();
        }

        var artistSearchResults = await _musicBrainzQuery.FindArtistsAsync($"\"{searchInfo.Name}\"", null, null, false, cancellationToken)
            .ConfigureAwait(false);
        if (artistSearchResults.Results.Count > 0)
        {
            return GetResultsFromResponse(artistSearchResults.Results);
        }

        if (searchInfo.Name.HasDiacritics())
        {
            // Try again using the search with an accented characters query
            var artistAccentsSearchResults = await _musicBrainzQuery.FindArtistsAsync($"artistaccent:\"{searchInfo.Name}\"", null, null, false, cancellationToken)
                .ConfigureAwait(false);
            if (artistAccentsSearchResults.Results.Count > 0)
            {
                return GetResultsFromResponse(artistAccentsSearchResults.Results);
            }
        }

        return Enumerable.Empty<RemoteSearchResult>();
    }

    private IEnumerable<RemoteSearchResult> GetResultsFromResponse(IEnumerable<ISearchResult<IArtist>>? releaseSearchResults)
    {
        if (releaseSearchResults is null)
        {
            yield break;
        }

        foreach (var result in releaseSearchResults)
        {
            yield return GetResultFromResponse(result.Item);
        }
    }

    private RemoteSearchResult GetResultFromResponse(IArtist artist)
    {
        var searchResult = new RemoteSearchResult
        {
            Name = artist.Name,
            ProductionYear = artist.LifeSpan?.Begin?.Year,
            PremiereDate = artist.LifeSpan?.Begin?.NearestDate
        };

        searchResult.SetProviderId(MetadataProvider.MusicBrainzArtist, artist.Id.ToString());

        return searchResult;
    }

    /// <inheritdoc />
    public async Task<MetadataResult<MusicArtist>> GetMetadata(ArtistInfo info, CancellationToken cancellationToken)
    {
        var result = new MetadataResult<MusicArtist> { Item = new MusicArtist() };

        var musicBrainzId = info.GetMusicBrainzArtistId();

        if (string.IsNullOrWhiteSpace(musicBrainzId))
        {
            var searchResults = await GetSearchResults(info, cancellationToken).ConfigureAwait(false);

            var singleResult = searchResults.FirstOrDefault();

            if (singleResult != null)
            {
                musicBrainzId = singleResult.GetProviderId(MetadataProvider.MusicBrainzArtist);
                result.Item.Overview = singleResult.Overview;

                if (Plugin.Instance!.Configuration.ReplaceArtistName)
                {
                    result.Item.Name = singleResult.Name;
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(musicBrainzId))
        {
            result.HasMetadata = true;
            result.Item.SetProviderId(MetadataProvider.MusicBrainzArtist, musicBrainzId);
        }

        return result;
    }

    /// <inheritdoc />
    public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Dispose all resources.
    /// </summary>
    /// <param name="disposing">Whether to dispose.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _musicBrainzQuery.Dispose();
        }
    }
}
