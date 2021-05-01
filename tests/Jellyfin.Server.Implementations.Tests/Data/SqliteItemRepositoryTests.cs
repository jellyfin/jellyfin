using System;
using System.Collections.Generic;
using AutoFixture;
using AutoFixture.AutoMoq;
using Emby.Server.Implementations.Data;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
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

            _fixture = new Fixture().Customize(new AutoMoqCustomization { ConfigureMembers = true });
            _fixture.Inject(appHost);
            _sqliteItemRepository = _fixture.Create<SqliteItemRepository>();
        }

        public static IEnumerable<object[]> ItemImageInfoFromValueString_Valid_TestData()
        {
            yield return new object[]
            {
                "/mnt/series/Family Guy/Season 1/Family Guy - S01E01-thumb.jpg*637452096478512963*Primary*1920*1080*WjQbtJtSO8nhNZ%L_Io#R/oaS6o}-;adXAoIn7j[%hW9s:WGw[nN",
                new ItemImageInfo()
                {
                    Path = "/mnt/series/Family Guy/Season 1/Family Guy - S01E01-thumb.jpg",
                    Type = ImageType.Primary,
                    DateModified = new DateTime(637452096478512963, DateTimeKind.Utc),
                    Width = 1920,
                    Height = 1080,
                    BlurHash = "WjQbtJtSO8nhNZ%L_Io#R*oaS6o}-;adXAoIn7j[%hW9s:WGw[nN"
                }
            };

            yield return new object[]
            {
                "https://image.tmdb.org/t/p/original/zhB5CHEgqqh4wnEqDNJLfWXJlcL.jpg*0*Primary*0*0",
                new ItemImageInfo()
                {
                    Path = "https://image.tmdb.org/t/p/original/zhB5CHEgqqh4wnEqDNJLfWXJlcL.jpg",
                    Type = ImageType.Primary,
                }
            };

            yield return new object[]
            {
                "%MetadataPath%/library/68/68578562b96c80a7ebd530848801f645/poster.jpg*637264380567586027*Primary*600*336",
                new ItemImageInfo()
                {
                    Path = "/meta/data/path/library/68/68578562b96c80a7ebd530848801f645/poster.jpg",
                    Type = ImageType.Primary,
                    DateModified = new DateTime(637264380567586027, DateTimeKind.Utc),
                    Width = 600,
                    Height = 336
                }
            };
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
        public void ItemImageInfoFromValueString_Invalid_Null(string value)
        {
            Assert.Null(_sqliteItemRepository.ItemImageInfoFromValueString(value));
        }

        public static IEnumerable<object[]> DeserializeImages_Valid_TestData()
        {
            yield return new object[]
            {
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
                }
            };

            yield return new object[]
            {
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
                }
            };
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
        [MemberData(nameof(DeserializeImages_Valid_TestData))]
        public void SerializeImages_Valid_Success(string expected, ItemImageInfo[] value)
        {
            Assert.Equal(expected, _sqliteItemRepository.SerializeImages(value));
        }
    }
}
