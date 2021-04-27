using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using Jellyfin.Data.Entities;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using MediaBrowser.Providers.Movies;
using MediaBrowser.Providers.TV;
using MediaBrowser.XbmcMetadata.Parsers;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

#pragma warning disable CA5369

namespace Jellyfin.XbmcMetadata.Tests.Parsers
{
    public class EpisodeNfoProviderTests
    {
        private readonly EpisodeNfoParser _parser;
        private readonly IUserManager _userManager;
        private readonly IUserDataManager _userDataManager;
        private readonly User _testUser;
        private readonly EpisodeMetadataService _metadataService;
        private UserItemData _testUserItemData = new UserItemData();

        public EpisodeNfoProviderTests()
        {
            _testUser = new User("Test User", "Auth provider", "Reset provider");

            var providerManager = new Mock<IProviderManager>();

            var imdbExternalId = new ImdbExternalId();
            var externalIdInfo = new ExternalIdInfo(imdbExternalId.ProviderName, imdbExternalId.Key, imdbExternalId.Type, imdbExternalId.UrlFormatString);

            providerManager.Setup(x => x.GetExternalIdInfos(It.IsAny<IHasProviderIds>()))
                .Returns(new[] { externalIdInfo });

            var nfoConfig = new XbmcMetadataOptions()
            {
                UserId = _testUser.Id.ToString("N", CultureInfo.InvariantCulture)
            };
            var configManager = new Mock<IServerConfigurationManager>();
            configManager.Setup(x => x.GetConfiguration(It.IsAny<string>()))
                .Returns(nfoConfig);

            var userManager = new Mock<IUserManager>();
            userManager.Setup(x => x.GetUserById(It.IsAny<Guid>()))
                .Returns(_testUser);
            _userManager = userManager.Object;

            var userData = new Mock<IUserDataManager>();
            userData.Setup(x => x.SaveUserData(
                    It.IsAny<User>(),
                    It.IsAny<BaseItem>(),
                    It.IsAny<UserItemData>(),
                    It.IsAny<UserDataSaveReason>(),
                    It.IsAny<CancellationToken>())).Callback<
                        User,
                        BaseItem,
                        UserItemData,
                        UserDataSaveReason,
                        CancellationToken>((u, i, d, r, t) => _testUserItemData = d);
            userData.Setup(x => x.GetUserData(_testUser, It.IsAny<BaseItem>()))
                .Returns(() => _testUserItemData);
            _userDataManager = userData.Object;

            var directoryService = new Mock<IDirectoryService>();

            _metadataService = new EpisodeMetadataService(
                configManager.Object,
                new NullLogger<EpisodeMetadataService>(),
                providerManager.Object,
                null,
                null,
                _userManager,
                _userDataManager);

            _parser = new EpisodeNfoParser(
                new NullLogger<EpisodeNfoParser>(),
                configManager.Object,
                providerManager.Object,
                _userManager,
                _userDataManager,
                directoryService.Object);
        }

