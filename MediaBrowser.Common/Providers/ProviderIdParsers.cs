using System;
using System.Diagnostics.CodeAnalysis;

namespace MediaBrowser.Common.Providers
{
    /// <summary>
    /// Parsers for provider ids.
    /// </summary>
    public static class ProviderIdParsers
    {
        private const int ImdbMinNumbers = 7;
        private const int ImdbMaxNumbers = 8;
        private const string ImdbPrefix = "tt";

        /// <summary>
        /// Parses an IMDb id from a string.
        /// </summary>
        /// <param name="text">The text to parse.</param>
        /// <param name="imdbId">The parsed IMDb id.</param>
        /// <returns>True if parsing was successful, false otherwise.</returns>
        public static bool TryFindImdbId(ReadOnlySpan<char> text, out ReadOnlySpan<char> imdbId)
        {
            // IMDb id is at least 9 chars (tt + 7 numbers)
            while (text.Length >= 2 + ImdbMinNumbers)
            {
                var ttPos = text.IndexOf(ImdbPrefix);
                if (ttPos == -1)
                {
                    imdbId = default;
                    return false;
                }

                text = text.Slice(ttPos);
                var i = 2;
                var limit = Math.Min(text.Length, ImdbMaxNumbers + 2);
                for (; i < limit; i++)
                {
                    var c = text[i];
                    if (!IsDigit(c))
                    {
                        break;
                    }
                }

                // Skip if more than 8 digits + 2 chars for tt
                if (i <= ImdbMaxNumbers + 2 && i >= ImdbMinNumbers + 2)
                {
                    imdbId = text.Slice(0, i);
                    return true;
                }

                text = text.Slice(i);
            }

            imdbId = default;
            return false;
        }

        /// <summary>
        /// Parses an TMDb id from a movie url.
        /// </summary>
        /// <param name="text">The text with the url to parse.</param>
        /// <param name="tmdbId">The parsed TMDb id.</param>
        /// <returns>True if parsing was successful, false otherwise.</returns>
        public static bool TryFindTmdbMovieId(ReadOnlySpan<char> text, out ReadOnlySpan<char> tmdbId)
            => TryFindProviderId(text, "themoviedb.org/movie/", out tmdbId);

        /// <summary>
        /// Parses an TMDb id from a series url.
        /// </summary>
        /// <param name="text">The text with the url to parse.</param>
        /// <param name="tmdbId">The parsed TMDb id.</param>
        /// <returns>True if parsing was successful, false otherwise.</returns>
        public static bool TryFindTmdbSeriesId(ReadOnlySpan<char> text, out ReadOnlySpan<char> tmdbId)
            => TryFindProviderId(text, "themoviedb.org/tv/", out tmdbId);

        /// <summary>
        /// Parses an TVDb id from a url.
        /// </summary>
        /// <param name="text">The text with the url to parse.</param>
        /// <param name="tvdbId">The parsed TVDb id.</param>
        /// <returns>True if parsing was successful, false otherwise.</returns>
        public static bool TryFindTvdbId(ReadOnlySpan<char> text, out ReadOnlySpan<char> tvdbId)
            => TryFindProviderId(text, "thetvdb.com/?tab=series&id=", out tvdbId);

        private static bool TryFindProviderId(ReadOnlySpan<char> text, ReadOnlySpan<char> searchString, [NotNullWhen(true)] out ReadOnlySpan<char> providerId)
        {
            var searchPos = text.IndexOf(searchString);
            if (searchPos == -1)
            {
                providerId = default;
                return false;
            }

            text = text.Slice(searchPos + searchString.Length);

            int i = 0;
            for (; i < text.Length; i++)
            {
                var c = text[i];

                if (!IsDigit(c))
                {
                    break;
                }
            }

            if (i >= 1)
            {
                providerId = text.Slice(0, i);
                return true;
            }

            providerId = default;
            return false;
        }

        private static bool IsDigit(char c)
        {
            return c >= '0' && c <= '9';
        }
    }
}
