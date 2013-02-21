using MediaBrowser.Common.Logging;
using MediaBrowser.Common.Win32;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

namespace MediaBrowser.Controller.Resolvers.TV
{
    public static class TVUtils
    {
        public static readonly string TVDBApiKey = "B89CE93890E9419B";
        public static readonly string BannerUrl = "http://www.thetvdb.com/banners/";

        /// <summary>
        /// A season folder must contain one of these somewhere in the name
        /// </summary>
        private static readonly string[] SeasonFolderNames = new[]
                                                                 {
                                                                     "season",
                                                                     "sæson",
                                                                     "temporada",
                                                                     "saison",
                                                                     "staffel"
                                                                 };

        /// <summary>
        /// Used to detect paths that represent episodes, need to make sure they don't also
        /// match movie titles like "2001 A Space..."
        /// Currently we limit the numbers here to 2 digits to try and avoid this
        /// </summary>
        /// <remarks>
        /// The order here is important, if the order is changed some of the later
        /// ones might incorrectly match things that higher ones would have caught.
        /// The most restrictive expressions should appear first
        /// </remarks>
        private static readonly Regex[] EpisodeExpressions = new[]
                                                                 {
                                                                     new Regex(
                                                                         @".*\\[s|S]?(?<seasonnumber>\d{1,2})[x|X](?<epnumber>\d{1,3})[^\\]*$",
                                                                         RegexOptions.Compiled),
                                                                     // 01x02 blah.avi S01x01 balh.avi
                                                                     new Regex(
                                                                         @".*\\[s|S](?<seasonnumber>\d{1,2})x?[e|E](?<epnumber>\d{1,3})[^\\]*$",
                                                                         RegexOptions.Compiled),
                                                                     // S01E02 blah.avi, S01xE01 blah.avi
                                                                     new Regex(
                                                                         @".*\\(?<seriesname>[^\\]*)[s|S]?(?<seasonnumber>\d{1,2})[x|X](?<epnumber>\d{1,3})[^\\]*$",
                                                                         RegexOptions.Compiled),
                                                                     // 01x02 blah.avi S01x01 balh.avi
                                                                     new Regex(
                                                                         @".*\\(?<seriesname>[^\\]*)[s|S](?<seasonnumber>\d{1,2})[x|X|\.]?[e|E](?<epnumber>\d{1,3})[^\\]*$",
                                                                         RegexOptions.Compiled)
                                                                     // S01E02 blah.avi, S01xE01 blah.avi
                                                                 };

        /// <summary>
        /// To avoid the following matching movies they are only valid when contained in a folder which has been matched as a being season
        /// </summary>
        private static readonly Regex[] EpisodeExpressionsInASeasonFolder = new[]
                                                                                {
                                                                                    new Regex(
                                                                                        @".*\\(?<epnumber>\d{1,2})\s?-\s?[^\\]*$",
                                                                                        RegexOptions.Compiled),
                                                                                    // 01 - blah.avi, 01-blah.avi
                                                                                    new Regex(
                                                                                        @".*\\(?<epnumber>\d{1,2})[^\d\\]*[^\\]*$",
                                                                                        RegexOptions.Compiled),
                                                                                    // 01.avi, 01.blah.avi "01 - 22 blah.avi" 
                                                                                    new Regex(
                                                                                        @".*\\(?<seasonnumber>\d)(?<epnumber>\d{1,2})[^\d\\]+[^\\]*$",
                                                                                        RegexOptions.Compiled),
                                                                                    // 01.avi, 01.blah.avi
                                                                                    new Regex(
                                                                                        @".*\\\D*\d+(?<epnumber>\d{2})",
                                                                                        RegexOptions.Compiled)
                                                                                    // hell0 - 101 -  hello.avi

                                                                                };

        public static int? GetSeasonNumberFromPath(string path)
        {
            // Look for one of the season folder names
            foreach (var name in SeasonFolderNames)
            {
                int index = path.IndexOf(name, StringComparison.OrdinalIgnoreCase);

                if (index != -1)
                {
                    return GetSeasonNumberFromPathSubstring(path.Substring(index + name.Length));
                }
            }

            return null;
        }

        /// <summary>
        /// Extracts the season number from the second half of the Season folder name (everything after "Season", or "Staffel")
        /// </summary>
        private static int? GetSeasonNumberFromPathSubstring(string path)
        {
            int numericStart = -1;
            int length = 0;

            // Find out where the numbers start, and then keep going until they end
            for (int i = 0; i < path.Length; i++)
            {
                if (char.IsNumber(path, i))
                {
                    if (numericStart == -1)
                    {
                        numericStart = i;
                    }
                    length++;
                }
                else if (numericStart != -1)
                {
                    break;
                }
            }

            if (numericStart == -1)
            {
                return null;
            }

            return int.Parse(path.Substring(numericStart, length));
        }

        public static bool IsSeasonFolder(string path)
        {
            return GetSeasonNumberFromPath(path) != null;
        }

        public static bool IsSeriesFolder(string path, IEnumerable<WIN32_FIND_DATA> fileSystemChildren)
        {
            // A folder with more than 3 non-season folders in will not becounted as a series
            var nonSeriesFolders = 0;

            foreach (var child in fileSystemChildren)
            {
                if (child.IsHidden || child.IsSystemFile)
                {
                    continue;
                }

                if (child.IsDirectory)
                {
                    if (IsSeasonFolder(child.Path))
                    {
                        return true;
                    }

                    nonSeriesFolders++;

                    if (nonSeriesFolders >= 3)
                    {
                        return false;
                    }
                }
                else
                {
                    if (EntityResolutionHelper.IsVideoFile(child.Path) &&
                        !string.IsNullOrEmpty(EpisodeNumberFromFile(child.Path, false)))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static string EpisodeNumberFromFile(string fullPath, bool isInSeason)
        {
            string fl = fullPath.ToLower();
            foreach (var r in EpisodeExpressions)
            {
                Match m = r.Match(fl);
                if (m.Success)
                    return m.Groups["epnumber"].Value;
            }
            if (isInSeason)
            {
                var match = EpisodeExpressionsInASeasonFolder.Select(r => r.Match(fl))
                    .FirstOrDefault(m => m.Success);

                if (match != null)
                {
                    return match.Value;
                }
            }

            return null;
        }

        public static string SeasonNumberFromEpisodeFile(string fullPath)
        {
            string fl = fullPath.ToLower();
            foreach (var r in EpisodeExpressions)
            {
                Match m = r.Match(fl);
                if (m.Success)
                {
                    Group g = m.Groups["seasonnumber"];
                    if (g != null)
                        return g.Value;
                    return null;
                }
            }
            return null;
        }

        public static List<DayOfWeek> GetAirDays(string day)
        {
            if (!string.IsNullOrWhiteSpace(day))
            {
                if (day.Equals("Daily", StringComparison.OrdinalIgnoreCase))
                {
                    return new List<DayOfWeek>
                               {
                                   DayOfWeek.Sunday,
                                   DayOfWeek.Monday,
                                   DayOfWeek.Tuesday,
                                   DayOfWeek.Wednesday,
                                   DayOfWeek.Thursday,
                                   DayOfWeek.Friday,
                                   DayOfWeek.Saturday
                               };
                }

                DayOfWeek value;

                if (Enum.TryParse(day, true, out value))
                {
                    return new List<DayOfWeek>
                               {
                                   value
                               };
                }

                Logger.LogWarning("Invalid value passed into GetAirDays: {0}", day);

                return new List<DayOfWeek>
                               {
                               };
            }
            return null;
        }
    }
}
