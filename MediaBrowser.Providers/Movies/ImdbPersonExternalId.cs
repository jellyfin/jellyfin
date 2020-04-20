using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Providers.Movies
{
    public class ImdbPersonExternalId : IExternalId
    {
        /// <inheritdoc />
        public string Name => "IMDb";

        /// <inheritdoc />
        public string Key => MetadataProviders.Imdb.ToString();

        /// <inheritdoc />
        public string UrlFormatString => "https://www.imdb.com/name/{0}";

        /// <inheritdoc />
        public bool Supports(IHasProviderIds item) => item is Person;
    }
}
