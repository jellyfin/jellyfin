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
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Extensions;
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

            var remoteImages = new List<RemoteImageInfo>();

            for (var i = 0; i < collection.Images.Posters.Count; i++)
            {
                var poster = collection.Images.Posters[i];
                remoteImages.Add(new RemoteImageInfo
                {
                    Url = _tmdbClientManager.GetPosterUrl(poster.FilePath),
                    CommunityRating = poster.VoteAverage,
                    VoteCount = poster.VoteCount,
                    Width = poster.Width,
                    Height = poster.Height,
                    Language = TmdbUtils.AdjustImageLanguage(poster.Iso_639_1, language),
                    ProviderName = Name,
                    Type = ImageType.Primary,
                    RatingType = RatingType.Score
                });
            }

            for (var i = 0; i < collection.Images.Backdrops.Count; i++)
            {
                var backdrop = collection.Images.Backdrops[i];
                remoteImages.Add(new RemoteImageInfo
                {
                    Url = _tmdbClientManager.GetBackdropUrl(backdrop.FilePath),
                    CommunityRating = backdrop.VoteAverage,
                    VoteCount = backdrop.VoteCount,
                    Width = backdrop.Width,
                    Height = backdrop.Height,
                    ProviderName = Name,
                    Type = ImageType.Backdrop,
                    RatingType = RatingType.Score
                });
            }

            return remoteImages.OrderByLanguageDescending(language);
        }

        public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return _httpClientFactory.CreateClient(NamedClient.Default).GetAsync(url, cancellationToken);
        }
    }
}
