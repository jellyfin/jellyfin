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
using Microsoft.Extensions.Logging;
using TvDbSharper;
using TvDbSharper.Dto;

namespace MediaBrowser.Providers.TV.TheTVDB
{
    public class TvdbEpisodeImageProvider : IRemoteImageProvider
    {
        private readonly IHttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly TvDbClientManager _tvDbClientManager;

        public TvdbEpisodeImageProvider(IHttpClient httpClient, ILogger logger)
        {
            _httpClient = httpClient;
            _logger = logger;
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
            var imageResult = new List<RemoteImageInfo>();

            if (series != null && TvdbSeriesProvider.IsValidSeries(series.ProviderIds))
            {
                var episodeTvdbId = episode.GetProviderId(MetadataProviders.Tvdb);

                // Process images
                try
                {
                    if (string.IsNullOrEmpty(episodeTvdbId))
                    {
                        var episodeNumber = episode.IndexNumber.Value;
                        var seasonNumber = episode.ParentIndexNumber.Value;
                        episodeTvdbId = await _tvDbClientManager.GetEpisodeTvdbId(
                            Convert.ToInt32(series.GetProviderId(MetadataProviders.Tvdb)), episodeNumber, seasonNumber,
                            cancellationToken);
                        if (string.IsNullOrEmpty(episodeTvdbId))
                        {
                            _logger.LogError("Episode {SeasonNumber}x{EpisodeNumber} not found for series {SeriesTvdbId}",
                                seasonNumber, episodeNumber);
                            return imageResult;
                        }
                    }

                    var episodeResult =
                        await _tvDbClientManager.GetEpisodesAsync(Convert.ToInt32(episodeTvdbId), cancellationToken);

                    var image = GetImageInfo(episodeResult.Data);
                    if (image != null)
                    {
                        imageResult.Add(image);
                    }
                }
                catch (TvDbServerException e)
                {
                    _logger.LogError(e, "Failed to retrieve episode images for {TvDbId}", episodeTvdbId);
                }
            }

            return imageResult;
        }

        private RemoteImageInfo GetImageInfo(EpisodeRecord episode)
        {
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
