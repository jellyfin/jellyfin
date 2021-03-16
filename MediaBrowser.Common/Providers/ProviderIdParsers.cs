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

            while (true)
            {
                var ttPos = span.IndexOf(tt);
                if (ttPos == -1)
                {
                    imdbId = default;
                    return false;
                }

                span = span.Slice(ttPos + tt.Length);

                int i = 0;
                // IMDb id has a maximum of 8 digits
                int max = span.Length > 8 ? 8 : span.Length;
                for (; i < max; i++)
                {
                    var c = span[i];

                    if (c < '0' || c > '9')
                    {
                        break;
                    }
                }

                // IMDb id has a minimum of 7 digits
                if (i >= 7)
                {
                    imdbId = string.Concat(tt, span.Slice(0, i));
                    return true;
                }
            }
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

            while (true)
            {
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

                    if (c < '0' || c > '9')
                    {
                        break;
                    }
                }

                if (i >= 1)
                {
                    providerId = span.Slice(0, i).ToString();
                    return true;
                }
            }
        }
    }
}
