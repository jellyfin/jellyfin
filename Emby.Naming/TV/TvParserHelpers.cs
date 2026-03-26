using System;
using System.Collections.Frozen;
using MediaBrowser.Model.Entities;

namespace Emby.Naming.TV;

/// <summary>
/// Helper class for TV metadata parsing.
/// </summary>
public static class TvParserHelpers
{
    private static readonly FrozenSet<string> _continuingState = FrozenSet.ToFrozenSet(
        ["Pilot", "Returning Series", "Returning"],
        StringComparer.OrdinalIgnoreCase);

    private static readonly FrozenSet<string> _endedState = FrozenSet.ToFrozenSet(
        ["Cancelled", "Canceled"],
        StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Tries to parse a string into <see cref="SeriesStatus"/>.
    /// </summary>
    /// <param name="status">The status string.</param>
    /// <param name="enumValue">The <see cref="SeriesStatus"/>.</param>
    /// <returns>Returns true if parsing was successful.</returns>
    public static bool TryParseSeriesStatus(string? status, out SeriesStatus? enumValue)
    {
        if (Enum.TryParse(status, true, out SeriesStatus seriesStatus))
        {
            enumValue = seriesStatus;
            return true;
        }

        if (status is not null && _continuingState.Contains(status))
        {
            enumValue = SeriesStatus.Continuing;
            return true;
        }

        if (status is not null && _endedState.Contains(status))
        {
            enumValue = SeriesStatus.Ended;
            return true;
        }

        enumValue = null;
        return false;
    }
}
