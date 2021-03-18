#nullable enable

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

        /// <summary>
        /// Parses an IMDb id from a string.
        /// </summary>
        /// <param name="text">The text to parse.</param>
        /// <param name="imdbId">The parsed IMDb id.</param>
        /// <returns>True if parsing was successful, false otherwise.</returns>
        public static bool TryParseImdbId(string text, [NotNullWhen(true)] out string? imdbId)
        {
            var span = text.AsSpan();
            var tt = "tt".AsSpan();

            // imdb id is at least 9 chars (tt + 7 numbers)
            while (span.Length >= 2 + ImdbMinNumbers)
            {
                var ttPos = span.IndexOf(tt);
                if (ttPos == -1)
                {
                    imdbId = default;
                    return false;
                }

                span = span.Slice(ttPos + tt.Length);
                var i = 0;
                for (; i < Math.Min(span.Length, ImdbMaxNumbers); i++)
                {
                    var c = span[i];
                    if (!IsDigit(c))
                    {
                        break;
                    }
                }

                // skip if more than 8 digits
                if (i <= ImdbMaxNumbers && i >= ImdbMinNumbers)
                {
                    imdbId = string.Concat(tt, span.Slice(0, i));
                    return true;
                }

                span = span.Slice(i);
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
        public static bool TryParseTmdbMovieId(string text, [NotNullWhen(true)] out string? tmdbId)
            => TryParseProviderId(text, "themoviedb.org/movie/", out tmdbId);

        /// <summary>
        /// Parses an TMDb id from a series url.
        /// </summary>
        /// <param name="text">The text with the url to parse.</param>
        /// <param name="tmdbId">The parsed TMDb id.</param>
        /// <returns>True if parsing was successful, false otherwise.</returns>
        public static bool TryParseTmdbSeriesId(string text, [NotNullWhen(true)] out string? tmdbId)
            => TryParseProviderId(text, "themoviedb.org/tv/", out tmdbId);

        /// <summary>
        /// Parses an TVDb id from a url.
        /// </summary>
        /// <param name="text">The text with the url to parse.</param>
        /// <param name="tvdbId">The parsed TVDb id.</param>
        /// <returns>True if parsing was successful, false otherwise.</returns>
        public static bool TryParseTvdbId(string text, [NotNullWhen(true)] out string? tvdbId)
            => TryParseProviderId(text, "thetvdb.com/?tab=series&id=", out tvdbId);

        private static bool TryParseProviderId(string text, string searchString, [NotNullWhen(true)] out string? providerId)
        {
            var span = text.AsSpan();
            var searchSpan = searchString.AsSpan();

            var searchPos = span.IndexOf(searchSpan);
            if (searchPos == -1)
            {
                providerId = default;
                return false;
            }

            span = span.Slice(searchPos + searchSpan.Length);

            int i = 0;
            for (; i < span.Length; i++)
            {
                var c = span[i];

                if (!IsDigit(c))
                {
                    break;
                }
            }

            if (i >= 1)
            {
                providerId = span.Slice(0, i).ToString();
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
