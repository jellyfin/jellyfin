using System;
using System.Linq;
using System.Text.RegularExpressions;
using Emby.Naming.Common;

namespace Emby.Naming.Video.DateTimeResolvers;

/// <summary>
/// Stuff to reshare.
/// </summary>
public class DateTimeResolverHelpers
{
    /// <summary>
    /// Characters that should not be at the start or end of any filename.
    /// </summary>
    public static readonly char[] TrimChars = [' ', '-', '.', '_', '*'];

    /// <summary>
    /// Gets the name of the movie. depending on where the date is found inside the file.
    /// </summary>
    /// <param name="fileName">the original name of the file.</param>
    /// <param name="dateWithSpecialCharacters">what is the date as the full match eg '(2020)'.</param>
    /// <param name="namingOptions">NamingOptions.</param>
    /// <returns>The best assumption of the fileName.</returns>
    public static string GetBestNameMatchAfterRemovalOfDate(string fileName, string dateWithSpecialCharacters, NamingOptions namingOptions)
    {
        var positionOfDate = fileName.IndexOf(dateWithSpecialCharacters, StringComparison.Ordinal);
        var dateAtBegginingOfFileName = positionOfDate == 0;

        string withoutDate;

        if (dateAtBegginingOfFileName)
        {
            withoutDate = fileName.Substring(dateWithSpecialCharacters.Length);
        }
        else
        {
            // if we the date is somewhere in the middle of the file we assume the filename is before (by convention)
            withoutDate = fileName.Substring(0, positionOfDate);
        }

        // Remove annotation of file
        var closingBracketIndex = withoutDate.IndexOf(']', StringComparison.InvariantCulture);
        if (withoutDate.StartsWith('[') && closingBracketIndex != -1)
        {
            var withoutPrefix = withoutDate[(closingBracketIndex + 1) ..];

            if (withoutPrefix.Trim(TrimChars).Length != 0)
            {
                // dont trim it if thats all we have. then it must be the filename
                withoutDate = withoutPrefix;
            }
        }

        withoutDate = TrimAfterFirstNonTitleOccurrence(withoutDate, namingOptions.NonTitleStringsRegexes);

        return withoutDate.Trim(TrimChars);
    }

    /// <summary>
    /// Removes everything starting from the first non-title occurence.
    /// </summary>
    /// <param name="fileName">the filename.</param>
    /// <param name="nonTitleStringRegexes">the regexes of matches of what are non-titles.</param>
    /// <returns>the cleaned filename.</returns>
    public static string TrimAfterFirstNonTitleOccurrence(string fileName, Regex[] nonTitleStringRegexes)
    {
        var firstMatch = nonTitleStringRegexes
            .Select(regex => regex.Match(fileName))
            .Where(match => match.Success)
            .SelectMany(match => match.Groups.Values)
            .Where(group => group.Length != 0)
            .MinBy(group => group.Index);

        if (firstMatch is null)
        {
            return fileName;
        }

        var newFileName = fileName[..firstMatch.Index].Trim(TrimChars);

        return newFileName.Length == 0 ? fileName : newFileName;
    }
}
