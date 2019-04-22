using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Common.Net;
using Jellyfin.Controller.Entities;
using Jellyfin.Controller.Providers;
using Jellyfin.Model.Providers;

namespace Jellyfin.Providers.Movies
{
    public class MovieDbTrailerProvider : IHasOrder, IRemoteMetadataProvider<Trailer, TrailerInfo>
    {
        private readonly IHttpClient _httpClient;

        public MovieDbTrailerProvider(IHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public Task<IEnumerable<RemoteSearchResult>> GetSearchResults(TrailerInfo searchInfo, CancellationToken cancellationToken)
        {
            return MovieDbProvider.Current.GetMovieSearchResults(searchInfo, cancellationToken);
        }

        public Task<MetadataResult<Trailer>> GetMetadata(TrailerInfo info, CancellationToken cancellationToken)
        {
            return MovieDbProvider.Current.GetItemMetadata<Trailer>(info, cancellationToken);
        }

        public string Name => MovieDbProvider.Current.Name;

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
