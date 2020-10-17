#pragma warning disable CS1591

using System.Collections.Generic;
using System.Net.Http;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Serialization;

namespace MediaBrowser.Providers.Plugins.Omdb
{
    public class OmdbImageProvider : IRemoteImageProvider, IHasOrder
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IFileSystem _fileSystem;
        private readonly IServerConfigurationManager _configurationManager;
        private readonly IApplicationHost _appHost;

        public OmdbImageProvider(IJsonSerializer jsonSerializer, IApplicationHost appHost, IHttpClientFactory httpClientFactory, IFileSystem fileSystem, IServerConfigurationManager configurationManager)
        {
            _jsonSerializer = jsonSerializer;
            _httpClientFactory = httpClientFactory;
            _fileSystem = fileSystem;
            _configurationManager = configurationManager;
            _appHost = appHost;
        }

        public string Name => "The Open Movie Database";

        // After other internet providers, because they're better
        // But before fallback providers like screengrab
        public int Order => 90;

        public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
        {
            return new List<ImageType>
            {
                ImageType.Primary
            };
        }

        public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
        {
            var imdbId = item.GetProviderId(MetadataProvider.Imdb);

            var list = new List<RemoteImageInfo>();

            var provider = new OmdbProvider(_jsonSerializer, _httpClientFactory, _fileSystem, _appHost, _configurationManager);

            if (!string.IsNullOrWhiteSpace(imdbId))
            {
                var rootObject = await provider.GetRootObject(imdbId, cancellationToken).ConfigureAwait(false);

                if (!string.IsNullOrEmpty(rootObject.Poster))
                {
                    if (item is Episode)
                    {
                        // img.omdbapi.com is returning 404's
                        list.Add(new RemoteImageInfo
                        {
                            ProviderName = Name,
                            Url = rootObject.Poster
                        });
                    }
                    else
                    {
                        list.Add(new RemoteImageInfo
                        {
                            ProviderName = Name,
                            Url = string.Format(CultureInfo.InvariantCulture, "https://img.omdbapi.com/?i={0}&apikey=2c9d9507", imdbId)
                        });
                    }
                }
            }

            return list;
        }

        public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return _httpClientFactory.CreateClient(NamedClient.Default).GetAsync(url, cancellationToken);
        }

        public bool Supports(BaseItem item)
        {
            return item is Movie || item is Trailer || item is Episode;
        }
    }
}
