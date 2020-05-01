using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Providers.Tmdb.Models.General;
using MediaBrowser.Providers.Tmdb.Models.People;
using MediaBrowser.Providers.Tmdb.Movies;

namespace MediaBrowser.Providers.Tmdb.People
{
    public class TmdbPersonImageProvider : IRemoteImageProvider, IHasOrder
    {
        private readonly IServerConfigurationManager _config;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IHttpClient _httpClient;

        public TmdbPersonImageProvider(IServerConfigurationManager config, IJsonSerializer jsonSerializer, IHttpClient httpClient)
        {
            _config = config;
            _jsonSerializer = jsonSerializer;
            _httpClient = httpClient;
        }

        public string Name => ProviderName;

        public static string ProviderName => TmdbUtils.ProviderName;

        public bool Supports(BaseItem item)
        {
            return item is Person;
        }

        public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
        {
            return new List<ImageType>
            {
                ImageType.Primary
            };
        }

        public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
        {
            var person = (Person)item;
            var id = person.GetProviderId(MetadataProviders.Tmdb);

            if (!string.IsNullOrEmpty(id))
            {
                await TmdbPersonProvider.Current.EnsurePersonInfo(id, cancellationToken).ConfigureAwait(false);

                var dataFilePath = TmdbPersonProvider.GetPersonDataFilePath(_config.ApplicationPaths, id);

                var result = _jsonSerializer.DeserializeFromFile<PersonResult>(dataFilePath);

                var images = result.Images ?? new PersonImages();

                var tmdbSettings = await TmdbMovieProvider.Current.GetTmdbSettings(cancellationToken).ConfigureAwait(false);

                var tmdbImageUrl = tmdbSettings.images.GetImageUrl("original");

                return GetImages(images, item.GetPreferredMetadataLanguage(), tmdbImageUrl);
            }

            return new List<RemoteImageInfo>();
        }

        private IEnumerable<RemoteImageInfo> GetImages(PersonImages images, string preferredLanguage, string baseImageUrl)
        {
            var list = new List<RemoteImageInfo>();

            if (images.Profiles != null)
            {
                list.AddRange(images.Profiles.Select(i => new RemoteImageInfo
                {
                    ProviderName = Name,
                    Type = ImageType.Primary,
                    Width = i.Width,
                    Height = i.Height,
                    Language = GetLanguage(i),
                    Url = baseImageUrl + i.File_Path
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
                .ThenByDescending(i => i.VoteCount ?? 0);
        }

        private string GetLanguage(Profile profile)
        {
            return profile.Iso_639_1?.ToString();
        }

        public int Order => 0;

        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return _httpClient.GetResponse(new HttpRequestOptions
            {
                CancellationToken = cancellationToken,
                Url = url
            });
        }
    }
}
