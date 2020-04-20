using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Providers.Plugins.MusicBrainz;

namespace MediaBrowser.Providers.Music
{
    public class MusicBrainzAlbumArtistExternalId : IExternalId
    {
        /// <inheritdoc />
        public string Name => "MusicBrainz Album Artist";

        /// <inheritdoc />
        public string Key => MetadataProviders.MusicBrainzAlbumArtist.ToString();

        /// <inheritdoc />
        public string UrlFormatString => Plugin.Instance.Configuration.Server + "/artist/{0}";

        /// <inheritdoc />
        public bool Supports(IHasProviderIds item) => item is Audio;
    }
}
