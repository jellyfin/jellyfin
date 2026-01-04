using System;
using System.Globalization;

namespace MediaBrowser.Providers.Parsers
{
    /// <summary>
    /// Parser for vinyl-style track number formats.
    /// Treats each side as a separate "disc" to avoid collisions (C=disc 3, D=disc 4, etc.).
    /// This matches what Plex and other players do for vinyl compatibility.
    /// </summary>
    public static class VinylTrackNumberParser
    {
        private static readonly CultureInfo InvariantCulture = CultureInfo.InvariantCulture;

        /// <summary>
        /// Parses vinyl-style track numbers and treats each side as a separate disc.
        /// Side letters become disc numbers: A=1, B=2, C=3, D=4, etc.
        /// Examples: "A1" → (1, 1), "B2" → (2, 2), "C15" → (15, 3).
        /// </summary>
        /// <param name="vinylTrack">The vinyl track number string to parse.</param>
        /// <param name="trackNumber">The parsed track number.</param>
        /// <param name="sideAsDiscNumber">The side letter treated as disc number.</param>
        /// <returns><c>true</c> if parsing succeeded; otherwise, <c>false</c>.</returns>
        public static bool TryParseVinylTrackNumber(
            string? vinylTrack,
            out int trackNumber,
            out int sideAsDiscNumber)
        {
            trackNumber = 0;
            sideAsDiscNumber = 1; // Default

            if (string.IsNullOrWhiteSpace(vinylTrack))
            {
                return false;
            }

            string normalizedTrack = vinylTrack.Trim().ToUpperInvariant();

            try
            {
                // Handle standard vinyl formats: [Side Letter][Track Number]
                if (normalizedTrack.Length >= 2 && char.IsLetter(normalizedTrack[0]) && char.IsDigit(normalizedTrack[1]))
                {
                    char sideLetter = normalizedTrack[0];
                    sideAsDiscNumber = char.ToUpper(sideLetter, InvariantCulture) - 'A' + 1;

                    var numericPart = normalizedTrack.Substring(1);

                    if (int.TryParse(numericPart, NumberStyles.Integer, InvariantCulture, out trackNumber))
                    {
                        return true;
                    }
                }

                // Handle reverse vinyl formats: [Track Number][Side Letter]
                if (normalizedTrack.Length >= 2 && char.IsDigit(normalizedTrack[0]) && char.IsLetter(normalizedTrack[^1]))
                {
                    char sideLetter = normalizedTrack[^1];
                    sideAsDiscNumber = char.ToUpper(sideLetter, InvariantCulture) - 'A' + 1;

                    var numericPart = normalizedTrack[..^1];

                    if (int.TryParse(numericPart, NumberStyles.Integer, InvariantCulture, out trackNumber))
                    {
                        return true;
                    }
                }

                // Plain numeric track
                if (int.TryParse(normalizedTrack, NumberStyles.Integer, InvariantCulture, out trackNumber))
                {
                    // For numeric tracks, disc number remains default (1)
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }

            return false;
        }

        /// <summary>
        /// Parses vinyl-style track numbers and extracts all components.
        /// </summary>
        /// <param name="vinylTrack">The track string to parse.</param>
        /// <param name="trackNumber">The parsed track number.</param>
        /// <param name="sideAsDiscNumber">The side as disc number.</param>
        /// <param name="sideLetter">The side letter.</param>
        /// <returns><c>true</c> if parsing succeeded; otherwise, <c>false</c>.</returns>
        public static bool TryParseVinylTrack(
            string? vinylTrack,
            out int trackNumber,
            out int sideAsDiscNumber,
            out char sideLetter)
        {
            trackNumber = 0;
            sideAsDiscNumber = 1;
            sideLetter = 'A';

            if (string.IsNullOrWhiteSpace(vinylTrack))
            {
                return false;
            }

            string normalizedTrack = vinylTrack.Trim().ToUpperInvariant();

            try
            {
                // Handle standard vinyl formats: [Side Letter][Track Number]
                if (normalizedTrack.Length >= 2 && char.IsLetter(normalizedTrack[0]) && char.IsDigit(normalizedTrack[1]))
                {
                    sideLetter = normalizedTrack[0];
                    sideAsDiscNumber = char.ToUpper(sideLetter, InvariantCulture) - 'A' + 1;

                    var numericPart = normalizedTrack.Substring(1);

                    if (int.TryParse(numericPart, NumberStyles.Integer, InvariantCulture, out trackNumber))
                    {
                        return true;
                    }
                }

                // Handle reverse vinyl formats: [Track Number][Side Letter]
                if (normalizedTrack.Length >= 2 && char.IsDigit(normalizedTrack[0]) && char.IsLetter(normalizedTrack[^1]))
                {
                    sideLetter = normalizedTrack[^1];
                    sideAsDiscNumber = char.ToUpper(sideLetter, InvariantCulture) - 'A' + 1;

                    var numericPart = normalizedTrack[..^1];

                    if (int.TryParse(numericPart, NumberStyles.Integer, InvariantCulture, out trackNumber))
                    {
                        return true;
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }

            return false;
        }

        /// <summary>
        /// Checks if a string appears to be in vinyl track number format.
        /// </summary>
        /// <param name="trackNumber">The track number string to check.</param>
        /// <returns><c>true</c> if the string appears to be in vinyl format; otherwise, <c>false</c>.</returns>
        public static bool IsVinylFormat(string? trackNumber)
        {
            if (string.IsNullOrWhiteSpace(trackNumber))
            {
                return false;
            }

            string normalized = trackNumber.Trim().ToUpperInvariant();

            return (normalized.Length >= 2 && char.IsLetter(normalized[0]) && char.IsDigit(normalized[1]))
                || (normalized.Length >= 2 && char.IsDigit(normalized[0]) && char.IsLetter(normalized[^1]));
        }

        /// <summary>
        /// Converts side letter to disc number (A=1, B=2, C=3, etc.).
        /// </summary>
        /// <param name="sideLetter">The side letter.</param>
        /// <returns>The disc number for that side.</returns>
        public static int SideLetterToDiscNumber(char sideLetter)
        {
            return char.ToUpper(sideLetter, InvariantCulture) - 'A' + 1;
        }

        /// <summary>
        /// Creates a display-friendly string showing side as disc.
        /// Example: "Disc C • Track 3" or "Side C • Track 3".
        /// </summary>
        /// <param name="sideLetter">The side letter.</param>
        /// <param name="trackNumber">The track number.</param>
        /// <param name="showAsSide">If true, shows "Side X", otherwise "Disc X".</param>
        /// <returns>A display string.</returns>
        public static string GetDisplayString(char sideLetter, int trackNumber, bool showAsSide = true)
        {
            return showAsSide
                ? $"Side {sideLetter} • Track {trackNumber}"
                : $"Disc {sideLetter} • Track {trackNumber}";
        }
    }
}
