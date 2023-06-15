using System;
using System.Collections.Generic;
using AutoFixture;
using AutoFixture.AutoMoq;
using Emby.Server.Implementations.Data;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Data
{
    public class SqliteItemRepositoryTests
    {
        public const string VirtualMetaDataPath = "%MetadataPath%";
        public const string MetaDataPath = "/meta/data/path";

        private readonly IFixture _fixture;
        private readonly SqliteItemRepository _sqliteItemRepository;

        public SqliteItemRepositoryTests()
        {
            var appHost = new Mock<IServerApplicationHost>();
            appHost.Setup(x => x.ExpandVirtualPath(It.IsAny<string>()))
                .Returns((string x) => x.Replace(VirtualMetaDataPath, MetaDataPath, StringComparison.Ordinal));
            appHost.Setup(x => x.ReverseVirtualPath(It.IsAny<string>()))
                .Returns((string x) => x.Replace(MetaDataPath, VirtualMetaDataPath, StringComparison.Ordinal));

            var configSection = new Mock<IConfigurationSection>();
            configSection
                .SetupGet(x => x[It.Is<string>(s => s == MediaBrowser.Controller.Extensions.ConfigurationExtensions.SqliteCacheSizeKey)])
                .Returns("0");
            var config = new Mock<IConfiguration>();
            config
                .Setup(x => x.GetSection(It.Is<string>(s => s == MediaBrowser.Controller.Extensions.ConfigurationExtensions.SqliteCacheSizeKey)))
                .Returns(configSection.Object);

            _fixture = new Fixture().Customize(new AutoMoqCustomization { ConfigureMembers = true });
            _fixture.Inject(appHost);
            _fixture.Inject(config);
            _sqliteItemRepository = _fixture.Create<SqliteItemRepository>();
        }

        public static TheoryData<string, ItemImageInfo> ItemImageInfoFromValueString_Valid_TestData()
        {
            var data = new TheoryData<string, ItemImageInfo>();

            data.Add(
                "/mnt/series/Family Guy/Season 1/Family Guy - S01E01-thumb.jpg*637452096478512963*Primary*1920*1080*WjQbtJtSO8nhNZ%L_Io#R/oaS6o}-;adXAoIn7j[%hW9s:WGw[nN",
                new ItemImageInfo
                {
                    Path = "/mnt/series/Family Guy/Season 1/Family Guy - S01E01-thumb.jpg",
                    Type = ImageType.Primary,
                    DateModified = new DateTime(637452096478512963, DateTimeKind.Utc),
                    Width = 1920,
                    Height = 1080,
                    BlurHash = "WjQbtJtSO8nhNZ%L_Io#R*oaS6o}-;adXAoIn7j[%hW9s:WGw[nN"
                });

            data.Add(
                "https://image.tmdb.org/t/p/original/zhB5CHEgqqh4wnEqDNJLfWXJlcL.jpg*0*Primary*0*0",
                new ItemImageInfo
                {
                    Path = "https://image.tmdb.org/t/p/original/zhB5CHEgqqh4wnEqDNJLfWXJlcL.jpg",
                    Type = ImageType.Primary,
                });

            data.Add(
                "https://image.tmdb.org/t/p/original/zhB5CHEgqqh4wnEqDNJLfWXJlcL.jpg*0*Primary",
                new ItemImageInfo
                {
                    Path = "https://image.tmdb.org/t/p/original/zhB5CHEgqqh4wnEqDNJLfWXJlcL.jpg",
                    Type = ImageType.Primary,
                });

            data.Add(
                "https://image.tmdb.org/t/p/original/zhB5CHEgqqh4wnEqDNJLfWXJlcL.jpg*0*Primary*600",
                new ItemImageInfo
                {
                    Path = "https://image.tmdb.org/t/p/original/zhB5CHEgqqh4wnEqDNJLfWXJlcL.jpg",
                    Type = ImageType.Primary,
                });

            data.Add(
                "%MetadataPath%/library/68/68578562b96c80a7ebd530848801f645/poster.jpg*637264380567586027*Primary*600*336",
                new ItemImageInfo
                {
                    Path = "/meta/data/path/library/68/68578562b96c80a7ebd530848801f645/poster.jpg",
                    Type = ImageType.Primary,
                    DateModified = new DateTime(637264380567586027, DateTimeKind.Utc),
                    Width = 600,
                    Height = 336
                });

            return data;
        }

        [Theory]
        [MemberData(nameof(ItemImageInfoFromValueString_Valid_TestData))]
        public void ItemImageInfoFromValueString_Valid_Success(string value, ItemImageInfo expected)
        {
            var result = _sqliteItemRepository.ItemImageInfoFromValueString(value);
            Assert.Equal(expected.Path, result.Path);
            Assert.Equal(expected.Type, result.Type);
            Assert.Equal(expected.DateModified, result.DateModified);
            Assert.Equal(expected.Width, result.Width);
            Assert.Equal(expected.Height, result.Height);
            Assert.Equal(expected.BlurHash, result.BlurHash);
        }

        [Theory]
        [InlineData("")]
        [InlineData("*")]
        [InlineData("https://image.tmdb.org/t/p/original/zhB5CHEgqqh4wnEqDNJLfWXJlcL.jpg*0")]
        [InlineData("/mnt/series/Family Guy/Season 1/Family Guy - S01E01-thumb.jpg*6374520964785129080*WjQbtJtSO8nhNZ%L_Io#R/oaS<o}-;adXAoIn7j[%hW9s:WGw[nN")] // Invalid modified date
        [InlineData("/mnt/series/Family Guy/Season 1/Family Guy - S01E01-thumb.jpg*-637452096478512963*WjQbtJtSO8nhNZ%L_Io#R/oaS<o}-;adXAoIn7j[%hW9s:WGw[nN")] // Negative modified date
        [InlineData("/mnt/series/Family Guy/Season 1/Family Guy - S01E01-thumb.jpg*637452096478512963*Invalid*1920*1080*WjQbtJtSO8nhNZ%L_Io#R/oaS6o}-;adXAoIn7j[%hW9s:WGw[nN")] // Invalid type
        public void ItemImageInfoFromValueString_Invalid_Null(string value)
        {
            Assert.Null(_sqliteItemRepository.ItemImageInfoFromValueString(value));
        }

        public static TheoryData<string, ItemImageInfo[]> DeserializeImages_Valid_TestData()
        {
            var data = new TheoryData<string, ItemImageInfo[]>();
            data.Add(
                "/mnt/series/Family Guy/Season 1/Family Guy - S01E01-thumb.jpg*637452096478512963*Primary*1920*1080*WjQbtJtSO8nhNZ%L_Io#R/oaS6o}-;adXAoIn7j[%hW9s:WGw[nN",
                new ItemImageInfo[]
                {
                    new ItemImageInfo()
                    {
                        Path = "/mnt/series/Family Guy/Season 1/Family Guy - S01E01-thumb.jpg",
                        Type = ImageType.Primary,
                        DateModified = new DateTime(637452096478512963, DateTimeKind.Utc),
                        Width = 1920,
                        Height = 1080,
                        BlurHash = "WjQbtJtSO8nhNZ%L_Io#R*oaS6o}-;adXAoIn7j[%hW9s:WGw[nN"
                    }
                });

            data.Add(
                "%MetadataPath%/library/2a/2a27372f1e9bc757b1db99721bbeae1e/poster.jpg*637261226720645297*Primary*0*0|%MetadataPath%/library/2a/2a27372f1e9bc757b1db99721bbeae1e/logo.png*637261226720805297*Logo*0*0|%MetadataPath%/library/2a/2a27372f1e9bc757b1db99721bbeae1e/landscape.jpg*637261226721285297*Thumb*0*0|%MetadataPath%/library/2a/2a27372f1e9bc757b1db99721bbeae1e/backdrop.jpg*637261226721685297*Backdrop*0*0",
                new ItemImageInfo[]
                {
                    new ItemImageInfo()
                    {
                        Path = "/meta/data/path/library/2a/2a27372f1e9bc757b1db99721bbeae1e/poster.jpg",
                        Type = ImageType.Primary,
                        DateModified = new DateTime(637261226720645297, DateTimeKind.Utc),
                    },
                    new ItemImageInfo()
                    {
                        Path = "/meta/data/path/library/2a/2a27372f1e9bc757b1db99721bbeae1e/logo.png",
                        Type = ImageType.Logo,
                        DateModified = new DateTime(637261226720805297, DateTimeKind.Utc),
                    },
                    new ItemImageInfo()
                    {
                        Path = "/meta/data/path/library/2a/2a27372f1e9bc757b1db99721bbeae1e/landscape.jpg",
                        Type = ImageType.Thumb,
                        DateModified = new DateTime(637261226721285297, DateTimeKind.Utc),
                    },
                    new ItemImageInfo()
                    {
                        Path = "/meta/data/path/library/2a/2a27372f1e9bc757b1db99721bbeae1e/backdrop.jpg",
                        Type = ImageType.Backdrop,
                        DateModified = new DateTime(637261226721685297, DateTimeKind.Utc),
                    }
                });

            return data;
        }

        public static TheoryData<string, ItemImageInfo[]> DeserializeImages_ValidAndInvalid_TestData()
        {
            var data = new TheoryData<string, ItemImageInfo[]>();
            data.Add(
                string.Empty,
                Array.Empty<ItemImageInfo>());

            data.Add(
                "/mnt/series/Family Guy/Season 1/Family Guy - S01E01-thumb.jpg*637452096478512963*Primary*1920*1080*WjQbtJtSO8nhNZ%L_Io#R/oaS6o}-;adXAoIn7j[%hW9s:WGw[nN|test|1234||ss",
                new ItemImageInfo[]
                {
                    new()
                    {
                        Path = "/mnt/series/Family Guy/Season 1/Family Guy - S01E01-thumb.jpg",
                        Type = ImageType.Primary,
                        DateModified = new DateTime(637452096478512963, DateTimeKind.Utc),
                        Width = 1920,
                        Height = 1080,
                        BlurHash = "WjQbtJtSO8nhNZ%L_Io#R*oaS6o}-;adXAoIn7j[%hW9s:WGw[nN"
                    }
                });

            data.Add(
                "|",
                Array.Empty<ItemImageInfo>());

            return data;
        }

        [Theory]
        [MemberData(nameof(DeserializeImages_Valid_TestData))]
        public void DeserializeImages_Valid_Success(string value, ItemImageInfo[] expected)
        {
            var result = _sqliteItemRepository.DeserializeImages(value);
            Assert.Equal(expected.Length, result.Length);
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.Equal(expected[i].Path, result[i].Path);
                Assert.Equal(expected[i].Type, result[i].Type);
                Assert.Equal(expected[i].DateModified, result[i].DateModified);
                Assert.Equal(expected[i].Width, result[i].Width);
                Assert.Equal(expected[i].Height, result[i].Height);
                Assert.Equal(expected[i].BlurHash, result[i].BlurHash);
            }
        }

        [Theory]
        [MemberData(nameof(DeserializeImages_ValidAndInvalid_TestData))]
        public void DeserializeImages_ValidAndInvalid_Success(string value, ItemImageInfo[] expected)
        {
            var result = _sqliteItemRepository.DeserializeImages(value);
            Assert.Equal(expected.Length, result.Length);
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.Equal(expected[i].Path, result[i].Path);
                Assert.Equal(expected[i].Type, result[i].Type);
                Assert.Equal(expected[i].DateModified, result[i].DateModified);
                Assert.Equal(expected[i].Width, result[i].Width);
                Assert.Equal(expected[i].Height, result[i].Height);
                Assert.Equal(expected[i].BlurHash, result[i].BlurHash);
            }
        }

        [Theory]
        [MemberData(nameof(DeserializeImages_Valid_TestData))]
        public void SerializeImages_Valid_Success(string expected, ItemImageInfo[] value)
        {
            Assert.Equal(expected, _sqliteItemRepository.SerializeImages(value));
        }

        public static TheoryData<string, Dictionary<string, string>> DeserializeProviderIds_Valid_TestData()
        {
            var data = new TheoryData<string, Dictionary<string, string>>();

            data.Add(
                "Imdb=tt0119567",
                new Dictionary<string, string>()
                {
                    { "Imdb", "tt0119567" },
                });

            data.Add(
                "Imdb=tt0119567|Tmdb=330|TmdbCollection=328",
                new Dictionary<string, string>()
                {
                    { "Imdb", "tt0119567" },
                    { "Tmdb", "330" },
                    { "TmdbCollection", "328" },
                });

            data.Add(
                "MusicBrainzAlbum=9d363e43-f24f-4b39-bc5a-7ef305c677c7|MusicBrainzReleaseGroup=63eba062-847c-3b73-8b0f-6baf27bba6fa|AudioDbArtist=111352|AudioDbAlbum=2116560|MusicBrainzAlbumArtist=20244d07-534f-4eff-b4d4-930878889970",
                new Dictionary<string, string>()
                {
                    { "MusicBrainzAlbum", "9d363e43-f24f-4b39-bc5a-7ef305c677c7" },
                    { "MusicBrainzReleaseGroup", "63eba062-847c-3b73-8b0f-6baf27bba6fa" },
                    { "AudioDbArtist", "111352" },
                    { "AudioDbAlbum", "2116560" },
                    { "MusicBrainzAlbumArtist", "20244d07-534f-4eff-b4d4-930878889970" },
                });

            return data;
        }

        [Theory]
        [MemberData(nameof(DeserializeProviderIds_Valid_TestData))]
        public void DeserializeProviderIds_Valid_Success(string value, Dictionary<string, string> expected)
        {
            var result = new ProviderIdsExtensionsTestsObject();
            SqliteItemRepository.DeserializeProviderIds(value, result);
            Assert.Equal(expected, result.ProviderIds);
        }

        [Theory]
        [MemberData(nameof(DeserializeProviderIds_Valid_TestData))]
        public void SerializeProviderIds_Valid_Success(string expected, Dictionary<string, string> values)
        {
            Assert.Equal(expected, SqliteItemRepository.SerializeProviderIds(values));
        }

        private sealed class ProviderIdsExtensionsTestsObject : IHasProviderIds
        {
            public Dictionary<string, string> ProviderIds { get; set; } = new Dictionary<string, string>();
        }
    }
}
