using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Providers.Music
{
    public class MusicBrainzReleaseGroupExternalId : IExternalId
    {
        public string Name
        {
            get { return "MusicBrainz Release Group"; }
        }

        public string Key
        {
            get { return MetadataProviders.MusicBrainzReleaseGroup.ToString(); }
        }

        public string UrlFormatString
        {
            get { return "https://musicbrainz.org/release-group/{0}"; }
        }

        public bool Supports(IHasProviderIds item)
        {
            return item is Audio || item is MusicAlbum;
        }
    }

    public class MusicBrainzAlbumArtistExternalId : IExternalId
    {
        public string Name
        {
            get { return "MusicBrainz Album Artist"; }
        }

        public string Key
        {
            get { return MetadataProviders.MusicBrainzAlbumArtist.ToString(); }
        }

        public string UrlFormatString
        {
            get { return "https://musicbrainz.org/artist/{0}"; }
        }

        public bool Supports(IHasProviderIds item)
        {
            return item is Audio;
        }
    }

    public class MusicBrainzAlbumExternalId : IExternalId
    {
        public string Name
        {
            get { return "MusicBrainz Album"; }
        }

        public string Key
        {
            get { return MetadataProviders.MusicBrainzAlbum.ToString(); }
        }

        public string UrlFormatString
        {
            get { return "https://musicbrainz.org/release/{0}"; }
        }

        public bool Supports(IHasProviderIds item)
        {
            return item is Audio || item is MusicAlbum;
        }
    }

    public class MusicBrainzArtistExternalId : IExternalId
    {
        public string Name
        {
            get { return "MusicBrainz"; }
        }

        public string Key
        {
            get { return MetadataProviders.MusicBrainzArtist.ToString(); }
        }

        public string UrlFormatString
        {
            get { return "https://musicbrainz.org/artist/{0}"; }
        }

        public bool Supports(IHasProviderIds item)
        {
            return item is MusicArtist;
        }
    }

    public class MusicBrainzOtherArtistExternalId : IExternalId
    {
        public string Name
        {
            get { return "MusicBrainz Artist"; }
        }

        public string Key
        {
            get { return MetadataProviders.MusicBrainzArtist.ToString(); }
        }

        public string UrlFormatString
        {
            get { return "https://musicbrainz.org/artist/{0}"; }
        }

        public bool Supports(IHasProviderIds item)
        {
            return item is Audio || item is MusicAlbum;
        }
    }

    public class MusicBrainzTrackId : IExternalId
    {
        public string Name
        {
            get { return "MusicBrainz Track"; }
        }

        public string Key
        {
            get { return MetadataProviders.MusicBrainzTrack.ToString(); }
        }

        public string UrlFormatString
        {
            get { return "https://musicbrainz.org/track/{0}"; }
        }

        public bool Supports(IHasProviderIds item)
        {
            return item is Audio;
        }
    }
}
