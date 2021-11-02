#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace MediaBrowser.Providers.Plugins.Tmdb.TV
{
    public class TmdbSeriesImageProvider : IRemoteImageProvider, IHasOrder
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly TmdbClientManager _tmdbClientManager;

        public TmdbSeriesImageProvider(IHttpClientFactory httpClientFactory, TmdbClientManager tmdbClientManager)
        {
            _httpClientFactory = httpClientFactory;
            _tmdbClientManager = tmdbClientManager;
        }

        public string Name => TmdbUtils.ProviderName;

        // After tvdb and fanart
        public int Order => 2;

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
            var tmdbId = item.GetProviderId(MetadataProvider.Tmdb);

            if (string.IsNullOrEmpty(tmdbId))
            {
                return Enumerable.Empty<RemoteImageInfo>();
            }

            var language = item.GetPreferredMetadataLanguage();

            // TODO use image languages if All Languages isn't toggled, but there's currently no way to get that value in here
            var series = await _tmdbClientManager
                .GetSeriesAsync(Convert.ToInt32(tmdbId, CultureInfo.InvariantCulture), null, null, cancellationToken)
                .ConfigureAwait(false);

            if (series?.Images == null)
            {
                return Enumerable.Empty<RemoteImageInfo>();
            }

            var posters = series.Images.Posters;
            var backdrops = series.Images.Backdrops;
            var remoteImages = new List<RemoteImageInfo>(posters.Count + backdrops.Count);

            TmdbUtils.ConvertPostersToRemoteImageInfo(posters, _tmdbClientManager, language, remoteImages);
            TmdbUtils.ConvertBackdropsToRemoteImageInfo(backdrops, _tmdbClientManager, language, remoteImages);

            return remoteImages;
        }

        public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return _httpClientFactory.CreateClient(NamedClient.Default).GetAsync(url, cancellationToken);
        }
    }
}
