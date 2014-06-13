using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace MediaBrowser.MediaEncoding.Subtitles
{
    public class SsaParser : ISubtitleParser
    {
        private readonly CultureInfo _usCulture = new CultureInfo("en-US");

        public SubtitleTrackInfo Parse(Stream stream, CancellationToken cancellationToken)
        {
            var trackInfo = new SubtitleTrackInfo();
            var eventIndex = 1;
            using (var reader = new StreamReader(stream))
            {
                string line;
                while (reader.ReadLine() != "[Events]")
                {}
                var headers = ParseFieldHeaders(reader.ReadLine());

                while ((line = reader.ReadLine()) != null)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }
                    if(line.StartsWith("["))
                        break;
                    if(string.IsNullOrEmpty(line))
                        continue;
                    var subEvent = new SubtitleTrackEvent { Id = eventIndex.ToString(_usCulture) };
                    eventIndex++;
                    var sections = line.Substring(10).Split(',');

                    subEvent.StartPositionTicks = GetTicks(sections[headers["Start"]]);
                    subEvent.EndPositionTicks = GetTicks(sections[headers["End"]]);
                    subEvent.Text = string.Join(",", sections.Skip(headers["Text"]));
                    subEvent.Text = Regex.Replace(subEvent.Text, @"\{(\\[\w]+\(?([\w\d]+,?)+\)?)+\}", string.Empty, RegexOptions.IgnoreCase);

                    trackInfo.TrackEvents.Add(subEvent);
                }
            }
            return trackInfo;
        }

        long GetTicks(string time)
        {
            TimeSpan span;
            return TimeSpan.TryParseExact(time, @"h\:mm\:ss\.ff", _usCulture, out span)
                ? span.Ticks: 0;
        }

        private Dictionary<string,int> ParseFieldHeaders(string line) {
            var fields = line.Substring(8).Split(',').Select(x=>x.Trim()).ToList();

            var result = new Dictionary<string, int> {
                                                         {"Start", fields.IndexOf("Start")},
                                                         {"End", fields.IndexOf("End")},
                                                         {"Text", fields.IndexOf("Text")}
                                                     };
            return result;
        }
    }
}
