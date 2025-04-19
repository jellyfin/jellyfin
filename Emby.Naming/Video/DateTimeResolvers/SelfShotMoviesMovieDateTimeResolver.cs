using System.Linq;
using System.Text.RegularExpressions;
using Emby.Naming.Common;

namespace Emby.Naming.Video.DateTimeResolvers;

/// <summary>
/// Self-Shot videos often have a timestamp in them. Here we want to preserve the name.
/// Eg. "My Movie 2013.12.09".
/// If there is a additional timestamp we will use the extra date but not.
/// </summary>
public partial class SelfShotMoviesMovieDateTimeResolver : IMovieDateTimeResolver
{
    private static readonly Regex[] _regexes = [TimestampWithDateRegex(), TimestampRegex()];

    [GeneratedRegex(@"(?'name'.*(?'timestamp'(19[0-9]{2}|20[0-9]{2})(-|\.)?([0-9]{1,2})(-|\.)?([0-9]{1,2}))).*(?'date'19[0-9]{2}|20[0-9]{2})", RegexOptions.IgnoreCase)]
    private static partial Regex TimestampWithDateRegex();

    [GeneratedRegex(@"(?'name'.*(?'timestamp'(19[0-9]{2}|20[0-9]{2})(-|\.)?([0-9]{1,2})(-|\.)?([0-9]{1,2})))", RegexOptions.IgnoreCase)]
    private static partial Regex TimestampRegex();

    /// <summary>
    /// Attempts to resolve date and Name from the provided fileName.
    /// </summary>
    /// <param name="fileName">Name of video.</param>
    /// <param name="namingOptions">NamingOptions.</param>
    /// <returns>null if could not resolve.</returns>
    public CleanDateTimeResult? Resolve(string fileName, NamingOptions namingOptions)
    {
        foreach (var regex in _regexes)
        {
            var match = regex.Match(fileName);

            if (match.Success)
            {
                match.Groups.TryGetValue("timestamp", out var timeStampGroup);

                var dashSeparatorCount = timeStampGroup!.Value.Count(character => character == '-');
                var dotSeparatorCount = timeStampGroup!.Value.Count(character => character == '.');
                if (dotSeparatorCount == 1 || dashSeparatorCount == 1)
                {
                    // this is not a homevideo. timestamp was wrongly read eg 2010-1080 (this means 1080p) we need to have 0 or 2 dashes
                    continue;
                }

                match.Groups.TryGetValue("name", out var nameGroup);
                match.Groups.TryGetValue("date", out var dateGroup);

                return new CleanDateTimeResult(nameGroup!.Value, dateGroup?.Value);
            }
        }

        return null;
    }
}
