using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Providers.Music
{
    public class MusicBrainzReleaseGroupExternalId : IExternalId
    {
        /// <inheritdoc />
        public string Name => "MusicBrainz Release Group";

        /// <inheritdoc />
        public string Key => MetadataProviders.MusicBrainzReleaseGroup.ToString();

        /// <inheritdoc />
        public string UrlFormatString => "https://musicbrainz.org/release-group/{0}";

        /// <inheritdoc />
        public bool Supports(IHasProviderIds item)
            => item is Audio || item is MusicAlbum;
    }

    public class MusicBrainzAlbumArtistExternalId : IExternalId
    {
        /// <inheritdoc />
        public string Name => "MusicBrainz Album Artist";

        /// <inheritdoc />
        public string Key => MetadataProviders.MusicBrainzAlbumArtist.ToString();

        /// <inheritdoc />
        public string UrlFormatString => "https://musicbrainz.org/artist/{0}";

        /// <inheritdoc />
        public bool Supports(IHasProviderIds item)
            => item is Audio;
    }

    public class MusicBrainzAlbumExternalId : IExternalId
    {
        /// <inheritdoc />
        public string Name => "MusicBrainz Album";

        /// <inheritdoc />
        public string Key => MetadataProviders.MusicBrainzAlbum.ToString();

        /// <inheritdoc />
        public string UrlFormatString => "https://musicbrainz.org/release/{0}";

        /// <inheritdoc />
        public bool Supports(IHasProviderIds item)
            => item is Audio || item is MusicAlbum;
    }

    public class MusicBrainzArtistExternalId : IExternalId
    {
        /// <inheritdoc />
        public string Name => "MusicBrainz";

        /// <inheritdoc />
        public string Key => MetadataProviders.MusicBrainzArtist.ToString();

        /// <inheritdoc />
        public string UrlFormatString => "https://musicbrainz.org/artist/{0}";

        /// <inheritdoc />
        public bool Supports(IHasProviderIds item) => item is MusicArtist;
    }

    public class MusicBrainzOtherArtistExternalId : IExternalId
    {
        /// <inheritdoc />
        public string Name => "MusicBrainz Artist";

        /// <inheritdoc />

        public string Key => MetadataProviders.MusicBrainzArtist.ToString();

        /// <inheritdoc />
        public string UrlFormatString => "https://musicbrainz.org/artist/{0}";

        /// <inheritdoc />
        public bool Supports(IHasProviderIds item)
            => item is Audio || item is MusicAlbum;
    }

    public class MusicBrainzTrackId : IExternalId
    {
        /// <inheritdoc />
        public string Name => "MusicBrainz Track";

        /// <inheritdoc />
        public string Key => MetadataProviders.MusicBrainzTrack.ToString();

        /// <inheritdoc />
        public string UrlFormatString => "https://musicbrainz.org/track/{0}";

        /// <inheritdoc />
        public bool Supports(IHasProviderIds item) => item is Audio;
    }
}
