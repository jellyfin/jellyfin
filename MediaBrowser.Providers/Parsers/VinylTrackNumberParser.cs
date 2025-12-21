using System;
using System.Globalization;

namespace MediaBrowser.Providers.Parsers
{
    /// <summary>
    /// Parser for vinyl-style track number formats.
    /// Extracts track number and optionally infers disc number when DISCNUMBER is missing.
    /// WARNING: Side letter information is discarded due to schema limitations.
    /// </summary>
    public static class VinylTrackNumberParser
    {
        private const int SidesPerDisc = 2;
        private static readonly CultureInfo InvariantCulture = CultureInfo.InvariantCulture;

        /// <summary>
        /// Parses vinyl-style track numbers, extracting track number and inferring disc number when needed.
        /// Side letter information is parsed but discarded due to lack of storage field.
        /// </summary>
        /// <param name="vinylTrack">The track string to parse (e.g., "C3").</param>
        /// <param name="existingDiscNumber">Optional disc number from DISCNUMBER tag.</param>
        /// <param name="trackNumber">The parsed track number within the side.</param>
        /// <param name="finalDiscNumber">The disc number to use (existing tag or inferred).</param>
        /// <returns><c>true</c> if parsing succeeded; otherwise, <c>false</c>.</returns>
        public static bool TryParseVinylTrack(
            string? vinylTrack,
            int? existingDiscNumber,
            out int trackNumber,
            out int finalDiscNumber)
        {
            trackNumber = 0;
            finalDiscNumber = existingDiscNumber ?? 1;

            if (string.IsNullOrWhiteSpace(vinylTrack))
            {
                return false;
            }

            string normalizedTrack = vinylTrack.Trim().ToUpperInvariant();

            try
            {
                char? sideLetter = null;
                int? sideNumber = null;

                // Parse and extract side information (then discard it)
                if (normalizedTrack.Length >= 2 && char.IsLetter(normalizedTrack[0]) && char.IsDigit(normalizedTrack[1]))
                {
                    sideLetter = normalizedTrack[0];
                    sideNumber = char.ToUpper(sideLetter.Value, InvariantCulture) - 'A' + 1;

                    var numericPart = normalizedTrack.Substring(1);
                    if (int.TryParse(numericPart, NumberStyles.Integer, InvariantCulture, out trackNumber))
                    {
                        // SIDE LETTER 'C' IS PARSED HERE BUT THEN DISCARDED
                        ApplyDiscNumberLogic(ref finalDiscNumber, existingDiscNumber, sideNumber);
                        return true;
                    }
                }

                if (normalizedTrack.Length >= 2 && char.IsDigit(normalizedTrack[0]) && char.IsLetter(normalizedTrack[^1]))
                {
                    sideLetter = normalizedTrack[^1];
                    sideNumber = char.ToUpper(sideLetter.Value, InvariantCulture) - 'A' + 1;

                    var numericPart = normalizedTrack[..^1];
                    if (int.TryParse(numericPart, NumberStyles.Integer, InvariantCulture, out trackNumber))
                    {
                        // SIDE LETTER 'C' IS PARSED HERE BUT NOT USED
                        ApplyDiscNumberLogic(ref finalDiscNumber, existingDiscNumber, sideNumber);
                        return true;
                    }
                }

                // Plain numeric track
                if (int.TryParse(normalizedTrack, NumberStyles.Integer, InvariantCulture, out trackNumber))
                {
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
        /// Backward-compatible method (discouraged).
        /// WARNING: Always infers disc number, which may override existing DISCNUMBER tags.
        /// Use TryParseVinylTrack instead for proper DISCNUMBER handling.
        /// </summary>
        /// <param name="vinylTrack">The vinyl track number string to parse.</param>
        /// <param name="trackNumber">The parsed track number.</param>
        /// <param name="inferredDiscNumber">The disc number inferred from the side letter.</param>
        /// <returns><c>true</c> if parsing succeeded; otherwise, <c>false</c>.</returns>
        public static bool TryParseVinylTrackNumber(
            string? vinylTrack,
            out int trackNumber,
            out int inferredDiscNumber)
        {
            // Always infers disc number (legacy behavior)
            return TryParseVinylTrack(vinylTrack, null, out trackNumber, out inferredDiscNumber);
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

        /// <summary>
        /// Creates display string showing what information was lost.
        /// Example: "Track 3 (Side C information lost due to schema limitations)".
        /// </summary>
        /// <param name="original">The original track string.</param>
        /// <param name="trackNum">The parsed track number.</param>
        /// <param name="discNum">The disc number used.</param>
        /// <returns>A display string acknowledging information loss.</returns>
        public static string GetLossAwareDisplayString(string? original, int trackNum, int discNum)
        {
            if (IsVinylFormat(original))
            {
                return $"Track {trackNum} (Disc {discNum}) - Side information from '{original}' was lost";
            }

            return $"Track {trackNum} (Disc {discNum})";
        }

        /// <summary>
        /// Applies disc number logic: uses existing tag if available, otherwise infers from side.
        /// </summary>
        /// <param name="finalDiscNumber">The disc number to modify.</param>
        /// <param name="existingDiscNumber">Optional disc number from DISCNUMBER tag.</param>
        /// <param name="sideNumber">The side number if available (A=1, B=2, etc.).</param>
        private static void ApplyDiscNumberLogic(ref int finalDiscNumber, int? existingDiscNumber, int? sideNumber)
        {
            // If no existing disc number AND we have side info, infer from side
            if (!existingDiscNumber.HasValue && sideNumber.HasValue)
            {
                finalDiscNumber = ((sideNumber.Value - 1) / SidesPerDisc) + 1;
            }
        }
    }
}
