using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using TvDbSharper;
using TvDbSharper.Dto;

namespace MediaBrowser.Providers.TV.TheTVDB
{
    public class TvdbEpisodeImageProvider : IRemoteImageProvider
    {
        private readonly CultureInfo _usCulture = new CultureInfo("en-US");
        private readonly IHttpClient _httpClient;
        private readonly TvDbClientManager _tvDbClientManager;

        public TvdbEpisodeImageProvider(IHttpClient httpClient)
        {
            _httpClient = httpClient;
            _tvDbClientManager = TvDbClientManager.Instance;
        }

        public string Name => "TheTVDB";

        public bool Supports(BaseItem item)
        {
            return item is Episode;
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
            var episode = (Episode)item;
            var series = episode.Series;

            if (series != null && TvdbSeriesProvider.IsValidSeries(series.ProviderIds))
            {
                var tvdbId = episode.GetProviderId(MetadataProviders.Tvdb);
                // Process images
                var episodeResult = await _tvDbClientManager.TvDbClient.Episodes.GetAsync(Convert.ToInt32(tvdbId), cancellationToken);

                var image = GetImageInfo(episodeResult.Data);
                return new List<RemoteImageInfo>
                {
                    image
                };
            }

            return new RemoteImageInfo[] { };
        }

        private RemoteImageInfo GetImageInfo(EpisodeRecord episode)
        {
            var height = 225;
            var width = 400;
            var url = string.Empty;

            if (string.IsNullOrEmpty(episode.Filename))
            {
                return null;
            }

            return new RemoteImageInfo
            {
                Width = Convert.ToInt32(episode.ThumbWidth),
                Height = Convert.ToInt32(episode.ThumbHeight),
                ProviderName = Name,
                Url = TVUtils.BannerUrl + episode.Filename,
                Type = ImageType.Primary
            };
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
