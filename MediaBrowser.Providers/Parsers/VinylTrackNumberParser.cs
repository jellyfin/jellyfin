using System;
using System.Globalization;

namespace MediaBrowser.Providers.Parsers
{
    /// <summary>
    /// Parser for vinyl-style track number formats.
    /// </summary>
    public static class VinylTrackNumberParser
    {
        private static readonly CultureInfo InvariantCulture = CultureInfo.InvariantCulture;

        /// <summary>
        /// Attempts to parse vinyl-style track numbers from string representations.
        /// Returns both the track number and the side as a disc number.
        /// Examples: "A1" → (1, 1), "B2" → (2, 2), "C15" → (15, 3).
        /// </summary>
        /// <param name="vinylTrack">The vinyl track number string to parse.</param>
        /// <param name="trackNumber">The parsed track number.</param>
        /// <param name="sideNumber">The parsed side number (A=1, B=2, C=3, etc.).</param>
        /// <returns><c>true</c> if parsing succeeded; otherwise, <c>false</c>.</returns>
        public static bool TryParseVinylTrackNumber(
            string? vinylTrack,
            out int trackNumber,
            out int sideNumber)
        {
            trackNumber = 0;
            sideNumber = 1; // Default to side A

            if (string.IsNullOrWhiteSpace(vinylTrack))
            {
                return false;
            }

            string normalizedTrack = vinylTrack.Trim().ToUpperInvariant();

            try
            {
                // Handle standard vinyl formats: [Side Letter][Track Number]
                // Examples: A1, B2, A01, B02, C15
                if (normalizedTrack.Length >= 2 && char.IsLetter(normalizedTrack[0]) && char.IsDigit(normalizedTrack[1]))
                {
                    // Extract side letter and convert to number (A=1, B=2, C=3, etc.)
                    char sideLetter = normalizedTrack[0];
                    sideNumber = char.ToUpper(sideLetter, InvariantCulture) - 'A' + 1;

                    var numericPart = normalizedTrack.Substring(1);

                    if (int.TryParse(numericPart, NumberStyles.Integer, InvariantCulture, out int trackOnSide))
                    {
                        // Extract just the track number within the side
                        trackNumber = trackOnSide;
                        return true;
                    }
                }

                // Handle reverse vinyl formats: [Track Number][Side Letter]
                // Examples: 1A, 2B, 01A, 02B
                if (normalizedTrack.Length >= 2 && char.IsDigit(normalizedTrack[0]) && char.IsLetter(normalizedTrack[^1]))
                {
                    // Extract side letter and convert to number
                    char sideLetter = normalizedTrack[^1];
                    sideNumber = char.ToUpper(sideLetter, InvariantCulture) - 'A' + 1;

                    var numericPart = normalizedTrack[..^1];

                    if (int.TryParse(numericPart, NumberStyles.Integer, InvariantCulture, out int trackOnSide))
                    {
                        trackNumber = trackOnSide;
                        return true;
                    }
                }

                // Final attempt: try parsing as plain numeric track number
                if (int.TryParse(normalizedTrack, NumberStyles.Integer, InvariantCulture, out trackNumber))
                {
                    return true;
                }
            }
            catch (Exception)
            {
                // Parsing failed
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

            // Check for standard format: A1, B2, C15
            if (normalized.Length >= 2 && char.IsLetter(normalized[0]) && char.IsDigit(normalized[1]))
            {
                return true;
            }

            // Check for reverse format: 1A, 2B, 01A
            if (normalized.Length >= 2 && char.IsDigit(normalized[0]) && char.IsLetter(normalized[^1]))
            {
                return true;
            }

            return false;
        }
    }
}
