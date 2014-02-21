using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Providers.Music
{
    public class AudioDbAlbumExternalId : IExternalId
    {
        public string Name
        {
            get { return "TheAudioDb"; }
        }

        public string Key
        {
            get { return MetadataProviders.AudioDbAlbum.ToString(); }
        }

        public string UrlFormatString
        {
            get { return "http://www.theaudiodb.com/album/{0}"; }
        }

        public bool Supports(IHasProviderIds item)
        {
            return item is MusicAlbum;
        }
    }

    public class AudioDbOtherAlbumExternalId : IExternalId
    {
        public string Name
        {
            get { return "TheAudioDb Album"; }
        }

        public string Key
        {
            get { return MetadataProviders.AudioDbAlbum.ToString(); }
        }

        public string UrlFormatString
        {
            get { return "http://www.theaudiodb.com/album/{0}"; }
        }

        public bool Supports(IHasProviderIds item)
        {
            return item is Audio;
        }
    }

    public class AudioDbArtistExternalId : IExternalId
    {
        public string Name
        {
            get { return "TheAudioDb"; }
        }

        public string Key
        {
            get { return MetadataProviders.AudioDbArtist.ToString(); }
        }

        public string UrlFormatString
        {
            get { return "http://www.theaudiodb.com/artist/{0}"; }
        }

        public bool Supports(IHasProviderIds item)
        {
            return item is MusicArtist;
        }
    }

    public class AudioDbOtherArtistExternalId : IExternalId
    {
        public string Name
        {
            get { return "TheAudioDb Artist"; }
        }

        public string Key
        {
            get { return MetadataProviders.AudioDbArtist.ToString(); }
        }

        public string UrlFormatString
        {
            get { return "http://www.theaudiodb.com/artist/{0}"; }
        }

        public bool Supports(IHasProviderIds item)
        {
            return item is Audio || item is MusicAlbum;
        }
    }

}
