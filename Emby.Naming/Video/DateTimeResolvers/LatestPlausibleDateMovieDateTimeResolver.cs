using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Emby.Naming.Common;

namespace Emby.Naming.Video.DateTimeResolvers;

/// <summary>
/// There may be multiple dates in the file but not all of them are plausible as productionDates
/// If there is only one Plausible one we can safely assume this is the actual date
/// If there are multiple plausible ones (before today) we can just try to pick the latest one - I mean when the hell was a movie produced in eg 1968 calling it 2001 or so and it being the future?
///        (except '2001: A Space Odyssey - 1968' thats a edge case and if they dont bracket it its their fault)
/// Eg. "1883 - 2021" will use 2021 as the date.
/// </summary>
public partial class LatestPlausibleDateMovieDateTimeResolver : IMovieDateTimeResolver
{
    [GeneratedRegex(@"(?'date'[0-9]{4})(?!p)", RegexOptions.IgnoreCase)]
    private static partial Regex AnyFourDigitDateRegex();

    /// <summary>
    /// Attempts to resolve date and Name from the provided fileName.
    /// </summary>
    /// <param name="fileName">Name of video.</param>
    /// <param name="namingOptions">NamingOptions.</param>
    /// <returns>null if could not resolve.</returns>
    public CleanDateTimeResult? Resolve(string fileName, NamingOptions namingOptions)
    {
        var matches = AnyFourDigitDateRegex().Matches(fileName);

        if (matches.Count == 0)
        {
            return null;
        }

        var assumedDate = matches
            .Select(match => int.Parse(match.Value, CultureInfo.InvariantCulture) as int?)
            .Where(IsPlausible)
            .Distinct()
            .Max();

        if (assumedDate is null)
        {
            return null;
        }

        var name = DateTimeResolverHelpers.GetBestNameMatchAfterRemovalOfDate(fileName, assumedDate.Value.ToString(CultureInfo.InvariantCulture), namingOptions);

        return new CleanDateTimeResult(name, assumedDate);
    }

    private bool IsPlausible(int? year)
    {
        if (year is null or < 1900)
        {
            return false;
        }

        // Additional year for edge cases.
        if (year > (DateTime.Now.Year + 1))
        {
            // This movie would have been produced in the future
            return false;
        }

        return true;
    }
}
