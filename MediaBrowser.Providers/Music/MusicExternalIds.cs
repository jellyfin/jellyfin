using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace MediaBrowser.Providers.Music
{
    public class ImvdbId : IExternalId
    {
        /// <inheritdoc />
        public string Name => "IMVDb";

        /// <inheritdoc />
        public string Key => "IMVDb";

        /// <inheritdoc />
        public ExternalIdMediaType Type => ExternalIdMediaType.General;

        /// <inheritdoc />
        public string UrlFormatString => null;

        /// <inheritdoc />
        public bool Supports(IHasProviderIds item)
            => item is MusicVideo;
    }
}
