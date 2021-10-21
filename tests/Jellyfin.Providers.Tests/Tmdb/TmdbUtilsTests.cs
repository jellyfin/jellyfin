using System.Collections.Generic;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using MediaBrowser.Providers.Plugins.Tmdb;
using TMDbLib.Objects.General;
using Xunit;

namespace Jellyfin.Providers.Tests.Tmdb
{
    public static class TmdbUtilsTests
    {
        [Theory]
        [InlineData("de", "de")]
        [InlineData("En", "En")]
        [InlineData("de-de", "de-DE")]
        [InlineData("en-US", "en-US")]
        [InlineData("de-CH", "de")]
        public static void NormalizeLanguage_Valid_Success(string input, string expected)
        {
            Assert.Equal(expected, TmdbUtils.NormalizeLanguage(input));
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData("", "")]
        public static void NormalizeLanguage_Invalid_Equal(string? input, string? expected)
        {
            Assert.Equal(expected, TmdbUtils.NormalizeLanguage(input!));
        }

        [Theory]
        [InlineData(null, null, null)]
        [InlineData(null, "en-US", null)]
        [InlineData("en", null, "en")]
        [InlineData("en", "en-US", "en-US")]
        [InlineData("fr-CA", "fr-BE", "fr-CA")]
        [InlineData("fr-CA", "fr", "fr-CA")]
        [InlineData("de", "en-US", "de")]
        public static void AdjustImageLanguage_Valid_Success(string imageLanguage, string requestLanguage, string expected)
        {
            Assert.Equal(expected, TmdbUtils.AdjustImageLanguage(imageLanguage, requestLanguage));
        }

        private static TheoryData<ImageType, ImageData, RemoteImageInfo> GetConvertedImages()
        {
            return new TheoryData<ImageType, ImageData, RemoteImageInfo>
            {
                {
                    ImageType.Primary,
                    new ()
                    {
                        Width = 1,
                        Height = 1,
                        AspectRatio = 1,
                        FilePath = "path 1",
                        Iso_639_1 = "en",
                        VoteAverage = 1.2,
                        VoteCount = 5
                    },
                    new ()
                    {
                        Type = ImageType.Primary,
                        Width = 1,
                        Height = 1,
                        Url = "converted path 1",
                        Language = "en-US",
                        CommunityRating = 1.2,
                        VoteCount = 5,
                        RatingType = RatingType.Score,
                        ProviderName = TmdbUtils.ProviderName
                    }
                },
                {
                    ImageType.Backdrop,
                    new ()
                    {
                        Width = 4,
                        Height = 2,
                        AspectRatio = 2,
                        FilePath = "path 2",
                        Iso_639_1 = null,
                        VoteAverage = 0,
                        VoteCount = 0
                    },
                    new ()
                    {
                        Type = ImageType.Backdrop,
                        Width = 4,
                        Height = 2,
                        Url = "converted path 2",
                        Language = null,
                        CommunityRating = 0,
                        VoteCount = 0,
                        RatingType = RatingType.Score,
                        ProviderName = TmdbUtils.ProviderName
                    }
                }
            };
        }

        [Theory]
        [MemberData(nameof(GetConvertedImages))]
        public static void ConvertToRemoteImageInfo_ImageList_ConvertsAll(ImageType type, ImageData input, RemoteImageInfo expected)
        {
            var images = new List<ImageData> { input };
            string UrlConverter(string s)
                => "converted " + s;
            var language = "en-US";

            var results = new List<RemoteImageInfo>(images.Count);
            TmdbUtils.ConvertToRemoteImageInfo(images, UrlConverter, type, language, results);

            Assert.Single(results);

            Assert.Equal(expected.Type, results[0].Type);
            Assert.Equal(expected.Width, results[0].Width);
            Assert.Equal(expected.Height, results[0].Height);
            Assert.Equal(expected.Url, results[0].Url);
            Assert.Equal(expected.Language, results[0].Language);
            Assert.Equal(expected.CommunityRating, results[0].CommunityRating);
            Assert.Equal(expected.VoteCount, results[0].VoteCount);
            Assert.Equal(expected.RatingType, results[0].RatingType);
            Assert.Equal(expected.ProviderName, results[0].ProviderName);
        }
    }
}
