using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Extensions;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using MediaBrowser.Providers.Extensions;
using MediaBrowser.Providers.Music;
using MetaBrainz.MusicBrainz;
using MetaBrainz.MusicBrainz.Interfaces.Entities;
using MetaBrainz.MusicBrainz.Interfaces.Searches;

namespace MediaBrowser.Providers.Plugins.MusicBrainz;

/// <summary>
/// MusicBrainz artist provider.
/// </summary>
public class MusicBrainzArtistProvider : IRemoteMetadataProvider<MusicArtist, ArtistInfo>, IHasOrder
{
    /// <inheritdoc />
    public string Name => "MusicBrainz";

    /// <inheritdoc />
    /// Runs first to populate the MusicBrainz artist ID used by downstream providers.
    public int Order => 0;

    /// <inheritdoc />
    public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(ArtistInfo searchInfo, CancellationToken cancellationToken)
    {
        var query = MusicBrainz.Plugin.Instance!.MusicBrainzQuery;
        var artistId = searchInfo.GetMusicBrainzArtistId();

        if (!string.IsNullOrWhiteSpace(artistId))
        {
            var artistResult = await query.LookupArtistAsync(new Guid(artistId), Include.Aliases, null, null, cancellationToken).ConfigureAwait(false);
            return GetResultFromResponse(artistResult).SingleItemAsEnumerable();
        }

        if (string.IsNullOrWhiteSpace(searchInfo.Name))
        {
            return [];
        }

        var artistSearchResults = await query.FindArtistsAsync($"\"{searchInfo.Name}\"", null, null, false, cancellationToken)
            .ConfigureAwait(false);
        if (artistSearchResults.Results.Count > 0)
        {
            return GetResultsFromResponse(artistSearchResults.Results);
        }

        if (searchInfo.Name.HasDiacritics())
        {
            // Try again using the search with an accented characters query
            var artistAccentsSearchResults = await query.FindArtistsAsync($"artistaccent:\"{searchInfo.Name}\"", null, null, false, cancellationToken)
                .ConfigureAwait(false);
            if (artistAccentsSearchResults.Results.Count > 0)
            {
                return GetResultsFromResponse(artistAccentsSearchResults.Results);
            }
        }

        return [];
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
            PremiereDate = artist.LifeSpan?.Begin?.NearestDate.AsCalendarDate(),
            SearchProviderName = Name,
        };

        searchResult.SetProviderId(MetadataProvider.MusicBrainzArtist, artist.Id.ToString());

        return searchResult;
    }

    /// <inheritdoc />
    public async Task<MetadataResult<MusicArtist>> GetMetadata(ArtistInfo info, CancellationToken cancellationToken)
    {
        var result = new MetadataResult<MusicArtist> { Item = new MusicArtist() };

        var musicBrainzId = info.GetMusicBrainzArtistId();

        // If we don't have an id yet, resolve one by name so we can look the artist up.
        if (string.IsNullOrWhiteSpace(musicBrainzId))
        {
            var searchResults = await GetSearchResults(info, cancellationToken).ConfigureAwait(false);
            musicBrainzId = searchResults.FirstOrDefault()?.GetProviderId(MetadataProvider.MusicBrainzArtist);
        }

        if (string.IsNullOrWhiteSpace(musicBrainzId))
        {
            return result;
        }

        var query = Plugin.Instance!.MusicBrainzQuery;
        var artist = await query.LookupArtistAsync(new Guid(musicBrainzId), Include.Genres | Include.Tags, null, null, cancellationToken).ConfigureAwait(false);

        if (artist is null)
        {
            return result;
        }

        result.HasMetadata = true;
        result.Item.SetProviderId(MetadataProvider.MusicBrainzArtist, artist.Id.ToString());

        if (Plugin.Instance!.Configuration.ReplaceArtistName && !string.IsNullOrWhiteSpace(artist.Name))
        {
            result.Item.Name = artist.Name;
        }

        if (artist.LifeSpan?.Begin is not null)
        {
            result.Item.PremiereDate = artist.LifeSpan.Begin.NearestDate.AsCalendarDate();
            result.Item.ProductionYear = artist.LifeSpan.Begin.Year;
        }

        if (artist.LifeSpan?.End is not null)
        {
            result.Item.EndDate = artist.LifeSpan.End.NearestDate.AsCalendarDate();
        }

        var location = string.IsNullOrWhiteSpace(artist.Area?.Name) ? artist.Country : artist.Area!.Name;
        if (!string.IsNullOrWhiteSpace(location))
        {
            result.Item.ProductionLocations = [location];
        }

        if (artist.Genres is not null && artist.Genres.Count > 0)
        {
            result.Item.Genres = artist.Genres
                .OrderByDescending(genre => genre.VoteCount)
                .Select(genre => genre.Name)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .ToArray();
        }

        if (artist.Tags is not null && artist.Tags.Count > 0)
        {
            result.Item.Tags = artist.Tags
                .OrderByDescending(tag => tag.VoteCount)
                .Select(tag => tag.Name)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .ToArray();
        }

        return result;
    }

    /// <inheritdoc />
    public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
