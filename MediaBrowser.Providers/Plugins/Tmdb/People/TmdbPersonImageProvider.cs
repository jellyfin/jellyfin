#pragma warning disable CS1591

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace MediaBrowser.Providers.Plugins.Tmdb.People
{
    public class TmdbPersonImageProvider : IRemoteImageProvider, IHasOrder
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly TmdbClientManager _tmdbClientManager;

        public TmdbPersonImageProvider(IHttpClientFactory httpClientFactory, TmdbClientManager tmdbClientManager)
        {
            _httpClientFactory = httpClientFactory;
            _tmdbClientManager = tmdbClientManager;
        }

        /// <inheritdoc />
        public string Name => TmdbUtils.ProviderName;

        /// <inheritdoc />
        public int Order => 0;

        public bool Supports(BaseItem item)
        {
            return item is Person;
        }

        public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
        {
            return new List<ImageType>
            {
                ImageType.Primary
            };
        }

        public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
        {
            var person = (Person)item;

            if (!person.TryGetProviderId(MetadataProvider.Tmdb, out var personTmdbId))
            {
                return Enumerable.Empty<RemoteImageInfo>();
            }

            var language = item.GetPreferredMetadataLanguage();
            var personResult = await _tmdbClientManager.GetPersonAsync(int.Parse(personTmdbId, CultureInfo.InvariantCulture), language, cancellationToken).ConfigureAwait(false);
            if (personResult?.Images?.Profiles == null)
            {
                return Enumerable.Empty<RemoteImageInfo>();
            }

            var profiles = personResult.Images.Profiles;
            var remoteImages = new List<RemoteImageInfo>(profiles.Count);

            _tmdbClientManager.ConvertProfilesToRemoteImageInfo(profiles, language, remoteImages);

            return remoteImages;
        }

        public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return _httpClientFactory.CreateClient(NamedClient.Default).GetAsync(url, cancellationToken);
        }
    }
}
