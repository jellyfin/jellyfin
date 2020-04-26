using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Providers.Plugins.AudioDb
{
    /// <summary>
    /// TheAudioDb other artist external id.
    /// </summary>
    public class AudioDbOtherArtistExternalId : IExternalId
    {
        /// <inheritdoc />
        public string Name => "TheAudioDb Artist";

        /// <inheritdoc />
        public string Key => MetadataProviders.AudioDbArtist.ToString();

        /// <inheritdoc />
        public string UrlFormatString => "https://www.theaudiodb.com/artist/{0}";

        /// <inheritdoc />
        public bool Supports(IHasProviderIds item) => item is Audio || item is MusicAlbum;
    }
}
