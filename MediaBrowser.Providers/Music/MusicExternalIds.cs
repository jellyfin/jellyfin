using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Providers.Music
{
    public class MusicBrainzReleaseGroupExternalId : IExternalId
    {
        public string Name => "MusicBrainz Release Group";

        public string Key => MetadataProviders.MusicBrainzReleaseGroup.ToString();

        public string UrlFormatString => "https://musicbrainz.org/release-group/{0}";

        public bool Supports(IHasProviderIds item)
        {
            return item is Audio || item is MusicAlbum;
        }
    }

    public class MusicBrainzAlbumArtistExternalId : IExternalId
    {
        public string Name => "MusicBrainz Album Artist";

        public string Key => MetadataProviders.MusicBrainzAlbumArtist.ToString();

        public string UrlFormatString => "https://musicbrainz.org/artist/{0}";

        public bool Supports(IHasProviderIds item)
        {
            return item is Audio;
        }
    }

    public class MusicBrainzAlbumExternalId : IExternalId
    {
        public string Name => "MusicBrainz Album";

        public string Key => MetadataProviders.MusicBrainzAlbum.ToString();

        public string UrlFormatString => "https://musicbrainz.org/release/{0}";

        public bool Supports(IHasProviderIds item)
        {
            return item is Audio || item is MusicAlbum;
        }
    }

    public class MusicBrainzArtistExternalId : IExternalId
    {
        public string Name => "MusicBrainz";

        public string Key => MetadataProviders.MusicBrainzArtist.ToString();

        public string UrlFormatString => "https://musicbrainz.org/artist/{0}";

        public bool Supports(IHasProviderIds item)
        {
            return item is MusicArtist;
        }
    }

    public class MusicBrainzOtherArtistExternalId : IExternalId
    {
        public string Name => "MusicBrainz Artist";

        public string Key => MetadataProviders.MusicBrainzArtist.ToString();

        public string UrlFormatString => "https://musicbrainz.org/artist/{0}";

        public bool Supports(IHasProviderIds item)
        {
            return item is Audio || item is MusicAlbum;
        }
    }

    public class MusicBrainzTrackId : IExternalId
    {
        public string Name => "MusicBrainz Track";

        public string Key => MetadataProviders.MusicBrainzTrack.ToString();

        public string UrlFormatString => "https://musicbrainz.org/track/{0}";

        public bool Supports(IHasProviderIds item)
        {
            return item is Audio;
        }
    }

    public class ImvdbId : IExternalId
    {
        public string Name => "IMVDb";

        public string Key => "IMVDb";

        public string UrlFormatString => null;

        public bool Supports(IHasProviderIds item)
        {
            return item is MusicVideo;
        }
    }
}
