#pragma warning disable CS1591

using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace MediaBrowser.Providers.Movies
{
    public class ImdbPersonExternalId : IExternalId
    {
        /// <inheritdoc />
        public string ProviderName => "IMDb";

        /// <inheritdoc />
        public string Key => MetadataProvider.Imdb.ToString();

        /// <inheritdoc />
        public ExternalIdMediaType? Type => ExternalIdMediaType.Person;

        /// <inheritdoc />
        public string UrlFormatString => "https://www.imdb.com/name/{0}";

        /// <inheritdoc />
        public bool Supports(IHasProviderIds item) => item is Person;
    }
}
