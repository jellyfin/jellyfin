using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Providers.Plugins.MusicBrainz;

namespace MediaBrowser.Providers.Music
{

    public class MusicBrainzAlbumExternalId : IExternalId
    {
        /// <inheritdoc />
        public string Name => "MusicBrainz Album";

        /// <inheritdoc />
        public string Key => MetadataProviders.MusicBrainzAlbum.ToString();

        /// <inheritdoc />
        public string UrlFormatString => Plugin.Instance.Configuration.Server + "/release/{0}";

        /// <inheritdoc />
        public bool Supports(IHasProviderIds item) => item is Audio || item is MusicAlbum;
    }

    public class MusicBrainzArtistExternalId : IExternalId
    {
        /// <inheritdoc />
        public string Name => "MusicBrainz";

        /// <inheritdoc />
        public string Key => MetadataProviders.MusicBrainzArtist.ToString();

        /// <inheritdoc />
        public string UrlFormatString => Plugin.Instance.Configuration.Server + "/artist/{0}";

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
        public string UrlFormatString => Plugin.Instance.Configuration.Server + "/artist/{0}";

        /// <inheritdoc />
        public bool Supports(IHasProviderIds item) => item is Audio || item is MusicAlbum;
    }

    public class MusicBrainzTrackId : IExternalId
    {
        /// <inheritdoc />
        public string Name => "MusicBrainz Track";

        /// <inheritdoc />
        public string Key => MetadataProviders.MusicBrainzTrack.ToString();

        /// <inheritdoc />
        public string UrlFormatString => Plugin.Instance.Configuration.Server + "/track/{0}";

        /// <inheritdoc />
        public bool Supports(IHasProviderIds item) => item is Audio;
    }
}
