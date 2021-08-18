using System;
using System.Globalization;
using System.Text;

namespace Jellyfin.Api.Helpers
{
    /// <summary>
    /// Hls Codec string helpers.
    /// </summary>
    public static class HlsCodecStringHelpers
    {
        /// <summary>
        /// Codec name for MP3.
        /// </summary>
        public const string MP3 = "mp4a.40.34";

        /// <summary>
        /// Codec name for AC-3.
        /// </summary>
        public const string AC3 = "mp4a.a5";

        /// <summary>
        /// Codec name for E-AC-3.
        /// </summary>
        public const string EAC3 = "mp4a.a6";

        /// <summary>
        /// Codec name for FLAC.
        /// </summary>
        public const string FLAC = "fLaC";

        /// <summary>
        /// Codec name for ALAC.
        /// </summary>
        public const string ALAC = "alac";

        /// <summary>
        /// Gets a MP3 codec string.
        /// </summary>
        /// <returns>MP3 codec string.</returns>
        public static string GetMP3String()
        {
            return MP3;
        }

        /// <summary>
        /// Gets an AAC codec string.
        /// </summary>
        /// <param name="profile">AAC profile.</param>
        /// <returns>AAC codec string.</returns>
        public static string GetAACString(string? profile)
        {
            StringBuilder result = new StringBuilder("mp4a", 9);

            if (string.Equals(profile, "HE", StringComparison.OrdinalIgnoreCase))
            {
                result.Append(".40.5");
            }
            else
            {
                // Default to LC if profile is invalid
                result.Append(".40.2");
            }

            return result.ToString();
        }

        /// <summary>
        /// Gets an AC-3 codec string.
        /// </summary>
        /// <returns>AC-3 codec string.</returns>
        public static string GetAC3String()
        {
            return AC3;
        }

        /// <summary>
        /// Gets an E-AC-3 codec string.
        /// </summary>
        /// <returns>E-AC-3 codec string.</returns>
        public static string GetEAC3String()
        {
            return EAC3;
        }

        /// <summary>
        /// Gets an FLAC codec string.
        /// </summary>
        /// <returns>FLAC codec string.</returns>
        public static string GetFLACString()
        {
            return FLAC;
        }

        /// <summary>
        /// Gets an ALAC codec string.
        /// </summary>
        /// <returns>ALAC codec string.</returns>
        public static string GetALACString()
        {
            return ALAC;
        }

        /// <summary>
        /// Gets a H.264 codec string.
        /// </summary>
        /// <param name="profile">H.264 profile.</param>
        /// <param name="level">H.264 level.</param>
        /// <returns>H.264 string.</returns>
        public static string GetH264String(string? profile, int level)
        {
            StringBuilder result = new StringBuilder("avc1", 11);

            if (string.Equals(profile, "high", StringComparison.OrdinalIgnoreCase))
            {
                result.Append(".6400");
            }
            else if (string.Equals(profile, "main", StringComparison.OrdinalIgnoreCase))
            {
                result.Append(".4D40");
            }
            else if (string.Equals(profile, "baseline", StringComparison.OrdinalIgnoreCase))
            {
                result.Append(".42E0");
            }
            else
            {
                // Default to constrained baseline if profile is invalid
                result.Append(".4240");
            }

            string levelHex = level.ToString("X2", CultureInfo.InvariantCulture);
            result.Append(levelHex);

            return result.ToString();
        }

        /// <summary>
        /// Gets a H.265 codec string.
        /// </summary>
        /// <param name="profile">H.265 profile.</param>
        /// <param name="level">H.265 level.</param>
        /// <returns>H.265 string.</returns>
        public static string GetH265String(string? profile, int level)
        {
            // The h265 syntax is a bit of a mystery at the time this comment was written.
            // This is what I've found through various sources:
            // FORMAT: [codecTag].[profile].[constraint?].L[level * 30].[UNKNOWN]
            StringBuilder result = new StringBuilder("hvc1", 16);

            if (string.Equals(profile, "main10", StringComparison.OrdinalIgnoreCase)
                || string.Equals(profile, "main 10", StringComparison.OrdinalIgnoreCase))
            {
                result.Append(".2.4");
            }
            else
            {
                // Default to main if profile is invalid
                result.Append(".1.4");
            }

            result.Append(".L")
                .Append(level)
                .Append(".B0");

            return result.ToString();
        }
    }
}
