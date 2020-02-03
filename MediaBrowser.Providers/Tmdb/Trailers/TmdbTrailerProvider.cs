using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Providers;
using MediaBrowser.Providers.Tmdb.Movies;

namespace MediaBrowser.Providers.Tmdb.Trailers
{
    public class TmdbTrailerProvider : IHasOrder, IRemoteMetadataProvider<Trailer, TrailerInfo>
    {
        private readonly IHttpClient _httpClient;

        public TmdbTrailerProvider(IHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public Task<IEnumerable<RemoteSearchResult>> GetSearchResults(TrailerInfo searchInfo, CancellationToken cancellationToken)
        {
            return TmdbMovieProvider.Current.GetMovieSearchResults(searchInfo, cancellationToken);
        }

        public Task<MetadataResult<Trailer>> GetMetadata(TrailerInfo info, CancellationToken cancellationToken)
        {
            return TmdbMovieProvider.Current.GetItemMetadata<Trailer>(info, cancellationToken);
        }

        public string Name => TmdbMovieProvider.Current.Name;

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
