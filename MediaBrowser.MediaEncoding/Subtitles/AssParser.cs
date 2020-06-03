using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using MediaBrowser.Model.MediaInfo;

namespace MediaBrowser.MediaEncoding.Subtitles
{
    public class AssParser : ISubtitleParser
    {
        private readonly CultureInfo _usCulture = new CultureInfo("en-US");

        public SubtitleTrackInfo Parse(Stream stream, CancellationToken cancellationToken)
        {
            var trackInfo = new SubtitleTrackInfo();
            var trackEvents = new List<SubtitleTrackEvent>();
            var eventIndex = 1;
            using (var reader = new StreamReader(stream))
            {
                string line;
                while (reader.ReadLine() != "[Events]")
                { }
                var headers = ParseFieldHeaders(reader.ReadLine());

                while ((line = reader.ReadLine()) != null)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    if (line.StartsWith("["))
                    {
                        break;
                    }

                    var subEvent = new SubtitleTrackEvent { Id = eventIndex.ToString(_usCulture) };
                    eventIndex++;
                    var sections = line.Substring(10).Split(',');

                    subEvent.StartPositionTicks = GetTicks(sections[headers["Start"]]);
                    subEvent.EndPositionTicks = GetTicks(sections[headers["End"]]);

                    subEvent.Text = string.Join(",", sections.Skip(headers["Text"]));
                    RemoteNativeFormatting(subEvent);

                    subEvent.Text = subEvent.Text.Replace("\\n", ParserValues.NewLine, StringComparison.OrdinalIgnoreCase);

                    subEvent.Text = Regex.Replace(subEvent.Text, @"\{(\\[\w]+\(?([\w\d]+,?)+\)?)+\}", string.Empty, RegexOptions.IgnoreCase);

                    trackEvents.Add(subEvent);
                }
            }
            trackInfo.TrackEvents = trackEvents.ToArray();
            return trackInfo;
        }

        long GetTicks(string time)
        {
            return TimeSpan.TryParseExact(time, @"h\:mm\:ss\.ff", _usCulture, out var span)
                ? span.Ticks : 0;
        }

        private Dictionary<string, int> ParseFieldHeaders(string line)
        {
            var fields = line.Substring(8).Split(',').Select(x => x.Trim()).ToList();

            var result = new Dictionary<string, int> {
                                                         {"Start", fields.IndexOf("Start")},
                                                         {"End", fields.IndexOf("End")},
                                                         {"Text", fields.IndexOf("Text")}
                                                     };
            return result;
        }

        /// <summary>
        /// Credit: https://github.com/SubtitleEdit/subtitleedit/blob/master/src/Logic/SubtitleFormats/AdvancedSubStationAlpha.cs
        /// </summary>
        private void RemoteNativeFormatting(SubtitleTrackEvent p)
        {
            int indexOfBegin = p.Text.IndexOf('{');
            string pre = string.Empty;
            while (indexOfBegin >= 0 && p.Text.IndexOf('}') > indexOfBegin)
            {
                string s = p.Text.Substring(indexOfBegin);
                if (s.StartsWith("{\\an1}", StringComparison.Ordinal) ||
                    s.StartsWith("{\\an2}", StringComparison.Ordinal) ||
                    s.StartsWith("{\\an3}", StringComparison.Ordinal) ||
                    s.StartsWith("{\\an4}", StringComparison.Ordinal) ||
                    s.StartsWith("{\\an5}", StringComparison.Ordinal) ||
                    s.StartsWith("{\\an6}", StringComparison.Ordinal) ||
                    s.StartsWith("{\\an7}", StringComparison.Ordinal) ||
                    s.StartsWith("{\\an8}", StringComparison.Ordinal) ||
                    s.StartsWith("{\\an9}", StringComparison.Ordinal))
                {
                    pre = s.Substring(0, 6);
                }
                else if (s.StartsWith("{\\an1\\", StringComparison.Ordinal) ||
                    s.StartsWith("{\\an2\\", StringComparison.Ordinal) ||
                    s.StartsWith("{\\an3\\", StringComparison.Ordinal) ||
                    s.StartsWith("{\\an4\\", StringComparison.Ordinal) ||
                    s.StartsWith("{\\an5\\", StringComparison.Ordinal) ||
                    s.StartsWith("{\\an6\\", StringComparison.Ordinal) ||
                    s.StartsWith("{\\an7\\", StringComparison.Ordinal) ||
                    s.StartsWith("{\\an8\\", StringComparison.Ordinal) ||
                    s.StartsWith("{\\an9\\", StringComparison.Ordinal))
                {
                    pre = s.Substring(0, 5) + "}";
                }
                int indexOfEnd = p.Text.IndexOf('}');
                p.Text = p.Text.Remove(indexOfBegin, (indexOfEnd - indexOfBegin) + 1);

                indexOfBegin = p.Text.IndexOf('{');
            }
            p.Text = pre + p.Text;
        }
    }
}
