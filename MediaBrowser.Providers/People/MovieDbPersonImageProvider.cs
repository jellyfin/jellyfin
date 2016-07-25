using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Providers.Movies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.People
{
    public class MovieDbPersonImageProvider : IRemoteImageProvider, IHasOrder
    {
        private readonly IServerConfigurationManager _config;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IHttpClient _httpClient;

        public MovieDbPersonImageProvider(IServerConfigurationManager config, IJsonSerializer jsonSerializer, IHttpClient httpClient)
        {
            _config = config;
            _jsonSerializer = jsonSerializer;
            _httpClient = httpClient;
        }

        public string Name
        {
            get { return ProviderName; }
        }

        public static string ProviderName
        {
            get { return "TheMovieDb"; }
        }

        public bool Supports(IHasImages item)
        {
            return item is Person;
        }

        public IEnumerable<ImageType> GetSupportedImages(IHasImages item)
        {
            return new List<ImageType>
            {
                ImageType.Primary
            };
        }

        public async Task<IEnumerable<RemoteImageInfo>> GetImages(IHasImages item, CancellationToken cancellationToken)
        {
            var person = (Person)item;
            var id = person.GetProviderId(MetadataProviders.Tmdb);

            if (!string.IsNullOrEmpty(id))
            {
                await MovieDbPersonProvider.Current.EnsurePersonInfo(id, cancellationToken).ConfigureAwait(false);

                var dataFilePath = MovieDbPersonProvider.GetPersonDataFilePath(_config.ApplicationPaths, id);

                var result = _jsonSerializer.DeserializeFromFile<MovieDbPersonProvider.PersonResult>(dataFilePath);

                var images = result.images ?? new MovieDbPersonProvider.Images();

                var tmdbSettings = await MovieDbProvider.Current.GetTmdbSettings(cancellationToken).ConfigureAwait(false);

                var tmdbImageUrl = tmdbSettings.images.secure_base_url + "original";

                return GetImages(images, item.GetPreferredMetadataLanguage(), tmdbImageUrl);
            }

            return new List<RemoteImageInfo>();
        }

        private IEnumerable<RemoteImageInfo> GetImages(MovieDbPersonProvider.Images images, string preferredLanguage, string baseImageUrl)
        {
            var list = new List<RemoteImageInfo>();

            if (images.profiles != null)
            {
                list.AddRange(images.profiles.Select(i => new RemoteImageInfo
                {
                    ProviderName = Name,
                    Type = ImageType.Primary,
                    Width = i.width,
                    Height = i.height,
                    Language = GetLanguage(i),
                    Url = baseImageUrl + i.file_path
                }));
            }

            var language = preferredLanguage;

            var isLanguageEn = string.Equals(language, "en", StringComparison.OrdinalIgnoreCase);

            return list.OrderByDescending(i =>
            {
                if (string.Equals(language, i.Language, StringComparison.OrdinalIgnoreCase))
                {
                    return 3;
                }
                if (!isLanguageEn)
                {
                    if (string.Equals("en", i.Language, StringComparison.OrdinalIgnoreCase))
                    {
                        return 2;
                    }
                }
                if (string.IsNullOrEmpty(i.Language))
                {
                    return isLanguageEn ? 3 : 2;
                }
                return 0;
            })
                .ThenByDescending(i => i.CommunityRating ?? 0)
                .ThenByDescending(i => i.VoteCount ?? 0)
                .ToList();
        }

        private string GetLanguage(MovieDbPersonProvider.Profile profile)
        {
            return profile.iso_639_1 == null ? null : profile.iso_639_1.ToString();
        }

        public int Order
        {
            get { return 0; }
        }

        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return _httpClient.GetResponse(new HttpRequestOptions
            {
                CancellationToken = cancellationToken,
                Url = url,
                ResourcePool = MovieDbProvider.Current.MovieDbResourcePool
            });
        }
    }
}
