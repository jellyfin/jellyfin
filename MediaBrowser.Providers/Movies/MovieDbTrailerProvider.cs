using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Providers;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.Movies
{
    public class MovieDbTrailerProvider : IRemoteMetadataProvider<Trailer, TrailerInfo>, IHasOrder, IRemoteMetadataProvider<ChannelVideoItem, ChannelItemLookupInfo>
    {
        private readonly IHttpClient _httpClient;

        public MovieDbTrailerProvider(IHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public Task<MetadataResult<Trailer>> GetMetadata(TrailerInfo info, CancellationToken cancellationToken)
        {
            return MovieDbProvider.Current.GetItemMetadata<Trailer>(info, cancellationToken);
        }

        public Task<IEnumerable<RemoteSearchResult>> GetSearchResults(TrailerInfo searchInfo, CancellationToken cancellationToken)
        {
            return MovieDbProvider.Current.GetMovieSearchResults(searchInfo, cancellationToken);
        }

        public Task<MetadataResult<ChannelVideoItem>> GetMetadata(ChannelItemLookupInfo info, CancellationToken cancellationToken)
        {
            if (info.ContentType != Model.Channels.ChannelMediaContentType.Trailer)
            {
                return Task.FromResult(new MetadataResult<ChannelVideoItem>());
            }

            return MovieDbProvider.Current.GetItemMetadata<ChannelVideoItem>(info, cancellationToken);
        }

        public Task<IEnumerable<RemoteSearchResult>> GetSearchResults(ChannelItemLookupInfo searchInfo, CancellationToken cancellationToken)
        {
            if (searchInfo.ContentType != ChannelMediaContentType.Trailer)
            {
                return Task.FromResult<IEnumerable<RemoteSearchResult>>(new List<RemoteSearchResult>());
            }
            
            return MovieDbProvider.Current.GetMovieSearchResults(searchInfo, cancellationToken);
        }

        public string Name
        {
            get { return MovieDbProvider.Current.Name; }
        }

        public bool HasChanged(IHasMetadata item, DateTime date)
        {
            return MovieDbProvider.Current.HasChanged(item, date);
        }

        public int Order
        {
            get
            {
                // After Omdb
                return 1;
            }
        }

        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return _httpClient.GetResponse(new HttpRequestOptions
            {
                CancellationToken = cancellationToken,
                Url = url,
                ResourcePool = MovieDbProvider.Current.MovieDbResourcePool
            });
        }
    }
}
