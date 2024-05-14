using System;
using Emby.Naming.Common;
using MediaBrowser.Model.Entities;

namespace Emby.Naming.TV;

/// <summary>
/// Helper class for tv metadata parsing.
/// </summary>
public static class TvParserHelpers
{
    /// <summary>
    /// Parses a information about series from path.
    /// </summary>
    /// <param name="status">The status string.</param>
    /// <param name="enumValue">The enum value.</param>
    /// <returns>Returns true if parsing was successful.</returns>
    public static bool TryParseSeriesStatus(string status, out SeriesStatus? enumValue)
    {
        if (Enum.TryParse(status, true, out SeriesStatus seriesStatus))
        {
            enumValue = seriesStatus;
            return true;
        }

        switch (status)
        {
            case "Pilot":
            case "Returning Series":
                enumValue = SeriesStatus.Continuing;
                return true;
            case "Cancelled":
                enumValue = SeriesStatus.Ended;
                return true;
            default:
                enumValue = null;
                return false;
        }
    }
}
