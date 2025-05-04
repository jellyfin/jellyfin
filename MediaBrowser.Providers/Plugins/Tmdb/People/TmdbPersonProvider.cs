using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace MediaBrowser.Providers.Plugins.Tmdb.People
{
    /// <summary>
    /// Person image provider powered by TMDb.
    /// </summary>
    public class TmdbPersonProvider : IRemoteMetadataProvider<Person, PersonLookupInfo>
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly TmdbClientManager _tmdbClientManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="TmdbPersonProvider"/> class.
        /// </summary>
        /// <param name="httpClientFactory">The <see cref="IHttpClientFactory"/>.</param>
        /// <param name="tmdbClientManager">The <see cref="TmdbClientManager"/>.</param>
        public TmdbPersonProvider(IHttpClientFactory httpClientFactory, TmdbClientManager tmdbClientManager)
        {
            _httpClientFactory = httpClientFactory;
            _tmdbClientManager = tmdbClientManager;
        }

        /// <inheritdoc />
        public string Name => TmdbUtils.ProviderName;

        /// <inheritdoc />
        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(PersonLookupInfo searchInfo, CancellationToken cancellationToken)
        {
            if (searchInfo.TryGetProviderId(MetadataProvider.Tmdb, out var personTmdbId))
            {
                var personResult = await _tmdbClientManager.GetPersonAsync(int.Parse(personTmdbId, CultureInfo.InvariantCulture), searchInfo.MetadataLanguage, cancellationToken).ConfigureAwait(false);

                if (personResult is not null)
                {
                    var result = new RemoteSearchResult
                    {
                        Name = personResult.Name,
                        SearchProviderName = Name,
                        Overview = personResult.Biography
                    };

                    if (personResult.Images?.Profiles is not null && personResult.Images.Profiles.Count > 0)
                    {
                        result.ImageUrl = _tmdbClientManager.GetProfileUrl(personResult.Images.Profiles[0].FilePath);
                    }

                    result.SetProviderId(MetadataProvider.Tmdb, personResult.Id.ToString(CultureInfo.InvariantCulture));
                    result.TrySetProviderId(MetadataProvider.Imdb, personResult.ExternalIds.ImdbId);

                    return new[] { result };
                }
            }

            var personSearchResult = await _tmdbClientManager.SearchPersonAsync(searchInfo.Name, cancellationToken).ConfigureAwait(false);

            var remoteSearchResults = new RemoteSearchResult[personSearchResult.Count];
            for (var i = 0; i < personSearchResult.Count; i++)
            {
                var person = personSearchResult[i];
                var remoteSearchResult = new RemoteSearchResult
                {
                    SearchProviderName = Name,
                    Name = person.Name,
                    ImageUrl = _tmdbClientManager.GetProfileUrl(person.ProfilePath)
                };

                remoteSearchResult.SetProviderId(MetadataProvider.Tmdb, person.Id.ToString(CultureInfo.InvariantCulture));
                remoteSearchResults[i] = remoteSearchResult;
            }

            return remoteSearchResults;
        }

        /// <inheritdoc />
        public async Task<MetadataResult<Person>> GetMetadata(PersonLookupInfo info, CancellationToken cancellationToken)
        {
            var personTmdbId = Convert.ToInt32(info.GetProviderId(MetadataProvider.Tmdb), CultureInfo.InvariantCulture);

            // We don't already have an Id, need to fetch it
            if (personTmdbId <= 0)
            {
                var personSearchResults = await _tmdbClientManager.SearchPersonAsync(info.Name, cancellationToken).ConfigureAwait(false);
                if (personSearchResults.Count > 0)
                {
                    personTmdbId = personSearchResults[0].Id;
                }
            }

            var result = new MetadataResult<Person>();

            if (personTmdbId > 0)
            {
                var person = await _tmdbClientManager.GetPersonAsync(personTmdbId, info.MetadataLanguage, cancellationToken).ConfigureAwait(false);
                if (person is null)
                {
                    return result;
                }

                result.HasMetadata = true;

                var item = new Person
                {
                    // Take name from incoming info, don't rename the person
                    // TODO: This should go in PersonMetadataService, not each person provider
                    Name = info.Name,
                    HomePageUrl = person.Homepage,
                    Overview = person.Biography,
                    PremiereDate = person.Birthday?.ToUniversalTime(),
                    EndDate = person.Deathday?.ToUniversalTime()
                };

                if (!string.IsNullOrWhiteSpace(person.PlaceOfBirth))
                {
                    item.ProductionLocations = new[] { person.PlaceOfBirth };
                }

                item.SetProviderId(MetadataProvider.Tmdb, person.Id.ToString(CultureInfo.InvariantCulture));
                item.TrySetProviderId(MetadataProvider.Imdb, person.ImdbId);

                result.HasMetadata = true;
                result.Item = item;
            }

            return result;
        }

        /// <inheritdoc />
        public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return _httpClientFactory.CreateClient(NamedClient.Default).GetAsync(url, cancellationToken);
        }
    }
}
