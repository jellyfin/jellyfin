using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
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
/// Music album metadata provider for MusicBrainz.
/// </summary>
public class MusicBrainzAlbumProvider : IRemoteMetadataProvider<MusicAlbum, AlbumInfo>, IHasOrder
{
    /// <inheritdoc />
    public string Name => "MusicBrainz";

    /// <inheritdoc />
    public int Order => 0;

    /// <inheritdoc />
    public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(AlbumInfo searchInfo, CancellationToken cancellationToken)
    {
        var query = MusicBrainz.Plugin.Instance!.MusicBrainzQuery;
        var releaseId = searchInfo.GetReleaseId();
        var releaseGroupId = searchInfo.GetReleaseGroupId();

        if (!string.IsNullOrEmpty(releaseId))
        {
            var releaseResult = await query.LookupReleaseAsync(new Guid(releaseId), Include.Artists | Include.ReleaseGroups, cancellationToken).ConfigureAwait(false);
            return GetReleaseResult(releaseResult).SingleItemAsEnumerable();
        }

        if (!string.IsNullOrEmpty(releaseGroupId))
        {
            var releaseGroupResult = await query.LookupReleaseGroupAsync(new Guid(releaseGroupId), Include.Releases, null, cancellationToken).ConfigureAwait(false);

            // No need to pass the cancellation token to GetReleaseGroupResultAsync as we're already passing it to ToBlockingEnumerable
            return GetReleaseGroupResultAsync(releaseGroupResult.Releases, CancellationToken.None).ToBlockingEnumerable(cancellationToken);
        }

        var artistMusicBrainzId = searchInfo.GetMusicBrainzArtistId();

        if (!string.IsNullOrWhiteSpace(artistMusicBrainzId))
        {
            var releaseSearchResults = await query.FindReleasesAsync($"\"{searchInfo.Name}\" AND arid:{artistMusicBrainzId}", null, null, false, cancellationToken)
                .ConfigureAwait(false);

            if (releaseSearchResults.Results.Count > 0)
            {
                return GetReleaseSearchResult(releaseSearchResults.Results);
            }
        }
        else
        {
            // I'm sure there is a better way but for now it resolves search for 12" Mixes
            var queryName = searchInfo.Name.Replace("\"", string.Empty, StringComparison.Ordinal);

            var releaseSearchResults = await query.FindReleasesAsync($"\"{queryName}\" AND artist:\"{searchInfo.GetAlbumArtist()}\"c", null, null, false, cancellationToken)
                .ConfigureAwait(false);

            if (releaseSearchResults.Results.Count > 0)
            {
                return GetReleaseSearchResult(releaseSearchResults.Results);
            }
        }

        return Enumerable.Empty<RemoteSearchResult>();
    }

    private IEnumerable<RemoteSearchResult> GetReleaseSearchResult(IEnumerable<ISearchResult<IRelease>>? releaseSearchResults)
    {
        if (releaseSearchResults is null)
        {
            yield break;
        }

        foreach (var result in releaseSearchResults)
        {
            yield return GetReleaseResult(result.Item);
        }
    }

    private async IAsyncEnumerable<RemoteSearchResult> GetReleaseGroupResultAsync(IEnumerable<IRelease>? releaseSearchResults, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (releaseSearchResults is null)
        {
            yield break;
        }

        var query = MusicBrainz.Plugin.Instance!.MusicBrainzQuery;
        foreach (var result in releaseSearchResults)
        {
            // Fetch full release info, otherwise artists are missing
            var fullResult = await query.LookupReleaseAsync(result.Id, Include.Artists | Include.ReleaseGroups, cancellationToken).ConfigureAwait(false);
            yield return GetReleaseResult(fullResult);
        }
    }

    private RemoteSearchResult GetReleaseResult(IRelease releaseSearchResult)
    {
        var searchResult = new RemoteSearchResult
        {
            Name = releaseSearchResult.Title,
            ProductionYear = releaseSearchResult.Date?.Year,
            PremiereDate = releaseSearchResult.Date?.NearestDate,
            SearchProviderName = Name
        };

        // Add artists and use first as album artist
        var artists = releaseSearchResult.ArtistCredit;
        if (artists is not null && artists.Count > 0)
        {
            var artistResults = new RemoteSearchResult[artists.Count];
            for (int i = 0; i < artists.Count; i++)
            {
                var artist = artists[i];
                var artistResult = new RemoteSearchResult
                {
                    Name = artist.Name
                };

                if (artist.Artist?.Id is not null)
                {
                    artistResult.SetProviderId(MetadataProvider.MusicBrainzArtist, artist.Artist!.Id.ToString());
                }

                artistResults[i] = artistResult;
            }

            searchResult.AlbumArtist = artistResults[0];
            searchResult.Artists = artistResults;
        }

        searchResult.SetProviderId(MetadataProvider.MusicBrainzAlbum, releaseSearchResult.Id.ToString());

        if (releaseSearchResult.ReleaseGroup?.Id is not null)
        {
            searchResult.SetProviderId(MetadataProvider.MusicBrainzReleaseGroup, releaseSearchResult.ReleaseGroup.Id.ToString());
        }

        return searchResult;
    }

