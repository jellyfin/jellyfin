using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace MediaBrowser.Providers.Plugins.AudioDb
{
    public class AudioDbAlbumExternalId : IExternalId
    {
        /// <inheritdoc />
        public string ProviderName => "TheAudioDb";

        /// <inheritdoc />
        public string Key => MetadataProviders.AudioDbAlbum.ToString();

        /// <inheritdoc />
        public ExternalIdMediaType Type => ExternalIdMediaType.General;

        /// <inheritdoc />
        public string UrlFormatString => "https://www.theaudiodb.com/album/{0}";

        /// <inheritdoc />
        public bool Supports(IHasProviderIds item) => item is MusicAlbum;
    }

    public class AudioDbOtherAlbumExternalId : IExternalId
    {
        /// <inheritdoc />
        public string ProviderName => "TheAudioDb";

        /// <inheritdoc />
        public string Key => MetadataProviders.AudioDbAlbum.ToString();

        /// <inheritdoc />
        public ExternalIdMediaType Type => ExternalIdMediaType.Album;

        /// <inheritdoc />
        public string UrlFormatString => "https://www.theaudiodb.com/album/{0}";

        /// <inheritdoc />
        public bool Supports(IHasProviderIds item) => item is Audio;
    }

    public class AudioDbArtistExternalId : IExternalId
    {
        /// <inheritdoc />
        public string ProviderName => "TheAudioDb";

        /// <inheritdoc />
        public string Key => MetadataProviders.AudioDbArtist.ToString();

        /// <inheritdoc />
        public ExternalIdMediaType Type => ExternalIdMediaType.Artist;

        /// <inheritdoc />
        public string UrlFormatString => "https://www.theaudiodb.com/artist/{0}";

        /// <inheritdoc />
        public bool Supports(IHasProviderIds item) => item is MusicArtist;
    }

    public class AudioDbOtherArtistExternalId : IExternalId
    {
        /// <inheritdoc />
        public string ProviderName => "TheAudioDb";

        /// <inheritdoc />
        public string Key => MetadataProviders.AudioDbArtist.ToString();

        /// <inheritdoc />
        public ExternalIdMediaType Type => ExternalIdMediaType.OtherArtist;

        /// <inheritdoc />
        public string UrlFormatString => "https://www.theaudiodb.com/artist/{0}";

        /// <inheritdoc />
        public bool Supports(IHasProviderIds item) => item is Audio || item is MusicAlbum;
    }
}
