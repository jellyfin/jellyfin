using System;
using System.Collections.Generic;
using AutoFixture;
using AutoFixture.AutoMoq;
using Emby.Server.Implementations.Data;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Persistence;
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
        private readonly BaseItemRepository _sqliteItemRepository;

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
            _sqliteItemRepository = _fixture.Create<BaseItemRepository>();
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

        private sealed class ProviderIdsExtensionsTestsObject : IHasProviderIds
        {
            public Dictionary<string, string> ProviderIds { get; set; } = new Dictionary<string, string>();
        }
    }
}
