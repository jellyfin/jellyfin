using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;
using TvDbSharper;
using TvDbSharper.Dto;
using RatingType = MediaBrowser.Model.Dto.RatingType;
using Series = MediaBrowser.Controller.Entities.TV.Series;

namespace MediaBrowser.Providers.Plugins.TheTvdb
{
    public class TvdbSeriesImageProvider : IRemoteImageProvider, IHasOrder
    {
        private readonly IHttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly TvdbClientManager _tvdbClientManager;

        public TvdbSeriesImageProvider(IHttpClient httpClient, ILogger<TvdbSeriesImageProvider> logger, TvdbClientManager tvdbClientManager)
        {
            _httpClient = httpClient;
            _logger = logger;
            _tvdbClientManager = tvdbClientManager;
        }

        public string Name => ProviderName;

        public static string ProviderName => "TheTVDB";

        public bool Supports(BaseItem item)
        {
            return item is Series;
        }

        public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
        {
            return new List<ImageType>
            {
                ImageType.Primary,
                ImageType.Banner,
                ImageType.Backdrop
            };
        }

        public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
        {
            if (!TvdbSeriesProvider.IsValidSeries(item.ProviderIds))
            {
                return Array.Empty<RemoteImageInfo>();
            }

            var language = item.GetPreferredMetadataLanguage();
            var remoteImages = new List<RemoteImageInfo>();
            var keyTypes = new[] { KeyType.Poster, KeyType.Series, KeyType.Fanart };
            var tvdbId = Convert.ToInt32(item.GetProviderId(MetadataProviders.Tvdb));
            foreach (KeyType keyType in keyTypes)
            {
                var imageQuery = new ImagesQuery
                {
                    KeyType = keyType
                };
                try
                {
                    var imageResults =
                        await _tvdbClientManager.GetImagesAsync(tvdbId, imageQuery, language, cancellationToken)
                            .ConfigureAwait(false);

                    remoteImages.AddRange(GetImages(imageResults.Data, language));
                }
                catch (TvDbServerException)
                {
                    _logger.LogDebug("No images of type {KeyType} exist for series {TvDbId}", keyType,
                        tvdbId);
                }
            }
            return remoteImages;
        }

        private IEnumerable<RemoteImageInfo> GetImages(Image[] images, string preferredLanguage)
        {
            var list = new List<RemoteImageInfo>();
            var languages = _tvdbClientManager.GetLanguagesAsync(CancellationToken.None).Result.Data;

            foreach (Image image in images)
            {
                var imageInfo = new RemoteImageInfo
                {
                    RatingType = RatingType.Score,
                    CommunityRating = (double?)image.RatingsInfo.Average,
                    VoteCount = image.RatingsInfo.Count,
                    Url = TvdbUtils.BannerUrl + image.FileName,
                    ProviderName = Name,
                    Language = languages.FirstOrDefault(lang => lang.Id == image.LanguageId)?.Abbreviation,
                    ThumbnailUrl = TvdbUtils.BannerUrl + image.Thumbnail
                };

                var resolution = image.Resolution.Split('x');
                if (resolution.Length == 2)
                {
                    imageInfo.Width = Convert.ToInt32(resolution[0]);
                    imageInfo.Height = Convert.ToInt32(resolution[1]);
                }

                imageInfo.Type = TvdbUtils.GetImageTypeFromKeyType(image.KeyType);
                list.Add(imageInfo);
            }
            var isLanguageEn = string.Equals(preferredLanguage, "en", StringComparison.OrdinalIgnoreCase);

            return list.OrderByDescending(i =>
                {
                    if (string.Equals(preferredLanguage, i.Language, StringComparison.OrdinalIgnoreCase))
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
