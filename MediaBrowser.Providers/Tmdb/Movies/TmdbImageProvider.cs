using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Providers.Tmdb.Models.General;
using MediaBrowser.Providers.Tmdb.Models.Movies;

namespace MediaBrowser.Providers.Tmdb.Movies
{
    public class TmdbImageProvider : IRemoteImageProvider, IHasOrder
    {
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IHttpClient _httpClient;
        private readonly IFileSystem _fileSystem;

        public TmdbImageProvider(IJsonSerializer jsonSerializer, IHttpClient httpClient, IFileSystem fileSystem)
        {
            _jsonSerializer = jsonSerializer;
            _httpClient = httpClient;
            _fileSystem = fileSystem;
        }

        public string Name => ProviderName;

        public static string ProviderName => TmdbUtils.ProviderName;

        public bool Supports(BaseItem item)
        {
            return item is Movie || item is MusicVideo || item is Trailer;
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

            var language = item.GetPreferredMetadataLanguage();

            var results = await FetchImages(item, null, _jsonSerializer, cancellationToken).ConfigureAwait(false);

            if (results == null)
            {
                return list;
            }

            var tmdbSettings = await TmdbMovieProvider.Current.GetTmdbSettings(cancellationToken).ConfigureAwait(false);

            var tmdbImageUrl = tmdbSettings.images.GetImageUrl("original");

            var supportedImages = GetSupportedImages(item).ToList();

            if (supportedImages.Contains(ImageType.Primary))
            {
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
            }

            if (supportedImages.Contains(ImageType.Backdrop))
            {
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
            }

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
        /// <returns>IEnumerable{MovieDbProvider.Poster}.</returns>
        private IEnumerable<Poster> GetPosters(Images images)
        {
            return images.Posters ?? new List<Poster>();
        }

        /// <summary>
        /// Gets the backdrops.
        /// </summary>
        /// <param name="images">The images.</param>
        /// <returns>IEnumerable{MovieDbProvider.Backdrop}.</returns>
        private IEnumerable<Backdrop> GetBackdrops(Images images)
        {
            var eligibleBackdrops = images.Backdrops == null ? new List<Backdrop>() :
                images.Backdrops;

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
        private async Task<Images> FetchImages(BaseItem item, string language, IJsonSerializer jsonSerializer, CancellationToken cancellationToken)
        {
            var tmdbId = item.GetProviderId(MetadataProviders.Tmdb);

            if (string.IsNullOrWhiteSpace(tmdbId))
            {
                var imdbId = item.GetProviderId(MetadataProviders.Imdb);
                if (!string.IsNullOrWhiteSpace(imdbId))
                {
                    var movieInfo = await TmdbMovieProvider.Current.FetchMainResult(imdbId, false, language, cancellationToken).ConfigureAwait(false);
                    if (movieInfo != null)
                    {
                        tmdbId = movieInfo.Id.ToString(CultureInfo.InvariantCulture);
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(tmdbId))
            {
                return null;
            }

            await TmdbMovieProvider.Current.EnsureMovieInfo(tmdbId, language, cancellationToken).ConfigureAwait(false);

            var path = TmdbMovieProvider.Current.GetDataFilePath(tmdbId, language);

            if (!string.IsNullOrEmpty(path))
            {
                var fileInfo = _fileSystem.GetFileInfo(path);

                if (fileInfo.Exists)
                {
                    return jsonSerializer.DeserializeFromFile<MovieResult>(path).Images;
                }
            }

            return null;
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
