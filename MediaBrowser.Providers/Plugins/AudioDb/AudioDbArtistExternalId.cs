using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Providers.Plugins.AudioDb
{
    public class AudioDbArtistExternalId : IExternalId
    {
        /// <inheritdoc />
        public string Name => "TheAudioDb";

        /// <inheritdoc />
        public string Key => MetadataProviders.AudioDbArtist.ToString();

        /// <inheritdoc />
        public string UrlFormatString => "https://www.theaudiodb.com/artist/{0}";

        /// <inheritdoc />
        public bool Supports(IHasProviderIds item) => item is MusicArtist;
    }
}
