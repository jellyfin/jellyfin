using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Providers.Movies;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.TV
{
    public class MovieDbSeriesImageProvider : IRemoteImageProvider, IHasOrder
    {
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IHttpClient _httpClient;

        public MovieDbSeriesImageProvider(IJsonSerializer jsonSerializer, IHttpClient httpClient)
        {
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
            return item is Series;
        }

        public IEnumerable<ImageType> GetSupportedImages(IHasImages item)
        {
            return new List<ImageType>
            {
                ImageType.Primary, 
                ImageType.Backdrop
            };
        }

        public async Task<IEnumerable<RemoteImageInfo>> GetImages(IHasImages item, CancellationToken cancellationToken)
        {
            var list = new List<RemoteImageInfo>();

            var results = await FetchImages((BaseItem)item, null, _jsonSerializer, cancellationToken).ConfigureAwait(false);

            if (results == null)
            {
                return list;
            }

            var tmdbSettings = await MovieDbProvider.Current.GetTmdbSettings(cancellationToken).ConfigureAwait(false);

            var tmdbImageUrl = tmdbSettings.images.secure_base_url + "original";

            var language = item.GetPreferredMetadataLanguage();

            list.AddRange(GetPosters(results).Select(i => new RemoteImageInfo
            {
                Url = tmdbImageUrl + i.file_path,
                CommunityRating = i.vote_average,
                VoteCount = i.vote_count,
                Width = i.width,
                Height = i.height,
                Language = MovieDbProvider.AdjustImageLanguage(i.iso_639_1, language),
                ProviderName = Name,
                Type = ImageType.Primary,
                RatingType = RatingType.Score
            }));

            list.AddRange(GetBackdrops(results).Select(i => new RemoteImageInfo
            {
                Url = tmdbImageUrl + i.file_path,
                CommunityRating = i.vote_average,
                VoteCount = i.vote_count,
                Width = i.width,
                Height = i.height,
                ProviderName = Name,
                Type = ImageType.Backdrop,
                RatingType = RatingType.Score
            }));

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

        /// <summary>
        /// Gets the posters.
        /// </summary>
        /// <param name="images">The images.</param>
        private IEnumerable<MovieDbSeriesProvider.Poster> GetPosters(MovieDbSeriesProvider.Images images)
        {
            return images.posters ?? new List<MovieDbSeriesProvider.Poster>();
        }

        /// <summary>
        /// Gets the backdrops.
        /// </summary>
        /// <param name="images">The images.</param>
        private IEnumerable<MovieDbSeriesProvider.Backdrop> GetBackdrops(MovieDbSeriesProvider.Images images)
        {
            var eligibleBackdrops = images.backdrops == null ? new List<MovieDbSeriesProvider.Backdrop>() :
                images.backdrops
                .ToList();

            return eligibleBackdrops.OrderByDescending(i => i.vote_average)
                .ThenByDescending(i => i.vote_count);
        }

        /// <summary>
        /// Fetches the images.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="language">The language.</param>
        /// <param name="jsonSerializer">The json serializer.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{MovieImages}.</returns>
        private async Task<MovieDbSeriesProvider.Images> FetchImages(BaseItem item, string language, IJsonSerializer jsonSerializer,
            CancellationToken cancellationToken)
        {
            var tmdbId = item.GetProviderId(MetadataProviders.Tmdb);

            if (string.IsNullOrEmpty(tmdbId))
            {
                return null;
            }

            await MovieDbSeriesProvider.Current.EnsureSeriesInfo(tmdbId, language, cancellationToken).ConfigureAwait(false);

            var path = MovieDbSeriesProvider.Current.GetDataFilePath(tmdbId, language);

            if (!string.IsNullOrEmpty(path))
            {
                var fileInfo = new FileInfo(path);

                if (fileInfo.Exists)
                {
                    return jsonSerializer.DeserializeFromFile<MovieDbSeriesProvider.RootObject>(path).images;
                }
            }

            return null;
        }

        public int Order
        {
            get
            {
                // After tvdb and fanart
                return 2;
            }
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
