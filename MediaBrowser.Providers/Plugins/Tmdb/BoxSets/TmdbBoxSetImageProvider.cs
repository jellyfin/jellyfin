#nullable disable

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
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace MediaBrowser.Providers.Plugins.Tmdb.BoxSets
{
    public class TmdbBoxSetImageProvider : IRemoteImageProvider, IHasOrder
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly TmdbClientManager _tmdbClientManager;

        public TmdbBoxSetImageProvider(IHttpClientFactory httpClientFactory, TmdbClientManager tmdbClientManager)
        {
            _httpClientFactory = httpClientFactory;
            _tmdbClientManager = tmdbClientManager;
        }

        public string Name => TmdbUtils.ProviderName;

        public int Order => 0;

        public bool Supports(BaseItem item)
        {
            return item is BoxSet;
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
            var tmdbId = Convert.ToInt32(item.GetProviderId(MetadataProvider.Tmdb), CultureInfo.InvariantCulture);

            if (tmdbId <= 0)
            {
                return Enumerable.Empty<RemoteImageInfo>();
            }

            var language = item.GetPreferredMetadataLanguage();

            // TODO use image languages if All Languages isn't toggled, but there's currently no way to get that value in here
            var collection = await _tmdbClientManager.GetCollectionAsync(tmdbId, null, null, cancellationToken).ConfigureAwait(false);

            if (collection?.Images == null)
            {
                return Enumerable.Empty<RemoteImageInfo>();
            }

            var posters = collection.Images.Posters;
            var backdrops = collection.Images.Backdrops;
            var remoteImages = new List<RemoteImageInfo>(posters.Count + backdrops.Count);

            _tmdbClientManager.ConvertPostersToRemoteImageInfo(posters, language, remoteImages);
            _tmdbClientManager.ConvertBackdropsToRemoteImageInfo(backdrops, language, remoteImages);

            return remoteImages;
        }

        public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return _httpClientFactory.CreateClient(NamedClient.Default).GetAsync(url, cancellationToken);
        }
    }
}
