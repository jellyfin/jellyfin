using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using MediaBrowser.Providers.Genres;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.FolderImages
{
    public class DefaultImageProvider : IRemoteImageProvider, IHasItemChangeMonitor
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
                return urlPrefix + "books.png";
            }
            if (string.Equals(viewType, CollectionType.Games, StringComparison.OrdinalIgnoreCase))
            {
                return urlPrefix + "games.png";
            }
            if (string.Equals(viewType, CollectionType.Music, StringComparison.OrdinalIgnoreCase))
            {
                return urlPrefix + "music.png";
            }
            if (string.Equals(viewType, CollectionType.Photos, StringComparison.OrdinalIgnoreCase))
            {
                return urlPrefix + "photos.png";
            }
            if (string.Equals(viewType, CollectionType.TvShows, StringComparison.OrdinalIgnoreCase))
            {
                return urlPrefix + "tv.png";
            }
            if (string.Equals(viewType, CollectionType.Channels, StringComparison.OrdinalIgnoreCase))
            {
                return urlPrefix + "channels.png";
            }
            if (string.Equals(viewType, CollectionType.LiveTv, StringComparison.OrdinalIgnoreCase))
            {
                return urlPrefix + "livetv.png";
            }
            if (string.Equals(viewType, CollectionType.Movies, StringComparison.OrdinalIgnoreCase))
            {
                return urlPrefix + "movies.png";
            }

            return urlPrefix + "generic.png";
        }

        public string Name
        {
            get { return "Default Image Provider"; }
        }

        public bool Supports(IHasImages item)
        {
            var view = item as UserView;

            if (view != null)
            {
                return !view.UserId.HasValue;
            }
            
            return item is ICollectionFolder;
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

        public bool HasChanged(IHasMetadata item, MetadataStatus status, IDirectoryService directoryService)
        {
            return GetSupportedImages(item).Any(i => !item.HasImage(i));
        }
    }
}
