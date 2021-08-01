using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using MetaBrainz.MusicBrainz;
using MetaBrainz.MusicBrainz.Interfaces.Entities;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Providers.Music
{
    /// <summary>
    /// Music album metadata provider for MusicBrainz.
    /// </summary>
    public class MusicBrainzAlbumProvider : IRemoteMetadataProvider<MusicAlbum, AlbumInfo>, IHasOrder
    {
        private Query _musicBrainzQuery;

        /// <summary>
        /// Initializes a new instance of the <see cref="MusicBrainzAlbumProvider"/> class.
        /// </summary>
        public MusicBrainzAlbumProvider()
        {
            _musicBrainzQuery = new Query();

            Current = this;
        }

        internal static MusicBrainzAlbumProvider Current { get; private set; }

        /// <inheritdoc />
        public string Name => "MusicBrainz";

        /// <inheritdoc />
        public int Order => 0;

        /// <inheritdoc />
        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(AlbumInfo searchInfo, CancellationToken cancellationToken)
        {
            var releaseId = searchInfo.GetReleaseId();
            var releaseGroupId = searchInfo.GetReleaseGroupId();

            if (!string.IsNullOrEmpty(releaseId))
            {
                var releaseResult = await _musicBrainzQuery.LookupReleaseAsync(new Guid(releaseId)).ConfigureAwait(false);
                return GetResultsFromResponse(new[] { releaseResult });
            }
            else if (!string.IsNullOrEmpty(releaseGroupId))
            {
                var releaseGroupResult = await _musicBrainzQuery.LookupReleaseGroupAsync(new Guid(releaseGroupId)).ConfigureAwait(false);
                return GetResultsFromResponse(releaseGroupResult.Releases);
            }
            else
            {
                var artistMusicBrainzId = searchInfo.GetMusicBrainzArtistId();

                if (!string.IsNullOrWhiteSpace(artistMusicBrainzId))
                {
                    var releaseSearchResults = await _musicBrainzQuery.FindReleasesAsync($"\"{searchInfo.Name}\" AND arid:{artistMusicBrainzId}");

                    if (releaseSearchResults.Results.Count > 0)
                    {
                        return GetResultsFromResponse(releaseSearchResults.Results.Select(result => result.Item).ToList());
                    }
                }
                else
                {
                    // I'm sure there is a better way but for now it resolves search for 12" Mixes
                    var queryName = searchInfo.Name.Replace("\"", string.Empty, StringComparison.Ordinal);

                    var releaseSearchResults = await _musicBrainzQuery.FindReleasesAsync($"\"{queryName}\" AND artist:\"{searchInfo.GetAlbumArtist()}\"c");

                    if (releaseSearchResults.Results.Count > 0)
                    {
                        return GetResultsFromResponse(releaseSearchResults.Results.Select(result => result.Item).ToList());
                    }
                }
            }

            return Enumerable.Empty<RemoteSearchResult>();
        }

        private IEnumerable<RemoteSearchResult> GetResultsFromResponse(IEnumerable<IRelease> releaseSearchResults)
        {
            return releaseSearchResults.Select(result =>
            {
                var searchResult = new RemoteSearchResult
                {
                    Name = result.Title,
                    ProductionYear = result.Date?.Year,
                    PremiereDate = result.Date?.NearestDate
                };

                if (result.ArtistCredit?.Count > 0)
                {
                    searchResult.AlbumArtist = new RemoteSearchResult
                    {
                        SearchProviderName = Name,
                        Name = result.ArtistCredit[0].Name
                    };

                    if (result.ArtistCredit[0].Artist?.Id is not null)
                    {
                        searchResult.AlbumArtist.SetProviderId(MetadataProvider.MusicBrainzArtist, result.ArtistCredit[0].Artist?.Id.ToString());
                    }
                }

                searchResult.SetProviderId(MetadataProvider.MusicBrainzAlbum, result.Id.ToString());

                if (result.ReleaseGroup?.Id is not null)
                {
                    searchResult.SetProviderId(MetadataProvider.MusicBrainzReleaseGroup, result.ReleaseGroup.Id.ToString());
                }

                return searchResult;
            });
        }

        /// <inheritdoc />
        public async Task<MetadataResult<MusicAlbum>> GetMetadata(AlbumInfo info, CancellationToken cancellationToken)
        {
            // TODO: This sets essentially nothing. As-is, it's mostly useless. Make it actually pull metadata and use it.
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
                var releaseGroup = await _musicBrainzQuery.LookupReleaseGroupAsync(new Guid(releaseGroupId)).ConfigureAwait(false);
                var release = releaseGroup.Releases?.FirstOrDefault();
                releaseId = release?.Id.ToString();
                result.HasMetadata = true;
            }

            // If there is no release ID, lookup a release with the info we have
            if (string.IsNullOrWhiteSpace(releaseId))
            {
                var artistMusicBrainzId = info.GetMusicBrainzArtistId();
                IRelease releaseResult = null;

                if (!string.IsNullOrEmpty(artistMusicBrainzId))
                {
                    var releaseSearchResults = await _musicBrainzQuery.FindReleasesAsync($"\"{info.Name}\" AND arid:{artistMusicBrainzId}");
                    releaseResult = releaseSearchResults.Results.FirstOrDefault()?.Item;
                }
                else if (!string.IsNullOrEmpty(info.GetAlbumArtist()))
                {
                    var releaseSearchResults = await _musicBrainzQuery.FindReleasesAsync($"\"{info.Name}\" AND artist:{info.GetAlbumArtist()}");
                    releaseResult = releaseSearchResults.Results.FirstOrDefault()?.Item;
                }

                if (releaseResult != null)
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
                var release = await _musicBrainzQuery.LookupReleaseAsync(new Guid(releaseId)).ConfigureAwait(false);
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
}