    /// <inheritdoc />
    public async Task<MetadataResult<MusicAlbum>> GetMetadata(AlbumInfo info, CancellationToken cancellationToken)
    {
        // TODO: This sets essentially nothing. As-is, it's mostly useless. Make it actually pull metadata and use it.
        var query = MusicBrainz.Plugin.Instance!.MusicBrainzQuery;
        var releaseId = info.GetReleaseId();
        var releaseGroupId = info.GetReleaseGroupId();

        var result = new MetadataResult<MusicAlbum>
        {
            Item = new MusicAlbum()
        };

        // If there is a release group, but no release ID, try to match the release
        if (string.IsNullOrWhiteSpace(releaseId) && !string.IsNullOrWhiteSpace(releaseGroupId))
        {
            // TODO: Actually try to match the release. Simply taking the first result is stupid.
            var releaseGroup = await query.LookupReleaseGroupAsync(new Guid(releaseGroupId), Include.None, null, cancellationToken).ConfigureAwait(false);
            var release = releaseGroup.Releases?.Count > 0 ? releaseGroup.Releases[0] : null;
            if (release is not null)
            {
                releaseId = release.Id.ToString();
                result.HasMetadata = true;
            }
        }

        // If there is no release ID, lookup a release with the info we have
        if (string.IsNullOrWhiteSpace(releaseId))
        {
            var artistMusicBrainzId = info.GetMusicBrainzArtistId();
            IRelease? releaseResult = null;

            if (!string.IsNullOrEmpty(artistMusicBrainzId))
            {
                var releaseSearchResults = await query.FindReleasesAsync($"\"{info.Name}\" AND arid:{artistMusicBrainzId}", null, null, false, cancellationToken)
                    .ConfigureAwait(false);
                releaseResult = releaseSearchResults.Results.Count > 0 ? releaseSearchResults.Results[0].Item : null;
            }
            else if (!string.IsNullOrEmpty(info.GetAlbumArtist()))
            {
                var releaseSearchResults = await query.FindReleasesAsync($"\"{info.Name}\" AND artist:{info.GetAlbumArtist()}", null, null, false, cancellationToken)
                    .ConfigureAwait(false);
                releaseResult = releaseSearchResults.Results.Count > 0 ? releaseSearchResults.Results[0].Item : null;
            }

            if (releaseResult is not null)
            {
                releaseId = releaseResult.Id.ToString();

                if (releaseResult.ReleaseGroup?.Id is not null)
                {
                    releaseGroupId = releaseResult.ReleaseGroup.Id.ToString();
                }

                result.HasMetadata = true;
                result.Item.ProductionYear = releaseResult.Date?.Year;
                result.Item.Overview = releaseResult.Annotation;
            }
        }

        // If we have a release ID but not a release group ID, lookup the release group
        if (!string.IsNullOrWhiteSpace(releaseId) && string.IsNullOrWhiteSpace(releaseGroupId))
        {
            var release = await query.LookupReleaseAsync(new Guid(releaseId), Include.ReleaseGroups, cancellationToken).ConfigureAwait(false);
            releaseGroupId = release.ReleaseGroup?.Id.ToString();
            result.HasMetadata = true;
        }

        // If we have a release ID and a release group ID
        if (!string.IsNullOrWhiteSpace(releaseId) || !string.IsNullOrWhiteSpace(releaseGroupId))
        {
            result.HasMetadata = true;
        }

        if (result.HasMetadata)
        {
            if (!string.IsNullOrEmpty(releaseId))
            {
                result.Item.SetProviderId(MetadataProvider.MusicBrainzAlbum, releaseId);
            }

            if (!string.IsNullOrEmpty(releaseGroupId))
            {
                result.Item.SetProviderId(MetadataProvider.MusicBrainzReleaseGroup, releaseGroupId);
            }
        }

        return result;
    }

    /// <inheritdoc />
    public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
