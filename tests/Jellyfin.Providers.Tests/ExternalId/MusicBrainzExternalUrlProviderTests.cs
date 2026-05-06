using System;
using System.Reflection;
using MediaBrowser.Common;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Providers.Plugins.MusicBrainz;
using MediaBrowser.Providers.Plugins.MusicBrainz.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Providers.Tests.ExternalId
{
    public sealed class MusicBrainzExternalUrlProviderTests : IDisposable
    {
        private static readonly PropertyInfo _instanceProperty =
            typeof(Plugin).GetProperty("Instance", BindingFlags.Public | BindingFlags.Static)!;

        private static readonly MethodInfo _instanceSetter =
            _instanceProperty.GetSetMethod(nonPublic: true)!;

        private readonly Plugin? _previousPlugin;

        public MusicBrainzExternalUrlProviderTests()
        {
            _previousPlugin = Plugin.Instance;

            var appPathsMock = new Mock<IApplicationPaths>();
            appPathsMock.Setup(p => p.PluginsPath).Returns(System.IO.Path.GetTempPath());
            appPathsMock.Setup(p => p.PluginConfigurationsPath).Returns(System.IO.Path.GetTempPath());

            var xmlSerializerMock = new Mock<IXmlSerializer>();
            xmlSerializerMock
                .Setup(s => s.DeserializeFromFile(typeof(PluginConfiguration), It.IsAny<string>()))
                .Returns(new PluginConfiguration());

            var appHostMock = new Mock<IApplicationHost>();
            appHostMock.Setup(h => h.Name).Returns("Jellyfin");
            appHostMock.Setup(h => h.ApplicationVersionString).Returns("1.0.0");
            appHostMock.Setup(h => h.ApplicationUserAgentAddress).Returns("localhost");

            _ = new Plugin(appPathsMock.Object, xmlSerializerMock.Object, appHostMock.Object, NullLogger<Plugin>.Instance);
        }

        public void Dispose()
        {
            _instanceSetter.Invoke(null, new object?[] { _previousPlugin });
        }

        [Fact]
        public void GetExternalUrls_MusicAlbumWithMusicBrainzAlbumId_ReturnsCorrectUrl()
        {
            var album = new MusicAlbum();
            album.SetProviderId(MetadataProvider.MusicBrainzAlbum, "a1b2c3d4-e5f6-7890-abcd-ef1234567890");

            var urls = new MusicBrainzAlbumExternalUrlProvider().GetExternalUrls(album);

            Assert.Contains(PluginConfiguration.DefaultServer + "/release/a1b2c3d4-e5f6-7890-abcd-ef1234567890", urls);
        }

        [Fact]
        public void GetExternalUrls_MusicAlbumWithNoMusicBrainzAlbumId_ReturnsNoUrl()
        {
            var album = new MusicAlbum();

            var urls = new MusicBrainzAlbumExternalUrlProvider().GetExternalUrls(album);

            Assert.Empty(urls);
        }

        [Fact]
        public void GetExternalUrls_NonAlbumWithMusicBrainzAlbumId_ReturnsNoUrl()
        {
            var artist = new MusicArtist();
            artist.SetProviderId(MetadataProvider.MusicBrainzAlbum, "a1b2c3d4-e5f6-7890-abcd-ef1234567890");

            var urls = new MusicBrainzAlbumExternalUrlProvider().GetExternalUrls(artist);

            Assert.Empty(urls);
        }

        [Fact]
        public void GetExternalUrls_MusicAlbumWithMusicBrainzAlbumArtistId_ReturnsCorrectUrl()
        {
            var album = new MusicAlbum();
            album.SetProviderId(MetadataProvider.MusicBrainzAlbumArtist, "a1b2c3d4-e5f6-7890-abcd-ef1234567890");

            var urls = new MusicBrainzAlbumArtistExternalUrlProvider().GetExternalUrls(album);

            Assert.Contains(PluginConfiguration.DefaultServer + "/artist/a1b2c3d4-e5f6-7890-abcd-ef1234567890", urls);
        }

        [Fact]
        public void GetExternalUrls_MusicAlbumWithNoMusicBrainzAlbumArtistId_ReturnsNoUrl()
        {
            var album = new MusicAlbum();

            var urls = new MusicBrainzAlbumArtistExternalUrlProvider().GetExternalUrls(album);

            Assert.Empty(urls);
        }

        [Fact]
        public void GetExternalUrls_MusicArtistWithMusicBrainzArtistId_ReturnsCorrectUrl()
        {
            var artist = new MusicArtist();
            artist.SetProviderId(MetadataProvider.MusicBrainzArtist, "a1b2c3d4-e5f6-7890-abcd-ef1234567890");

            var urls = new MusicBrainzArtistExternalUrlProvider().GetExternalUrls(artist);

            Assert.Contains(PluginConfiguration.DefaultServer + "/artist/a1b2c3d4-e5f6-7890-abcd-ef1234567890", urls);
        }

        [Fact]
        public void GetExternalUrls_PersonWithMusicBrainzArtistId_ReturnsCorrectUrl()
        {
            var person = new Person();
            person.SetProviderId(MetadataProvider.MusicBrainzArtist, "a1b2c3d4-e5f6-7890-abcd-ef1234567890");

            var urls = new MusicBrainzArtistExternalUrlProvider().GetExternalUrls(person);

            Assert.Contains(PluginConfiguration.DefaultServer + "/artist/a1b2c3d4-e5f6-7890-abcd-ef1234567890", urls);
        }

        [Fact]
        public void GetExternalUrls_MusicArtistWithNoMusicBrainzArtistId_ReturnsNoUrl()
        {
            var artist = new MusicArtist();

            var urls = new MusicBrainzArtistExternalUrlProvider().GetExternalUrls(artist);

            Assert.Empty(urls);
        }

        [Fact]
        public void GetExternalUrls_NonArtistWithMusicBrainzArtistId_ReturnsNoUrl()
        {
            var album = new MusicAlbum();
            album.SetProviderId(MetadataProvider.MusicBrainzArtist, "a1b2c3d4-e5f6-7890-abcd-ef1234567890");

            var urls = new MusicBrainzArtistExternalUrlProvider().GetExternalUrls(album);

            Assert.Empty(urls);
        }

        [Fact]
        public void GetExternalUrls_MusicAlbumWithMusicBrainzReleaseGroupId_ReturnsCorrectUrl()
        {
            var album = new MusicAlbum();
            album.SetProviderId(MetadataProvider.MusicBrainzReleaseGroup, "a1b2c3d4-e5f6-7890-abcd-ef1234567890");

            var urls = new MusicBrainzReleaseGroupExternalUrlProvider().GetExternalUrls(album);

            Assert.Contains(PluginConfiguration.DefaultServer + "/release-group/a1b2c3d4-e5f6-7890-abcd-ef1234567890", urls);
        }

        [Fact]
        public void GetExternalUrls_MusicAlbumWithNoMusicBrainzReleaseGroupId_ReturnsNoUrl()
        {
            var album = new MusicAlbum();

            var urls = new MusicBrainzReleaseGroupExternalUrlProvider().GetExternalUrls(album);

            Assert.Empty(urls);
        }

        [Fact]
        public void GetExternalUrls_AudioWithMusicBrainzTrackId_ReturnsCorrectUrl()
        {
            var audio = new Audio();
            audio.SetProviderId(MetadataProvider.MusicBrainzTrack, "a1b2c3d4-e5f6-7890-abcd-ef1234567890");

            var urls = new MusicBrainzTrackExternalUrlProvider().GetExternalUrls(audio);

            Assert.Contains(PluginConfiguration.DefaultServer + "/track/a1b2c3d4-e5f6-7890-abcd-ef1234567890", urls);
        }

        [Fact]
        public void GetExternalUrls_AudioWithNoMusicBrainzTrackId_ReturnsNoUrl()
        {
            var audio = new Audio();

            var urls = new MusicBrainzTrackExternalUrlProvider().GetExternalUrls(audio);

            Assert.Empty(urls);
        }

        [Fact]
        public void GetExternalUrls_NonAudioWithMusicBrainzTrackId_ReturnsNoUrl()
        {
            var album = new MusicAlbum();
            album.SetProviderId(MetadataProvider.MusicBrainzTrack, "a1b2c3d4-e5f6-7890-abcd-ef1234567890");

            var urls = new MusicBrainzTrackExternalUrlProvider().GetExternalUrls(album);

            Assert.Empty(urls);
        }
    }
}
