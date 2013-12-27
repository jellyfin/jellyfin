using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.Movies
{
    public class ManualMovieDbPersonImageProvider : IImageProvider
    {
        private readonly IServerConfigurationManager _config;
        private readonly IJsonSerializer _jsonSerializer;

        public ManualMovieDbPersonImageProvider(IServerConfigurationManager config, IJsonSerializer jsonSerializer)
        {
            _config = config;
            _jsonSerializer = jsonSerializer;
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

        public async Task<IEnumerable<RemoteImageInfo>> GetImages(IHasImages item, ImageType imageType, CancellationToken cancellationToken)
        {
            var images = await GetAllImages(item, cancellationToken).ConfigureAwait(false);

            return images.Where(i => i.Type == imageType);
        }

        public async Task<IEnumerable<RemoteImageInfo>> GetAllImages(IHasImages item, CancellationToken cancellationToken)
        {
            var person = (Person)item;
            var id = person.GetProviderId(MetadataProviders.Tmdb);

            if (!string.IsNullOrEmpty(id))
            {
                await MovieDbPersonProvider.Current.DownloadPersonInfoIfNeeded(id, cancellationToken).ConfigureAwait(false);

                var dataFilePath = MovieDbPersonProvider.GetPersonDataFilePath(_config.ApplicationPaths, id);

                var result = _jsonSerializer.DeserializeFromFile<MovieDbPersonProvider.PersonResult>(dataFilePath);

                var images = result.images ?? new MovieDbPersonProvider.Images();

                var tmdbSettings = await MovieDbProvider.Current.GetTmdbSettings(cancellationToken).ConfigureAwait(false);

                var tmdbImageUrl = tmdbSettings.images.base_url + "original";

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

        public int Priority
        {
            get { return 0; }
        }
    }
}
