#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Diacritics.Extensions;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using MediaBrowser.Providers.Plugins.MusicBrainz;
using MetaBrainz.MusicBrainz;
using MetaBrainz.MusicBrainz.Interfaces.Entities;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Providers.Music
{
    public class MusicBrainzArtistProvider : IRemoteMetadataProvider<MusicArtist, ArtistInfo>
    {
        private Query _musicBrainzQuery;

        public MusicBrainzArtistProvider()
        {
            _musicBrainzQuery = new Query();

            Current = this;
        }

        internal static MusicBrainzArtistProvider Current { get; private set; }

        /// <inheritdoc />
        public string Name => "MusicBrainz";

        /// <inheritdoc />
        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(ArtistInfo searchInfo, CancellationToken cancellationToken)
        {
            var artistId = searchInfo.GetMusicBrainzArtistId();

            if (!string.IsNullOrWhiteSpace(artistId))
            {
                var artistResult = await _musicBrainzQuery.LookupArtistAsync(new Guid(artistId)).ConfigureAwait(false);
                return GetResultsFromResponse(new[] { artistResult });
            }
            else
            {
                // They seem to throw bad request failures on any term with a slash
                var nameToSearch = searchInfo.Name.Replace('/', ' ');

                var artistSearchResults = await _musicBrainzQuery.FindArtistsAsync($"\"{searchInfo.Name}\"");
                if (artistSearchResults.Results.Count > 0)
                {
                    return GetResultsFromResponse(artistSearchResults.Results.Select(result => result.Item).ToList());
                }

                if (searchInfo.Name.HasDiacritics())
                {
                    // Try again using the search with an accented characters query
                    var artistAccentsSearchResults = await _musicBrainzQuery.FindArtistsAsync($"artistaccent:\"{searchInfo.Name}\"");
                    if (artistAccentsSearchResults.Results.Count > 0)
                    {
                        return GetResultsFromResponse(artistAccentsSearchResults.Results.Select(result => result.Item).ToList());
                    }
                }
            }

            return Enumerable.Empty<RemoteSearchResult>();
        }

        private IEnumerable<RemoteSearchResult> GetResultsFromResponse(IEnumerable<IArtist> releaseSearchResults)
        {
            return releaseSearchResults.Select(result =>
            {
                var searchResult = new RemoteSearchResult
                {
                    Name = result.Name,
                    ProductionYear = result.LifeSpan?.Begin?.Year,
                    PremiereDate = result.LifeSpan?.Begin?.NearestDate
                };

                searchResult.SetProviderId(MetadataProvider.MusicBrainzArtist, result.Id.ToString());

                return searchResult;
            });
        }

        /// <inheritdoc />
        public async Task<MetadataResult<MusicArtist>> GetMetadata(ArtistInfo info, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<MusicArtist>
            {
                Item = new MusicArtist()
            };

            var musicBrainzId = info.GetMusicBrainzArtistId();

            if (string.IsNullOrWhiteSpace(musicBrainzId))
            {
                var searchResults = await GetSearchResults(info, cancellationToken).ConfigureAwait(false);

                var singleResult = searchResults.FirstOrDefault();

                if (singleResult != null)
                {
                    musicBrainzId = singleResult.GetProviderId(MetadataProvider.MusicBrainzArtist);
                    result.Item.Overview = singleResult.Overview;

                    if (Plugin.Instance.Configuration.ReplaceArtistName)
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

        public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
