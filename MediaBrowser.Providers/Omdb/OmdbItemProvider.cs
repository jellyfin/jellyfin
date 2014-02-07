using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.Omdb
{
    public class OmdbItemProvider : ICustomMetadataProvider<Series>, 
        ICustomMetadataProvider<Movie>, ICustomMetadataProvider<Trailer>
    {
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IHttpClient _httpClient;

        public OmdbItemProvider(IJsonSerializer jsonSerializer, IHttpClient httpClient)
        {
            _jsonSerializer = jsonSerializer;
            _httpClient = httpClient;
        }

        public string Name
        {
            get { return "OMDb"; }
        }

        public Task<ItemUpdateType> FetchAsync(Series item, CancellationToken cancellationToken)
        {
            return new OmdbProvider(_jsonSerializer, _httpClient).Fetch(item, cancellationToken);
        }

        public Task<ItemUpdateType> FetchAsync(Movie item, CancellationToken cancellationToken)
        {
            return new OmdbProvider(_jsonSerializer, _httpClient).Fetch(item, cancellationToken);
        }

        private readonly Task<ItemUpdateType> _cachedTask = Task.FromResult(ItemUpdateType.Unspecified);
        public Task<ItemUpdateType> FetchAsync(Trailer item, CancellationToken cancellationToken)
        {
            if (item.IsLocalTrailer)
            {
                return _cachedTask;
            }

            return new OmdbProvider(_jsonSerializer, _httpClient).Fetch(item, cancellationToken);
        }
    }
}