        [Fact]
        public void Fetch_Valid_Success()
        {
            var result = new MetadataResult<Episode>()
            {
                Item = new Episode()
            };

            _parser.Fetch(result, "Test Data/The Bone Orchard.nfo", CancellationToken.None);
            var item = result.Item;

            // Invoke protected method to update UserData as we don't want to test the whole
            // MetadataService here. This would need too much infrastructure setup to make sense.
            var method = typeof(EpisodeMetadataService).GetMethod("ImportUserData", BindingFlags.Instance | BindingFlags.NonPublic);
            method?.Invoke(_metadataService, new object[] { item, result.UserDataList, CancellationToken.None });

            Assert.Equal("The Bone Orchard", item.Name);
            Assert.Equal("American Gods", item.SeriesName);
            Assert.Equal(1, item.IndexNumber);
            Assert.Equal(1, item.ParentIndexNumber);
            Assert.Equal("When Shadow Moon is released from prison early after the death of his wife, he meets Mr. Wednesday and is recruited as his bodyguard. Shadow discovers that this may be more than he bargained for.", item.Overview);
            Assert.Equal(0, item.RunTimeTicks);
            Assert.Equal("16", item.OfficialRating);
            Assert.Contains("Drama", item.Genres);
            Assert.Contains("Mystery", item.Genres);
            Assert.Contains("Sci-Fi & Fantasy", item.Genres);
            Assert.Equal(new DateTime(2017, 4, 30), item.PremiereDate);
            Assert.Equal(2017, item.ProductionYear);
            Assert.Single(item.Studios);
            Assert.Contains("Starz", item.Studios);
            Assert.Equal(1, item.IndexNumberEnd);
            Assert.Equal(2, item.AirsAfterSeasonNumber);
            Assert.Equal(3, item.AirsBeforeSeasonNumber);
            Assert.Equal(1, item.AirsBeforeEpisodeNumber);
            Assert.Equal("tt5017734", item.ProviderIds[MetadataProvider.Imdb.ToString()]);
            Assert.Equal("1276153", item.ProviderIds[MetadataProvider.Tmdb.ToString()]);

            // Credits
            var writers = result.People.Where(x => x.Type == PersonType.Writer).ToArray();
            Assert.Equal(2, writers.Length);
            Assert.Contains("Bryan Fuller", writers.Select(x => x.Name));
            Assert.Contains("Michael Green", writers.Select(x => x.Name));

            // Direcotrs
            var directors = result.People.Where(x => x.Type == PersonType.Director).ToArray();
            Assert.Single(directors);
            Assert.Contains("David Slade", directors.Select(x => x.Name));

            // userData
            var userData = _userDataManager.GetUserData(_testUser, item);
            Assert.Equal(TimeSpan.FromSeconds(1000).Ticks, userData.PlaybackPositionTicks);

            // Actors
            var actors = result.People.Where(x => x.Type == PersonType.Actor).ToArray();
            Assert.Equal(11, actors.Length);
            // Only test one actor
            var shadow = actors.FirstOrDefault(x => x.Role.Equals("Shadow Moon", StringComparison.Ordinal));
            Assert.NotNull(shadow);
            Assert.Equal("Ricky Whittle", shadow!.Name);
            Assert.Equal(0, shadow!.SortOrder);
            Assert.Equal("http://image.tmdb.org/t/p/original/cjeDbVfBp6Qvb3C74Dfy7BKDTQN.jpg", shadow!.ImageUrl);

            Assert.Equal(new DateTime(2017, 10, 7, 14, 25, 47), item.DateCreated);
        }

        [Fact]
        public void Fetch_Valid_MultiEpisode_Success()
        {
            var result = new MetadataResult<Episode>()
            {
                Item = new Episode()
            };

            _parser.Fetch(result, "Test Data/Rising.nfo", CancellationToken.None);

            var item = result.Item;
            Assert.Equal("Rising (1)", item.Name);
            Assert.Equal(1, item.IndexNumber);
            Assert.Equal(2, item.IndexNumberEnd);
            Assert.Equal(1, item.ParentIndexNumber);
            Assert.Equal("A new Stargate team embarks on a dangerous mission to a distant galaxy, where they discover a mythical lost city -- and a deadly new enemy.", item.Overview);
            Assert.Equal(new DateTime(2004, 7, 16), item.PremiereDate);
            Assert.Equal(2004, item.ProductionYear);
        }

        [Fact]
        public void Fetch_WithNullItem_ThrowsArgumentException()
        {
            var result = new MetadataResult<Episode>();

            Assert.Throws<ArgumentException>(() => _parser.Fetch(result, "Test Data/The Bone Orchard.nfo", CancellationToken.None));
        }

        [Fact]
        public void Fetch_NullResult_ThrowsArgumentException()
        {
            var result = new MetadataResult<Episode>()
            {
                Item = new Episode()
            };

            Assert.Throws<ArgumentException>(() => _parser.Fetch(result, string.Empty, CancellationToken.None));
        }
    }
}
