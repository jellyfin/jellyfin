using Jellyfin.Controller.Entities.Audio;
using Jellyfin.Controller.Providers;
using Jellyfin.Model.Entities;

namespace Jellyfin.Providers.Music
{
    public class AudioDbAlbumExternalId : IExternalId
    {
        public string Name => "TheAudioDb";

        public string Key => MetadataProviders.AudioDbAlbum.ToString();

        public string UrlFormatString => "https://www.theaudiodb.com/album/{0}";

        public bool Supports(IHasProviderIds item)
        {
            return item is MusicAlbum;
        }
    }

    public class AudioDbOtherAlbumExternalId : IExternalId
    {
        public string Name => "TheAudioDb Album";

        public string Key => MetadataProviders.AudioDbAlbum.ToString();

        public string UrlFormatString => "https://www.theaudiodb.com/album/{0}";

        public bool Supports(IHasProviderIds item)
        {
            return item is Audio;
        }
    }

    public class AudioDbArtistExternalId : IExternalId
    {
        public string Name => "TheAudioDb";

        public string Key => MetadataProviders.AudioDbArtist.ToString();

        public string UrlFormatString => "https://www.theaudiodb.com/artist/{0}";

        public bool Supports(IHasProviderIds item)
        {
            return item is MusicArtist;
        }
    }

    public class AudioDbOtherArtistExternalId : IExternalId
    {
        public string Name => "TheAudioDb Artist";

        public string Key => MetadataProviders.AudioDbArtist.ToString();

        public string UrlFormatString => "https://www.theaudiodb.com/artist/{0}";

        public bool Supports(IHasProviderIds item)
        {
            return item is Audio || item is MusicAlbum;
        }
    }

}
