using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Model.Entities;
using MediaBrowser.Providers.Plugins.AudioDb;
using Xunit;

namespace Jellyfin.Providers.Tests.ExternalId
{
    public sealed class AudioDbExternalUrlProviderTests
    {
        private readonly AudioDbAlbumExternalUrlProvider _albumProvider = new();
        private readonly AudioDbArtistExternalUrlProvider _artistProvider = new();

        [Fact]
        public void GetExternalUrls_MusicAlbumWithAudioDbAlbumId_ReturnsCorrectUrl()
        {
            var album = new MusicAlbum();
            album.SetProviderId(MetadataProvider.AudioDbAlbum, "12345");

            var urls = _albumProvider.GetExternalUrls(album);

            Assert.Contains("https://www.theaudiodb.com/album/12345", urls);
        }

        [Fact]
        public void GetExternalUrls_MusicAlbumWithNoAudioDbAlbumId_ReturnsNoUrl()
        {
            var album = new MusicAlbum();

            var urls = _albumProvider.GetExternalUrls(album);

            Assert.Empty(urls);
        }

        [Fact]
        public void GetExternalUrls_NonAlbumWithAudioDbAlbumId_ReturnsNoUrl()
        {
            var artist = new MusicArtist();
            artist.SetProviderId(MetadataProvider.AudioDbAlbum, "12345");

            var urls = _albumProvider.GetExternalUrls(artist);

            Assert.Empty(urls);
        }

        [Fact]
        public void GetExternalUrls_MusicArtistWithAudioDbArtistId_ReturnsCorrectUrl()
        {
            var artist = new MusicArtist();
            artist.SetProviderId(MetadataProvider.AudioDbArtist, "67890");

            var urls = _artistProvider.GetExternalUrls(artist);

            Assert.Contains("https://www.theaudiodb.com/artist/67890", urls);
        }

        [Fact]
        public void GetExternalUrls_PersonWithAudioDbArtistId_ReturnsCorrectUrl()
        {
            var person = new Person();
            person.SetProviderId(MetadataProvider.AudioDbArtist, "67890");

            var urls = _artistProvider.GetExternalUrls(person);

            Assert.Contains("https://www.theaudiodb.com/artist/67890", urls);
        }

        [Fact]
        public void GetExternalUrls_MusicArtistWithNoAudioDbArtistId_ReturnsNoUrl()
        {
            var artist = new MusicArtist();

            var urls = _artistProvider.GetExternalUrls(artist);

            Assert.Empty(urls);
        }

        [Fact]
        public void GetExternalUrls_NonArtistWithAudioDbArtistId_ReturnsNoUrl()
        {
            var album = new MusicAlbum();
            album.SetProviderId(MetadataProvider.AudioDbArtist, "67890");

            var urls = _artistProvider.GetExternalUrls(album);

            Assert.Empty(urls);
        }
    }
}
