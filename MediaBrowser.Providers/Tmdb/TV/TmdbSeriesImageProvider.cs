using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Providers.Tmdb.Models.General;
using MediaBrowser.Providers.Tmdb.Models.TV;
using MediaBrowser.Providers.Tmdb.Movies;

namespace MediaBrowser.Providers.Tmdb.TV
{
    public class TmdbSeriesImageProvider : IRemoteImageProvider, IHasOrder
    {
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IHttpClient _httpClient;
        private readonly IFileSystem _fileSystem;

        public TmdbSeriesImageProvider(IJsonSerializer jsonSerializer, IHttpClient httpClient, IFileSystem fileSystem)
        {
            _jsonSerializer = jsonSerializer;
            _httpClient = httpClient;
            _fileSystem = fileSystem;
        }

        public string Name => ProviderName;

        public static string ProviderName => TmdbUtils.ProviderName;

        public bool Supports(BaseItem item)
        {
            return item is Series;
        }

        public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
        {
            return new List<ImageType>
            {
                ImageType.Primary,
                ImageType.Backdrop
            };
        }

        public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
        {
            var list = new List<RemoteImageInfo>();

            var results = await FetchImages(item, null, _jsonSerializer, cancellationToken).ConfigureAwait(false);

            if (results == null)
            {
                return list;
            }

            var tmdbSettings = await TmdbMovieProvider.Current.GetTmdbSettings(cancellationToken).ConfigureAwait(false);

            var tmdbImageUrl = tmdbSettings.images.GetImageUrl("original");

            var language = item.GetPreferredMetadataLanguage();

            list.AddRange(GetPosters(results).Select(i => new RemoteImageInfo
            {
                Url = tmdbImageUrl + i.File_Path,
                CommunityRating = i.Vote_Average,
                VoteCount = i.Vote_Count,
                Width = i.Width,
                Height = i.Height,
                Language = TmdbMovieProvider.AdjustImageLanguage(i.Iso_639_1, language),
                ProviderName = Name,
                Type = ImageType.Primary,
                RatingType = RatingType.Score
            }));

            list.AddRange(GetBackdrops(results).Select(i => new RemoteImageInfo
            {
                Url = tmdbImageUrl + i.File_Path,
                CommunityRating = i.Vote_Average,
                VoteCount = i.Vote_Count,
                Width = i.Width,
                Height = i.Height,
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
                .ThenByDescending(i => i.VoteCount ?? 0);
        }

        /// <summary>
        /// Gets the posters.
        /// </summary>
        /// <param name="images">The images.</param>
        private IEnumerable<Poster> GetPosters(Images images)
        {
            return images.Posters ?? new List<Poster>();
        }

        /// <summary>
        /// Gets the backdrops.
        /// </summary>
        /// <param name="images">The images.</param>
        private IEnumerable<Backdrop> GetBackdrops(Images images)
        {
            var eligibleBackdrops = images.Backdrops ?? new List<Backdrop>();

            return eligibleBackdrops.OrderByDescending(i => i.Vote_Average)
                .ThenByDescending(i => i.Vote_Count);
        }

        /// <summary>
        /// Fetches the images.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="language">The language.</param>
        /// <param name="jsonSerializer">The json serializer.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{MovieImages}.</returns>
        private async Task<Images> FetchImages(BaseItem item, string language, IJsonSerializer jsonSerializer,
            CancellationToken cancellationToken)
        {
            var tmdbId = item.GetProviderId(MetadataProviders.Tmdb);

            if (string.IsNullOrEmpty(tmdbId))
            {
                return null;
            }

            await TmdbSeriesProvider.Current.EnsureSeriesInfo(tmdbId, language, cancellationToken).ConfigureAwait(false);

            var path = TmdbSeriesProvider.Current.GetDataFilePath(tmdbId, language);

            if (!string.IsNullOrEmpty(path))
            {
                var fileInfo = _fileSystem.GetFileInfo(path);

                if (fileInfo.Exists)
                {
                    return jsonSerializer.DeserializeFromFile<SeriesResult>(path).Images;
                }
            }

            return null;
        }
        // After tvdb and fanart
        public int Order => 2;

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
