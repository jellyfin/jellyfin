using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.Omdb
{
    public class OmdbSeriesProvider : ICustomMetadataProvider<Series>
    {
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IHttpClient _httpClient;

        public OmdbSeriesProvider(IJsonSerializer jsonSerializer, IHttpClient httpClient)
        {
            _jsonSerializer = jsonSerializer;
            _httpClient = httpClient;
        }

        public Task FetchAsync(Series item, CancellationToken cancellationToken)
        {
            return new OmdbProvider(_jsonSerializer, _httpClient).Fetch(item, cancellationToken);
        }

        public string Name
        {
            get { return "OMDb"; }
        }
    }
}
