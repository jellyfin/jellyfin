using System;
using System.Linq;
using System.Threading;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Providers;
using MediaBrowser.Providers.Plugins.Tmdb.Movies;
using MediaBrowser.XbmcMetadata.Parsers;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.XbmcMetadata.Tests.Parsers
{
    public class MovieNfoParserTests
    {
        private readonly MovieNfoParser _parser;
        private readonly IUserDataManager _userDataManager;
        private readonly User _testUser;
        private readonly FileSystemMetadata _localImageFileMetadata;

        public MovieNfoParserTests()
        {
            _testUser = new User("Test User", "Auth provider", "Reset provider");

            var providerManager = new Mock<IProviderManager>();

            var tmdbExternalId = new TmdbMovieExternalId();
            var externalIdInfo = new ExternalIdInfo(tmdbExternalId.ProviderName, tmdbExternalId.Key, tmdbExternalId.Type, tmdbExternalId.UrlFormatString);

            providerManager.Setup(x => x.GetExternalIdInfos(It.IsAny<IHasProviderIds>()))
                .Returns(new[] { externalIdInfo });

            var nfoConfig = new XbmcMetadataOptions()
            {
                UserId = "F38E6443-090B-4F7A-BD12-9CFF5020F7BC"
            };
            var configManager = new Mock<IConfigurationManager>();
            configManager.Setup(x => x.GetConfiguration(It.IsAny<string>()))
                .Returns(nfoConfig);

            var user = new Mock<IUserManager>();
            user.Setup(x => x.GetUserById(It.IsAny<Guid>()))
                .Returns(_testUser);

            var userData = new Mock<IUserDataManager>();
            userData.Setup(x => x.GetUserData(_testUser, It.IsAny<BaseItem>()))
                .Returns(new UserItemData()
                {
                    Key = "Something"
                });

            var directoryService = new Mock<IDirectoryService>();
            _localImageFileMetadata = new FileSystemMetadata()
            {
                Exists = true,
                FullName = OperatingSystem.IsWindows() ?
                    @"C:\media\movies\Justice League (2017).jpg"
                    : "/media/movies/Justice League (2017).jpg"
            };
            directoryService.Setup(x => x.GetFile(_localImageFileMetadata.FullName))
                .Returns(_localImageFileMetadata);

            _userDataManager = userData.Object;
            _parser = new MovieNfoParser(
                new NullLogger<MovieNfoParser>(),
                configManager.Object,
                providerManager.Object,
                user.Object,
                userData.Object,
                directoryService.Object);
        }

        [Fact]
        public void Fetch_Valid_Success()
        {
            var result = new MetadataResult<Video>()
            {
                Item = new Movie()
            };

            _parser.Fetch(result, "Test Data/Justice League.nfo", CancellationToken.None);
            var item = (Movie)result.Item;

            Assert.Equal("Justice League", item.OriginalTitle);
            Assert.Equal("Justice for all.", item.Tagline);
            Assert.Equal("tt0974015", item.ProviderIds[MetadataProvider.Imdb.ToString()]);
            Assert.Equal("141052", item.ProviderIds[MetadataProvider.Tmdb.ToString()]);

            Assert.Equal(4, item.Genres.Length);
            Assert.Contains("Action", item.Genres);
            Assert.Contains("Adventure", item.Genres);
            Assert.Contains("Fantasy", item.Genres);
            Assert.Contains("Sci-Fi", item.Genres);

            Assert.Equal(new DateTime(2017, 11, 15), item.PremiereDate);
            Assert.Equal(new DateTime(2017, 11, 16), item.EndDate);
            Assert.Single(item.Studios);
            Assert.Contains("DC Comics", item.Studios);

            Assert.Equal("1.777778", item.AspectRatio);
            Assert.Equal(Video3DFormat.HalfSideBySide, item.Video3DFormat);
            Assert.Equal(1920, item.Width);
            Assert.Equal(1080, item.Height);
            Assert.Equal(new TimeSpan(0, 0, 6268).Ticks, item.RunTimeTicks);
            Assert.True(item.HasSubtitles);
            Assert.Equal(7.6f, item.CriticRating);
            Assert.Equal("8.7", item.CustomRating);
            Assert.Equal("en", item.PreferredMetadataLanguage);
            Assert.Equal("us", item.PreferredMetadataCountryCode);
            Assert.Single(item.RemoteTrailers);
            Assert.Equal("https://www.youtube.com/watch?v=dQw4w9WgXcQ", item.RemoteTrailers[0].Url);

            Assert.Equal(20, result.People.Count);

            var writers = result.People.Where(x => x.Type == PersonKind.Writer).ToArray();
            Assert.Equal(3, writers.Length);
            var writerNames = writers.Select(x => x.Name);
            Assert.Contains("Jerry Siegel", writerNames);
            Assert.Contains("Joe Shuster", writerNames);
            Assert.Contains("Test", writerNames);

            var directors = result.People.Where(x => x.Type == PersonKind.Director).ToArray();
            Assert.Single(directors);
            Assert.Equal("Zack Snyder", directors[0].Name);

            var actors = result.People.Where(x => x.Type == PersonKind.Actor).ToArray();
            Assert.Equal(15, actors.Length);

            // Only test one actor
            var aquaman = actors.FirstOrDefault(x => x.Role.Equals("Aquaman", StringComparison.Ordinal));
            Assert.NotNull(aquaman);
            Assert.Equal("Jason Momoa", aquaman!.Name);
            Assert.Equal(5, aquaman!.SortOrder);
            Assert.Equal("https://m.media-amazon.com/images/M/MV5BMTI5MTU5NjM1MV5BMl5BanBnXkFtZTcwODc4MDk0Mw@@._V1_SX1024_SY1024_.jpg", aquaman!.ImageUrl);

            var lyricist = result.People.FirstOrDefault(x => x.Type == PersonKind.Lyricist);
            Assert.NotNull(lyricist);
            Assert.Equal("Test Lyricist", lyricist!.Name);

            Assert.Equal(new DateTime(2019, 8, 6, 9, 1, 18), item.DateCreated);

            // userData
            var userData = _userDataManager.GetUserData(_testUser, item);
            Assert.Equal(2, userData.PlayCount);
            Assert.True(userData.Played);
            Assert.Equal(new DateTime(2021, 02, 11, 07, 47, 23), userData.LastPlayedDate);

            // Movie set
            Assert.Equal("702342", item.ProviderIds[MetadataProvider.TmdbCollection.ToString()]);
            Assert.Equal("Justice League Collection", item.CollectionName);

            // Images
            Assert.Equal(7, result.RemoteImages.Count);

            var posters = result.RemoteImages.Where(x => x.Type == ImageType.Primary).ToList();
            Assert.Single(posters);
            Assert.Equal("http://image.tmdb.org/t/p/original/9rtrRGeRnL0JKtu9IMBWsmlmmZz.jpg", posters[0].Url);

            var logos = result.RemoteImages.Where(x => x.Type == ImageType.Logo).ToList();
            Assert.Single(logos);
            Assert.Equal("https://assets.fanart.tv/fanart/movies/141052/hdmovielogo/justice-league-5865bf95cbadb.png", logos[0].Url);

            var banners = result.RemoteImages.Where(x => x.Type == ImageType.Banner).ToList();
            Assert.Single(banners);
            Assert.Equal("https://assets.fanart.tv/fanart/movies/141052/moviebanner/justice-league-586017e95adbd.jpg", banners[0].Url);

            var thumbs = result.RemoteImages.Where(x => x.Type == ImageType.Thumb).ToList();
            Assert.Single(thumbs);
            Assert.Equal("https://assets.fanart.tv/fanart/movies/141052/moviethumb/justice-league-585fb155c3743.jpg", thumbs[0].Url);

            var art = result.RemoteImages.Where(x => x.Type == ImageType.Art).ToList();
            Assert.Single(art);
            Assert.Equal("https://assets.fanart.tv/fanart/movies/141052/hdmovieclearart/justice-league-5865c23193041.png", art[0].Url);

            var discArt = result.RemoteImages.Where(x => x.Type == ImageType.Disc).ToList();
            Assert.Single(discArt);
            Assert.Equal("https://assets.fanart.tv/fanart/movies/141052/moviedisc/justice-league-5a3af26360617.png", discArt[0].Url);

            var backdrop = result.RemoteImages.Where(x => x.Type == ImageType.Backdrop).ToList();
            Assert.Single(backdrop);
            Assert.Equal("https://assets.fanart.tv/fanart/movies/141052/moviebackground/justice-league-5793f518c6d6e.jpg", backdrop[0].Url);

            // Local Image - contains only one item depending on operating system
            Assert.Single(result.Images);
            Assert.Equal(_localImageFileMetadata.Name, result.Images[0].FileInfo.Name);
        }

        [Theory]
        [InlineData("Test Data/Tmdb.nfo", "Tmdb", "30287")]
        [InlineData("Test Data/Imdb.nfo", "Imdb", "tt0944947")]
        public void Parse_UrlFile_Success(string path, string provider, string id)
        {
            var result = new MetadataResult<Video>()
            {
                Item = new Movie()
            };

            _parser.Fetch(result, path, CancellationToken.None);
            var item = (Movie)result.Item;

            Assert.Equal(id, item.ProviderIds[provider]);
        }

        [Fact]
        public void Parse_GivenFileWithFanartTag_Success()
        {
            var result = new MetadataResult<Video>()
            {
                Item = new Movie()
            };

            _parser.Fetch(result, "Test Data/Fanart.nfo", CancellationToken.None);

            Assert.Single(result.RemoteImages, x => x.Type == ImageType.Backdrop);
            Assert.Equal("https://assets.fanart.tv/fanart/movies/141052/moviebackground/justice-league-5a5332c7b5e77.jpg", result.RemoteImages.First(x => x.Type == ImageType.Backdrop).Url);
        }

        [Fact]
        public void Parse_RadarrUrlFile_Success()
        {
            var result = new MetadataResult<Video>()
            {
                Item = new Movie()
            };

            _parser.Fetch(result, "Test Data/Radarr.nfo", CancellationToken.None);
            var item = (Movie)result.Item;

            Assert.Equal("583689", item.ProviderIds[MetadataProvider.Tmdb.ToString()]);
            Assert.Equal("tt4154796", item.ProviderIds[MetadataProvider.Imdb.ToString()]);
        }

        [Fact]
        public void Fetch_WithNullItem_ThrowsArgumentException()
        {
            var result = new MetadataResult<Video>();

            Assert.Throws<ArgumentException>(() => _parser.Fetch(result, "Test Data/Justice League.nfo", CancellationToken.None));
        }

        [Fact]
        public void Fetch_NullResult_ThrowsArgumentException()
        {
            var result = new MetadataResult<Video>()
            {
                Item = new Movie()
            };

            Assert.Throws<ArgumentException>(() => _parser.Fetch(result, string.Empty, CancellationToken.None));
        }

        [Fact]
        public void Parsing_Fields_With_Escaped_Xml_Special_Characters_Success()
        {
            var result = new MetadataResult<Video>()
            {
                Item = new Movie()
            };

            _parser.Fetch(result, "Test Data/Lilo & Stitch.nfo", CancellationToken.None);
            var item = (Movie)result.Item;

            Assert.Equal("Lilo & Stitch", item.Name);
            Assert.Equal("Lilo & Stitch", item.OriginalTitle);
            Assert.Equal("Lilo & Stitch Collection", item.CollectionName);
            Assert.StartsWith(">>", item.Overview, StringComparison.InvariantCulture);
            Assert.EndsWith("<<", item.Overview, StringComparison.InvariantCulture);
        }
    }
}
