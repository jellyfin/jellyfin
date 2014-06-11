using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace MediaBrowser.MediaEncoding.Subtitles
{
    public class SrtParser : ISubtitleParser
    {
        private readonly CultureInfo _usCulture = new CultureInfo("en-US");
        public SubtitleTrackInfo Parse(Stream stream, CancellationToken cancellationToken)
        {
            var trackInfo = new SubtitleTrackInfo();
            using ( var reader = new StreamReader(stream))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }
                    var subEvent = new SubtitleTrackEvent {Id = line};
                    line = reader.ReadLine();

                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }
                    
                    var time = Regex.Split(line, @"[\t ]*-->[\t ]*");
                    subEvent.StartPositionTicks = GetTicks(time[0]);
                    var endTime = time[1];
                    var idx = endTime.IndexOf(" ", StringComparison.Ordinal);
                    if (idx > 0)
                        endTime = endTime.Substring(0, idx);
                    subEvent.EndPositionTicks = GetTicks(endTime);
                    var multiline = new List<string>();
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (string.IsNullOrEmpty(line))
                        {
                            break;
                        }
                        multiline.Add(line);
                    }
                    subEvent.Text = string.Join(@"\N", multiline);
                    subEvent.Text = Regex.Replace(subEvent.Text, @"\{(\\[\w]+\(?([\w\d]+,?)+\)?)+\}", string.Empty, RegexOptions.IgnoreCase);
                    subEvent.Text = Regex.Replace(subEvent.Text, "<", "&lt;", RegexOptions.IgnoreCase);
                    subEvent.Text = Regex.Replace(subEvent.Text, ">", "&gt;", RegexOptions.IgnoreCase);
                    subEvent.Text = Regex.Replace(subEvent.Text, "&lt;(\\/?(font|b|u|i|s))((\\s+(\\w|\\w[\\w\\-]*\\w)(\\s*=\\s*(?:\\\".*?\\\"|'.*?'|[^'\\\">\\s]+))?)+\\s*|\\s*)(\\/?)&gt;", "<$1$3$7>", RegexOptions.IgnoreCase);
                    subEvent.Text = Regex.Replace(subEvent.Text, @"\\N", "<br />",RegexOptions.IgnoreCase);
                    trackInfo.TrackEvents.Add(subEvent);
                }
            }
            return trackInfo;
        }

        long GetTicks(string time) {
            TimeSpan span;
            return TimeSpan.TryParseExact(time, @"hh\:mm\:ss\.fff", _usCulture, out span)
                ? span.Ticks
                : (TimeSpan.TryParseExact(time, @"hh\:mm\:ss\,fff", _usCulture, out span) 
                ? span.Ticks : 0);
        }
    }
}
