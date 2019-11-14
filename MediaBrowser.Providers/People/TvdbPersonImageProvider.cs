using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using MediaBrowser.Providers.TV.TheTVDB;
using Microsoft.Extensions.Logging;
using TvDbSharper;

namespace MediaBrowser.Providers.People
{
    public class TvdbPersonImageProvider : IRemoteImageProvider, IHasOrder
    {
        private readonly IHttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly ILibraryManager _libraryManager;
        private readonly TvDbClientManager _tvDbClientManager;

        public TvdbPersonImageProvider(ILibraryManager libraryManager, IHttpClient httpClient, ILogger<TvdbPersonImageProvider> logger, TvDbClientManager tvDbClientManager)
        {
            _libraryManager = libraryManager;
            _httpClient = httpClient;
            _logger = logger;
            _tvDbClientManager = tvDbClientManager;
        }

        /// <inheritdoc />
        public string Name => "TheTVDB";

        /// <inheritdoc />
        public int Order => 1;

        /// <inheritdoc />
        public bool Supports(BaseItem item) => item is Person;

        /// <inheritdoc />
        public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
        {
            yield return ImageType.Primary;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
        {
            var seriesWithPerson = _libraryManager.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = new[] { typeof(Series).Name },
                PersonIds = new[] { item.Id },
                DtoOptions = new DtoOptions(false)
                {
                    EnableImages = false
                }

            }).Cast<Series>()
                .Where(i => TvdbSeriesProvider.IsValidSeries(i.ProviderIds))
                .ToList();

            var infos = (await Task.WhenAll(seriesWithPerson.Select(async i =>
                        await GetImageFromSeriesData(i, item.Name, cancellationToken).ConfigureAwait(false)))
                    .ConfigureAwait(false))
                .Where(i => i != null)
                .Take(1);

            return infos;
        }

        private async Task<RemoteImageInfo> GetImageFromSeriesData(Series series, string personName, CancellationToken cancellationToken)
        {
            var tvdbId = Convert.ToInt32(series.GetProviderId(MetadataProviders.Tvdb));

            try
            {
                var actorsResult = await _tvDbClientManager
                    .GetActorsAsync(tvdbId, series.GetPreferredMetadataLanguage(), cancellationToken)
                    .ConfigureAwait(false);
                var actor = actorsResult.Data.FirstOrDefault(a =>
                    string.Equals(a.Name, personName, StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrEmpty(a.Image));
                if (actor == null)
                {
                    return null;
                }

                return new RemoteImageInfo
                {
                    Url = TvdbUtils.BannerUrl + actor.Image,
                    Type = ImageType.Primary,
                    ProviderName = Name
                };
            }
            catch (TvDbServerException e)
            {
                _logger.LogError(e, "Failed to retrieve actor {ActorName} from series {SeriesTvdbId}", personName, tvdbId);
                return null;
            }
        }

        /// <inheritdoc />
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
