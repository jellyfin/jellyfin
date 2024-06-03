using System;
using System.Linq;
using MediaBrowser.Model.Entities;

namespace Emby.Naming.TV;

/// <summary>
/// Helper class for TV metadata parsing.
/// </summary>
public static class TvParserHelpers
{
    private static readonly string[] _continuingState = ["Pilot", "Returning Series", "Returning"];
    private static readonly string[] _endedState = ["Cancelled", "Canceled"];

    /// <summary>
    /// Tries to parse a string into <see cref="SeriesStatus"/>.
    /// </summary>
    /// <param name="status">The status string.</param>
    /// <param name="enumValue">The <see cref="SeriesStatus"/>.</param>
    /// <returns>Returns true if parsing was successful.</returns>
    public static bool TryParseSeriesStatus(string status, out SeriesStatus? enumValue)
    {
        if (Enum.TryParse(status, true, out SeriesStatus seriesStatus))
        {
            enumValue = seriesStatus;
            return true;
        }

        if (_continuingState.Contains(status, StringComparer.OrdinalIgnoreCase))
        {
            enumValue = SeriesStatus.Continuing;
            return true;
        }

        if (_endedState.Contains(status, StringComparer.OrdinalIgnoreCase))
        {
            enumValue = SeriesStatus.Ended;
            return true;
        }

        enumValue = null;
        return false;
    }
}
