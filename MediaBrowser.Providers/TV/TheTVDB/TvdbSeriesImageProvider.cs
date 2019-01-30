using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using TvDbSharper;
using TvDbSharper.Dto;
using RatingType = MediaBrowser.Model.Dto.RatingType;
using Series = MediaBrowser.Controller.Entities.TV.Series;

namespace MediaBrowser.Providers.TV.TheTVDB
{
    public class TvdbSeriesImageProvider : IRemoteImageProvider, IHasOrder
    {
        private readonly IHttpClient _httpClient;
        private readonly CultureInfo _usCulture = new CultureInfo("en-US");
        private readonly TvDbClient _tvDbClient = new TvDbClient();

        public TvdbSeriesImageProvider(IHttpClient httpClient)
        {
            _httpClient = httpClient;
            _tvDbClient.Authentication.AuthenticateAsync(TVUtils.TvdbApiKey);
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
                return new RemoteImageInfo[] { };
            }

            var language = item.GetPreferredMetadataLanguage();
            _tvDbClient.AcceptedLanguage = language;
            var remoteImages = new List<RemoteImageInfo>();
            var keyTypes = new[] {KeyType.Poster, KeyType.Series, KeyType.Fanart};
            // TODO error handling
            foreach (KeyType keyType in keyTypes)
            {
                var imageQuery = new ImagesQuery
                {
                    KeyType = keyType
                };
                var imageResults =
                    await _tvDbClient.Series.GetImagesAsync(Convert.ToInt32(item.GetProviderId(MetadataProviders.Tvdb)), imageQuery, cancellationToken);

                remoteImages.AddRange(GetImages(imageResults.Data, language));
            }
            return remoteImages;
        }

        private IEnumerable<RemoteImageInfo> GetImages(Image[] images, string preferredLanguage)
        {
            var list = new List<RemoteImageInfo>();

            foreach (Image image in images)
            {
                var resolution = image.Resolution.Split('x');
                var imageInfo = new RemoteImageInfo
                {
                    RatingType = RatingType.Score,
                    CommunityRating = (double?)image.RatingsInfo.Average,
                    VoteCount = image.RatingsInfo.Count,
                    Url = TVUtils.BannerUrl + image.FileName,
                    ProviderName = Name,
                    // TODO Language = image.LanguageId,
                    Width = Convert.ToInt32(resolution[0]),
                    Height = Convert.ToInt32(resolution[1]),
                    ThumbnailUrl = TVUtils.BannerUrl + image.Thumbnail
                };


                if (string.Equals(image.KeyType, "poster", StringComparison.OrdinalIgnoreCase))
                {
                    imageInfo.Type = ImageType.Primary;
                }
                else if (string.Equals(image.KeyType, "series", StringComparison.OrdinalIgnoreCase))
                {
                    imageInfo.Type = ImageType.Banner;
                }
                else if (string.Equals(image.KeyType, "fanart", StringComparison.OrdinalIgnoreCase))
                {
                    imageInfo.Type = ImageType.Backdrop;
                }

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
