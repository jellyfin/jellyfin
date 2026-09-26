using System;
using Jellyfin.Server.Migrations.Routines;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using Xunit;

namespace Jellyfin.Server.Tests.Migrations
{
    public class MigrateLibraryDbImageParsingTests
    {
        public static TheoryData<string, ItemImageInfo[]> DeserializeImages_TestData()
        {
            var data = new TheoryData<string, ItemImageInfo[]>();

            data.Add(
                "/mnt/series/Family Guy/Season 1/Family Guy - S01E01-thumb.jpg*637452096478512963*Primary*1920*1080*WjQbtJtSO8nhNZ%L_Io#R/oaS6o}-;adXAoIn7j[%hW9s:WGw[nN",
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
                "%MetadataPath%/library/2a/2a27372f1e9bc757b1db99721bbeae1e/poster.jpg*637261226720645297*Primary*0*0|%MetadataPath%/library/2a/2a27372f1e9bc757b1db99721bbeae1e/logo.png*637261226720805297*Logo*0*0",
                new ItemImageInfo[]
                {
                    new()
                    {
                        Path = "%MetadataPath%/library/2a/2a27372f1e9bc757b1db99721bbeae1e/poster.jpg",
                        Type = ImageType.Primary,
                        DateModified = new DateTime(637261226720645297, DateTimeKind.Utc),
                    },
                    new()
                    {
                        Path = "%MetadataPath%/library/2a/2a27372f1e9bc757b1db99721bbeae1e/logo.png",
                        Type = ImageType.Logo,
                        DateModified = new DateTime(637261226720805297, DateTimeKind.Utc),
                    }
                });

            // Path containing '|' (https://github.com/jellyfin/jellyfin/issues/10375):
            // '|' only separates records once the path*date*type fields are complete.
            data.Add(
                "/media/foo | bar/cover.jpg*637452096478512963*Primary",
                new ItemImageInfo[]
                {
                    new()
                    {
                        Path = "/media/foo | bar/cover.jpg",
                        Type = ImageType.Primary,
                        DateModified = new DateTime(637452096478512963, DateTimeKind.Utc),
                    }
                });

            data.Add(
                "/media/a|b|c/cover.jpg*637452096478512963*Primary*600*336",
                new ItemImageInfo[]
                {
                    new()
                    {
                        Path = "/media/a|b|c/cover.jpg",
                        Type = ImageType.Primary,
                        DateModified = new DateTime(637452096478512963, DateTimeKind.Utc),
                        Width = 600,
                        Height = 336,
                    }
                });

            data.Add(
                "/media/plain/cover.jpg*637452096478512963*Primary|/media/foo | bar/backdrop.jpg*637452096478512964*Backdrop|/media/plain/logo.png*637452096478512965*Logo",
                new ItemImageInfo[]
                {
                    new()
                    {
                        Path = "/media/plain/cover.jpg",
                        Type = ImageType.Primary,
                        DateModified = new DateTime(637452096478512963, DateTimeKind.Utc),
                    },
                    new()
                    {
                        Path = "/media/foo | bar/backdrop.jpg",
                        Type = ImageType.Backdrop,
                        DateModified = new DateTime(637452096478512964, DateTimeKind.Utc),
                    },
                    new()
                    {
                        Path = "/media/plain/logo.png",
                        Type = ImageType.Logo,
                        DateModified = new DateTime(637452096478512965, DateTimeKind.Utc),
                    }
                });

            // A blurhash containing an escaped '|' (stored as '\') must not end the record early
            // and must round-trip back to '|'.
            data.Add(
                "/media/foo | bar/cover.jpg*637452096478512963*Primary*1920*1080*Wj\\QbtJtSO8nhNZ%L",
                new ItemImageInfo[]
                {
                    new()
                    {
                        Path = "/media/foo | bar/cover.jpg",
                        Type = ImageType.Primary,
                        DateModified = new DateTime(637452096478512963, DateTimeKind.Utc),
                        Width = 1920,
                        Height = 1080,
                        BlurHash = "Wj|QbtJtSO8nhNZ%L",
                    }
                });

            data.Add(string.Empty, Array.Empty<ItemImageInfo>());

            data.Add("|", Array.Empty<ItemImageInfo>());

            // Trailing garbage that never completes a record is dropped.
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

            return data;
        }

        [Theory]
        [MemberData(nameof(DeserializeImages_TestData))]
        public void DeserializeImages_ParsesRecords(string value, ItemImageInfo[] expected)
        {
            var result = MigrateLibraryDb.DeserializeImages(value);

            Assert.Equal(expected.Length, result.Length);
            for (var i = 0; i < expected.Length; i++)
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
        [InlineData("")]
        [InlineData("/path/no/delimiters.jpg")]
        [InlineData("/path/only/one*637452096478512963")]
        [InlineData("/path/bad/date*notadate*Primary")]
        [InlineData("/path/bad/type*637452096478512963*NotAnImageType")]
        public void ItemImageInfoFromValueString_Invalid_ReturnsNull(string value)
        {
            Assert.Null(MigrateLibraryDb.ItemImageInfoFromValueString(value));
        }
    }
}
