#pragma warning disable CS1591

using System;
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
using MediaBrowser.Model.Extensions;
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
            var personTmdbId = Convert.ToInt32(person.GetProviderId(MetadataProvider.Tmdb), CultureInfo.InvariantCulture);

            if (personTmdbId > 0)
            {
                var personResult = await _tmdbClientManager.GetPersonAsync(personTmdbId, cancellationToken).ConfigureAwait(false);
                if (personResult?.Images?.Profiles == null)
                {
                    return Enumerable.Empty<RemoteImageInfo>();
                }

                var remoteImages = new List<RemoteImageInfo>();
                var language = item.GetPreferredMetadataLanguage();

                for (var i = 0; i < personResult.Images.Profiles.Count; i++)
                {
                    var image = personResult.Images.Profiles[i];
                    remoteImages.Add(new RemoteImageInfo
                    {
                        ProviderName = Name,
                        Type = ImageType.Primary,
                        Width = image.Width,
                        Height = image.Height,
                        Language = TmdbUtils.AdjustImageLanguage(image.Iso_639_1, language),
                        Url = _tmdbClientManager.GetProfileUrl(image.FilePath)
                    });
                }

                return remoteImages.OrderByLanguageDescending(language);
            }

            return Enumerable.Empty<RemoteImageInfo>();
        }

        public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return _httpClientFactory.CreateClient(NamedClient.Default).GetAsync(url, cancellationToken);
        }
    }
}
