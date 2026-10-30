using System;
using Xunit;

namespace Jellyfin.Extensions.Tests
{
    public class StringExtensionsTests
    {
        [Theory]
        [InlineData("", "")] // Identity edge-case (no diacritics)
        [InlineData("Indiana Jones", "Indiana Jones")] // Identity (no diacritics)
        [InlineData("a\ud800b", "ab")] // Invalid UTF-16 char stripping
        [InlineData("åäö", "aao")] // Issue #7484
        [InlineData("Jön", "Jon")] // Issue #7484
        [InlineData("Jönssonligan", "Jonssonligan")] // Issue #7484
        [InlineData("Kieślowski", "Kieslowski")] // Issue #7450
        [InlineData("Cidadão Kane", "Cidadao Kane")] // Issue #7560
        [InlineData("운명처럼 널 사랑해", "운명처럼 널 사랑해")] // Issue #6393 (Korean language support)
        [InlineData("애타는 로맨스", "애타는 로맨스")] // Issue #6393
        [InlineData("Le cœur a ses raisons", "Le coeur a ses raisons")] // Issue #8893
        [InlineData("Béla Tarr", "Bela Tarr")] // Issue #8893
        public void RemoveDiacritics_ValidInput_Corrects(string input, string expectedResult)
        {
            string result = input.RemoveDiacritics();
            Assert.Equal(expectedResult, result);
        }

        [Theory]
        [InlineData("Indiana Jones", "indiana jones")]
        [InlineData("Zoe Saldaña", "zoe saldana")] // Diacritics are stripped
        [InlineData("Yûki Kaji", "yuki kaji")]
        [InlineData("A.J. Cook", "a j cook")] // Punctuation becomes a separator
        [InlineData("Brian O’Neill", "brian o neill")] // Typographic and ascii apostrophes alike
        [InlineData("Anne-Marie", "anne marie")]
        [InlineData("  Two   spaced\twords  ", "two spaced words")] // Whitespace runs collapse and trim
        [InlineData("!!!", "")] // Nothing but punctuation cleans away entirely
        [InlineData("Apollo 13", "apollo 13")] // Digits are kept
        [InlineData("운명처럼 널 사랑해", "운명처럼 널 사랑해")] // Non-latin letters are kept as-is
        [InlineData("", "")]
        [InlineData("   ", "   ")] // Blank input is returned untouched
        public void GetCleanValue_ValidInput_Corrects(string input, string expectedResult)
        {
            Assert.Equal(expectedResult, input.GetCleanValue());
        }

        [Theory]
        [InlineData("", false)] // Identity edge-case (no diacritics)
        [InlineData("Indiana Jones", false)] // Identity (no diacritics)
        [InlineData("a\ud800b", true)] // Invalid UTF-16 char stripping
        [InlineData("åäö", true)] // Issue #7484
        [InlineData("Jön", true)] // Issue #7484
        [InlineData("Jönssonligan", true)] // Issue #7484
        [InlineData("Kieślowski", true)] // Issue #7450
        [InlineData("Cidadão Kane", true)] // Issue #7560
        [InlineData("운명처럼 널 사랑해", false)] // Issue #6393 (Korean language support)
        [InlineData("애타는 로맨스", false)] // Issue #6393
        [InlineData("Le cœur a ses raisons", true)] // Issue #8893
        [InlineData("Béla Tarr", true)] // Issue #8893
        public void HasDiacritics_ValidInput_Corrects(string input, bool expectedResult)
        {
            bool result = input.HasDiacritics();
            Assert.Equal(expectedResult, result);
        }

        [Theory]
        [InlineData("", '_', 0)]
        [InlineData("___", '_', 3)]
        [InlineData("test\x00", '\x00', 1)]
        [InlineData("Imdb=tt0119567|Tmdb=330|TmdbCollection=328", '|', 2)]
        public void ReadOnlySpan_Count_Success(string str, char needle, int count)
        {
            Assert.Equal(count, str.AsSpan().Count(needle));
        }

        [Theory]
        [InlineData("", 'q', "")]
        [InlineData("Banana split", ' ', "Banana")]
        [InlineData("Banana split", 'q', "Banana split")]
        [InlineData("Banana split 2", ' ', "Banana")]
        public void LeftPart_ValidArgsCharNeedle_Correct(string str, char needle, string expectedResult)
        {
            var result = str.AsSpan().LeftPart(needle).ToString();
            Assert.Equal(expectedResult, result);
        }

        [Theory]
        [InlineData("", 'q', "")]
        [InlineData("Banana split", ' ', "split")]
        [InlineData("Banana split", 'q', "Banana split")]
        [InlineData("Banana split.", '.', "")]
        [InlineData("Banana split 2", ' ', "2")]
        public void RightPart_ValidArgsCharNeedle_Correct(string str, char needle, string expectedResult)
        {
            var result = str.AsSpan().RightPart(needle).ToString();
            Assert.Equal(expectedResult, result);
        }

        [Theory]
        [InlineData("", "")]
        [InlineData("/media/movies/Film.mkv", "/media/movies/Film.mkv")]
        [InlineData(@"C:\media\movies\Film.mkv", @"C:\media\movies\Film.mkv")]
        [InlineData(@"/media/a""b.mkv", @"/media/a\""b.mkv")]
        [InlineData(@"/media/a\""b.mkv", @"/media/a\\\""b.mkv")]
        [InlineData(@"/media/a\\""b.mkv", @"/media/a\\\\\""b.mkv")]
        [InlineData(@"/media/a\b""c.mkv", @"/media/a\b\""c.mkv")]
        [InlineData(@"/media/trailing\", @"/media/trailing\\")]
        [InlineData(@"/media/evil\"" -f lavfi -i sine .mkv", @"/media/evil\\\"" -f lavfi -i sine .mkv")]
        public void EscapeProcessArgument_ValidInput_Corrects(string input, string expectedResult)
        {
            Assert.Equal(expectedResult, input.EscapeProcessArgument());
        }

        [Theory]
        [InlineData("/media/movies/Film with spaces.mkv")]
        [InlineData(@"C:\media\movies\Film.mkv")]
        public void EscapeProcessArgument_NothingToEscape_ReturnsSameInstance(string input)
        {
            Assert.Same(input, input.EscapeProcessArgument());
        }
    }
}
