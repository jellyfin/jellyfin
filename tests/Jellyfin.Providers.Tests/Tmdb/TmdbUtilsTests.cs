using System;
using System.Collections.Generic;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Model.Entities;
using MediaBrowser.Providers.Plugins.Tmdb;
using TMDbLib.Objects.Search;
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
        [InlineData("en", "en-US", "en-US")]
        [InlineData("fr-CA", "fr-BE", "fr-CA")]
        [InlineData("fr-CA", "fr", "fr-CA")]
        [InlineData("de", "en-US", "de")]
        [InlineData("", "en-US", "")]
        public static void AdjustImageLanguage_Valid_Success(string imageLanguage, string requestLanguage, string? expected)
        {
            Assert.Equal(expected, TmdbUtils.AdjustImageLanguage(imageLanguage, requestLanguage));
        }

        [Theory]
        [InlineData("11", true, 11)]
        // An id another provider filed under the TMDb key must not throw, it is simply not a TMDb id.
        [InlineData("nm0000123", false, 0)]
        [InlineData("tt0113375", false, 0)]
        [InlineData("11.0", false, 0)]
        [InlineData("-11", false, 0)]
        [InlineData("0", false, 0)]
        [InlineData("", false, 0)]
        [InlineData(null, false, 0)]
        public static void TryParseTmdbId_OnlyAcceptsTmdbIds(string? value, bool expected, int expectedId)
        {
            Assert.Equal(expected, TmdbUtils.TryParseTmdbId(value, out var tmdbId));
            Assert.Equal(expectedId, tmdbId);
        }

        [Theory]
        [InlineData("11", true, 11)]
        [InlineData("nm0000123", false, 0)]
        public static void TryGetTmdbId_OnlyAcceptsTmdbIds(string value, bool expected, int expectedId)
        {
            var item = new Movie();
            item.ProviderIds[MetadataProvider.Tmdb.ToString()] = value;

            Assert.Equal(expected, item.TryGetTmdbId(out var tmdbId));
            Assert.Equal(expectedId, tmdbId);
        }

        [Fact]
        public static void TryGetTmdbId_NoId_False()
        {
            Assert.False(new Movie().TryGetTmdbId(out var tmdbId));
            Assert.Equal(0, tmdbId);
        }

        [Theory]
        [InlineData("The Amityville Horror", "The Amityville Horror")]
        [InlineData("WALL-E", "WALL E")]
        // The interpunct is kept, it matches the TMDb title better than a space does.
        [InlineData("WALL·E", "WALL·E")]
        [InlineData("50-50", "50 50")]
        [InlineData("A Christmas No. 1", "A Christmas No 1")]
        // Vulgar fractions are numbers, dropping them turned "8½" into a search for "8".
        [InlineData("8½", "8½")]
        [InlineData("9½ Weeks", "9½ Weeks")]
        [InlineData("  Léon: The Professional  ", "Léon The Professional")]
        public static void CleanName_Valid_Success(string name, string expected)
        {
            Assert.Equal(expected, TmdbUtils.CleanName(name));
        }

        [Theory]
        [InlineData("WALL-E", "wall e")]
        [InlineData("WALL·E", "wall e")]
        [InlineData("WALL E", "wall e")]
        [InlineData("8½", "8½")]
        [InlineData("Ocean's Eleven", "ocean s eleven")]
        [InlineData(null, "")]
        [InlineData("   ", "")]
        public static void NormalizeTitle_Valid_Success(string? title, string expected)
        {
            Assert.Equal(expected, TmdbUtils.NormalizeTitle(title));
        }

        [Theory]
        // An unconfigured size fetches the original image, so it keeps the original resolution.
        [InlineData(null, true)]
        [InlineData("", true)]
        [InlineData("original", true)]
        [InlineData("Original", true)]
        [InlineData("w500", false)]
        [InlineData("original2", false)]
        public static void IsOriginalImageSize_Valid_Success(string? size, bool expected)
        {
            Assert.Equal(expected, TmdbUtils.IsOriginalImageSize(size));
        }

        [Theory]
        [MemberData(nameof(FindBestMatch_Movies_TestData))]
        public static void FindBestMatch_Movies_PicksExpected(string description, string name, int year, IReadOnlyList<SearchMovie> results, int expectedId)
        {
            var match = TmdbUtils.FindBestMatch(results, name, year);

            Assert.NotNull(match);
            Assert.True(expectedId == match.Id, $"{description}: expected {expectedId} but matched {match.Id}");
        }

        [Fact]
        public static void FindBestMatch_Series_PicksMatchingFirstAirYear()
        {
            IReadOnlyList<SearchTv> results =
            [
                Series(10042, "Doc", "Doc", 2001),
                Series(101048, "Doc", "Doc", 2020),
                Series(255055, "Doc", "Doc", 2025),
                Series(2430, "Doc Martin", "Doc Martin", 2004)
            ];

            var match = TmdbUtils.FindBestMatch(results, "Doc", 2025);

            Assert.NotNull(match);
            Assert.Equal(255055, match.Id);
        }

        [Fact]
        public static void FindBestMatch_NoResults_ReturnsNull()
        {
            Assert.Null(TmdbUtils.FindBestMatch(Array.Empty<SearchMovie>(), "Mulan", 2020));
            Assert.Null(TmdbUtils.FindBestMatch(Array.Empty<SearchTv>(), "Doc", 2025));
            Assert.Null(TmdbUtils.FindBestMatch((IReadOnlyList<SearchMovie>?)null, "Mulan", 2020));
            Assert.Null(TmdbUtils.FindBestMatch((IReadOnlyList<SearchTv>?)null, "Doc", 2025));
        }

        public static TheoryData<string, string, int, IReadOnlyList<SearchMovie>, int> FindBestMatch_Movies_TestData()
            => new()
            {
                // TMDb's year parameter does not filter, so the remake and the original both come back and
                // the wrong one is first. Results are in the order the live API returned them.
                {
                    "Mulan (2020)", "Mulan", 2020,
                    [Movie(10674, "Mulan", "Mulan", 1998), Movie(337401, "Mulan", "Mulan", 2020), Movie(752662, "Hua Mulan", "花木兰", 2020)],
                    337401
                },
                {
                    "Mulan (1998)", "Mulan", 1998,
                    [Movie(10674, "Mulan", "Mulan", 1998), Movie(337401, "Mulan", "Mulan", 2020), Movie(752662, "Hua Mulan", "花木兰", 2020)],
                    10674
                },
                {
                    "Aladdin (2019)", "Aladdin", 2019,
                    [Movie(812, "Aladdin", "Aladdin", 1992), Movie(420817, "Aladdin", "Aladdin", 2019), Movie(602411, "Adventures of Aladdin", "Adventures of Aladdin", 2019)],
                    420817
                },
                {
                    "The Lion King (2019)", "The Lion King", 2019,
                    [Movie(8587, "The Lion King", "The Lion King", 1994), Movie(420818, "The Lion King", "The Lion King", 2019)],
                    420818
                },
                {
                    "The Amityville Horror (1979)", "The Amityville Horror", 1979,
                    [Movie(10065, "The Amityville Horror", "The Amityville Horror", 2005), Movie(11449, "The Amityville Horror", "The Amityville Horror", 1979)],
                    11449
                },
                // A featurette outranks the film it belongs to. The interpunct must not stop "WALL-E" from
                // matching "WALL·E", or the prefix match on the featurette wins.
                {
                    "WALL-E (2008)", "WALL-E", 2008,
                    [Movie(877268, "WALL·E's Treasures & Trinkets", "WALL·E's Treasures & Trinkets", 2008), Movie(10681, "WALL·E", "WALL·E", 2008), Movie(10673, "Wall Street", "Wall Street", 1987)],
                    10681
                },
                // The name only survives as "8" if the fraction is stripped, and then every 1963 result ties.
                {
                    "8½ (1963)", "8½", 1963,
                    [Movie(422801, "Interpol Code 8", "国際秘密警察　指令第８号", 1963), Movie(520251, "Um 8 Uhr kommt Sadowski", "Um 8 Uhr kommt Sadowski", 1963), Movie(422, "8½", "8½", 1963)],
                    422
                },
                // Matched on the original title, the localized one is unrecognizable.
                {
                    "Ściany mają uszy (1966)", "Ściany mają uszy", 1966,
                    [Movie(1, "Something Else", "Something Else", 1966), Movie(2, "Walls Have Ears", "Ściany mają uszy", 1966)],
                    2
                },
                // Regional release dates straddle the new year, so a year that is off by one still matches.
                {
                    "Off by one year", "Some Movie", 2011,
                    [Movie(1, "Some Movie", "Some Movie", 2015), Movie(2, "Some Movie", "Some Movie", 2010)],
                    2
                },
                // Nothing matches the name, so TMDb's own ordering is kept.
                {
                    "A Christmas No. 1 (2021)", "A Christmas No. 1", 2021,
                    [Movie(878111, "A Christmas Number One", "A Christmas Number One", 2021), Movie(2, "Ten Hours for Christmas", "10 Horas para o Natal", 2021)],
                    878111
                },
                // A title that matches always beats one that only shares the year.
                {
                    "Title outranks year", "Some Movie", 2020,
                    [Movie(1, "A Different Movie", "A Different Movie", 2020), Movie(2, "Some Movie", "Some Movie", 1994)],
                    2
                },
                // Without a year the title alone decides, and equally good titles keep TMDb's order.
                {
                    "No year known", "Mulan", 0,
                    [Movie(10674, "Mulan", "Mulan", 1998), Movie(337401, "Mulan", "Mulan", 2020)],
                    10674
                },
                // An unparsable name must not throw or reorder anything.
                {
                    "Empty name", "  ", 2020,
                    [Movie(1, "Some Movie", "Some Movie", 1994), Movie(2, "Some Movie", "Some Movie", 2020)],
                    1
                }
            };

        private static SearchMovie Movie(int id, string title, string originalTitle, int year)
            => new()
            {
                Id = id,
                Title = title,
                OriginalTitle = originalTitle,
                ReleaseDate = new DateTime(year, 6, 1, 0, 0, 0, DateTimeKind.Utc)
            };

        private static SearchTv Series(int id, string name, string originalName, int year)
            => new()
            {
                Id = id,
                Name = name,
                OriginalName = originalName,
                FirstAirDate = new DateTime(year, 6, 1, 0, 0, 0, DateTimeKind.Utc)
            };
    }
}
