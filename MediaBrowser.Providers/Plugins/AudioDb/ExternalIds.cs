using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Providers.Plugins.AudioDb
{
    public class AudioDbAlbumExternalId : IExternalId
    {
        /// <inheritdoc />
        public string Name => "TheAudioDb";

        /// <inheritdoc />
        public string Key => MetadataProviders.AudioDbAlbum.ToString();

        /// <inheritdoc />
        public ExternalIdType Type => ExternalIdType.None;

        /// <inheritdoc />
        public string UrlFormatString => "https://www.theaudiodb.com/album/{0}";

        /// <inheritdoc />
        public bool Supports(IHasProviderIds item) => item is MusicAlbum;
    }

    public class AudioDbOtherAlbumExternalId : IExternalId
    {
        /// <inheritdoc />
        public string Name => "TheAudioDb";

        /// <inheritdoc />
        public string Key => MetadataProviders.AudioDbAlbum.ToString();

        /// <inheritdoc />
        public ExternalIdType Type => ExternalIdType.Album;

        /// <inheritdoc />
        public string UrlFormatString => "https://www.theaudiodb.com/album/{0}";

        /// <inheritdoc />
        public bool Supports(IHasProviderIds item) => item is Audio;
    }

    public class AudioDbArtistExternalId : IExternalId
    {
        /// <inheritdoc />
        public string Name => "TheAudioDb";

        /// <inheritdoc />
        public string Key => MetadataProviders.AudioDbArtist.ToString();

        /// <inheritdoc />
        public ExternalIdType Type => ExternalIdType.Artist;

        /// <inheritdoc />
        public string UrlFormatString => "https://www.theaudiodb.com/artist/{0}";

        /// <inheritdoc />
        public bool Supports(IHasProviderIds item) => item is MusicArtist;
    }

    public class AudioDbOtherArtistExternalId : IExternalId
    {
        /// <inheritdoc />
        public string Name => "TheAudioDb";

        /// <inheritdoc />
        public string Key => MetadataProviders.AudioDbArtist.ToString();

        /// <inheritdoc />
        public ExternalIdType Type => ExternalIdType.OtherArtist;

        /// <inheritdoc />
        public string UrlFormatString => "https://www.theaudiodb.com/artist/{0}";

        /// <inheritdoc />
        public bool Supports(IHasProviderIds item) => item is Audio || item is MusicAlbum;
    }
}
