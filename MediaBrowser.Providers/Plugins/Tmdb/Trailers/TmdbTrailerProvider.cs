#pragma warning disable CS1591

using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Providers;
using MediaBrowser.Providers.Plugins.Tmdb.Movies;

namespace MediaBrowser.Providers.Plugins.Tmdb.Trailers
{
    public class TmdbTrailerProvider : IHasOrder, IRemoteMetadataProvider<Trailer, TrailerInfo>
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public TmdbTrailerProvider(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
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

        public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return _httpClientFactory.CreateClient(NamedClient.Default).GetAsync(url, cancellationToken);
        }
    }
}
