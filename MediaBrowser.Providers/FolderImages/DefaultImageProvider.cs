using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using MediaBrowser.Providers.Genres;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.FolderImages
{
    public class DefaultImageProvider : IRemoteImageProvider
    {
        private readonly IHttpClient _httpClient;

        public DefaultImageProvider(IHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public IEnumerable<ImageType> GetSupportedImages(IHasImages item)
        {
            return new List<ImageType>
            {
                ImageType.Primary,
                ImageType.Thumb
            };
        }

        public Task<IEnumerable<RemoteImageInfo>> GetImages(IHasImages item, CancellationToken cancellationToken)
        {
            var view = item as UserView;

            if (view != null)
            {
                return GetImages(view.ViewType, cancellationToken);
            }

            var folder = (ICollectionFolder)item;
            return GetImages(folder.CollectionType, cancellationToken);
        }

        private Task<IEnumerable<RemoteImageInfo>> GetImages(string viewType, CancellationToken cancellationToken)
        {
            var url = GetImageUrl(viewType);

            return Task.FromResult<IEnumerable<RemoteImageInfo>>(new List<RemoteImageInfo>
            {
                 new RemoteImageInfo
                 {
                      ProviderName = Name,
                      Url = url,
                      Type = ImageType.Primary
                 },

                 new RemoteImageInfo
                 {
                      ProviderName = Name,
                      Url = url,
                      Type = ImageType.Thumb
                 }
            });
        }

        private string GetImageUrl(string viewType)
        {
            const string urlPrefix = "https://raw.githubusercontent.com/MediaBrowser/MediaBrowser.Resources/master/images/folders/";

            if (string.Equals(viewType, CollectionType.Books, StringComparison.OrdinalIgnoreCase))
            {
                return urlPrefix + "books.jpg";
            }
            if (string.Equals(viewType, CollectionType.Games, StringComparison.OrdinalIgnoreCase))
            {
                return urlPrefix + "games.jpg";
            }
            if (string.Equals(viewType, CollectionType.Music, StringComparison.OrdinalIgnoreCase))
            {
                return urlPrefix + "music.jpg";
            }
            if (string.Equals(viewType, CollectionType.Photos, StringComparison.OrdinalIgnoreCase))
            {
                return urlPrefix + "photos.jpg";
            }
            if (string.Equals(viewType, CollectionType.TvShows, StringComparison.OrdinalIgnoreCase))
            {
                return urlPrefix + "tv.jpg";
            }
            if (string.Equals(viewType, CollectionType.Channels, StringComparison.OrdinalIgnoreCase))
            {
                return urlPrefix + "channels.jpg";
            }
            if (string.Equals(viewType, CollectionType.LiveTv, StringComparison.OrdinalIgnoreCase))
            {
                return urlPrefix + "livetv.jpg";
            }
            if (string.Equals(viewType, CollectionType.Movies, StringComparison.OrdinalIgnoreCase))
            {
                return urlPrefix + "movies.jpg";
            }

            return urlPrefix + "generic.jpg";
        }

        public string Name
        {
            get { return "Default Image Provider"; }
        }

        public bool Supports(IHasImages item)
        {
            return item is UserView || item is ICollectionFolder;
        }

        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return _httpClient.GetResponse(new HttpRequestOptions
            {
                CancellationToken = cancellationToken,
                Url = url,
                ResourcePool = GenreImageProvider.ImageDownloadResourcePool
            });
        }
    }
}
