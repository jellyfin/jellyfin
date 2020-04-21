using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Providers.Plugins.MusicBrainz;

namespace MediaBrowser.Providers.Music
{

    /// <summary>
    /// MusicBrainz other artist external id.
    /// </summary>
    public class MusicBrainzOtherArtistExternalId : IExternalId
    {
        /// <inheritdoc />
        public string Name => "MusicBrainz Artist";

        /// <inheritdoc />

        public string Key => MetadataProviders.MusicBrainzArtist.ToString();

        /// <inheritdoc />
        public string UrlFormatString => Plugin.Instance.Configuration.Server + "/artist/{0}";

        /// <inheritdoc />
        public bool Supports(IHasProviderIds item) => item is Audio || item is MusicAlbum;
    }
}
