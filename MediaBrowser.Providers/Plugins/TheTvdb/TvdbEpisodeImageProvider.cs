using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;
using TvDbSharper;
using TvDbSharper.Dto;

namespace MediaBrowser.Providers.Plugins.TheTvdb
{
    public class TvdbEpisodeImageProvider : IRemoteImageProvider
    {
        private readonly IHttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly TvdbClientManager _tvdbClientManager;

        public TvdbEpisodeImageProvider(IHttpClient httpClient, ILogger<TvdbEpisodeImageProvider> logger, TvdbClientManager tvdbClientManager)
        {
            _httpClient = httpClient;
            _logger = logger;
            _tvdbClientManager = tvdbClientManager;
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
            var language = item.GetPreferredMetadataLanguage();
            if (series != null && TvdbSeriesProvider.IsValidSeries(series.ProviderIds))
            {
                // Process images
                try
                {
                    var episodeInfo = new EpisodeInfo
                    {
                        IndexNumber = episode.IndexNumber.Value,
                        ParentIndexNumber = episode.ParentIndexNumber.Value,
                        SeriesProviderIds = series.ProviderIds,
                        SeriesDisplayOrder = series.DisplayOrder
                    };
                    string episodeTvdbId = await _tvdbClientManager
                        .GetEpisodeTvdbId(episodeInfo, language, cancellationToken).ConfigureAwait(false);
                    if (string.IsNullOrEmpty(episodeTvdbId))
                    {
                        _logger.LogError(
                            "Episode {SeasonNumber}x{EpisodeNumber} not found for series {SeriesTvdbId}",
                            episodeInfo.ParentIndexNumber,
                            episodeInfo.IndexNumber,
                            series.GetProviderId(MetadataProviders.Tvdb));
                        return imageResult;
                    }

                    var episodeResult =
                        await _tvdbClientManager
                            .GetEpisodesAsync(Convert.ToInt32(episodeTvdbId), language, cancellationToken)
                            .ConfigureAwait(false);

                    var image = GetImageInfo(episodeResult.Data);
                    if (image != null)
                    {
                        imageResult.Add(image);
                    }
                }
                catch (TvDbServerException e)
                {
                    _logger.LogError(e, "Failed to retrieve episode images for series {TvDbId}", series.GetProviderId(MetadataProviders.Tvdb));
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
                Url = TvdbUtils.BannerUrl + episode.Filename,
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
