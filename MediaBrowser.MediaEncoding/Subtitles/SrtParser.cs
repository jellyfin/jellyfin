using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using MediaBrowser.Model.MediaInfo;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.MediaEncoding.Subtitles
{
    public class SrtParser : ISubtitleParser
    {
        private readonly ILogger _logger;

        private readonly CultureInfo _usCulture = new CultureInfo("en-US");

        public SrtParser(ILogger logger)
        {
            _logger = logger;
        }

        public SubtitleTrackInfo Parse(Stream stream, CancellationToken cancellationToken)
        {
            var trackInfo = new SubtitleTrackInfo();
            var trackEvents = new List<SubtitleTrackEvent>();
            using (var reader = new StreamReader(stream))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }
                    var subEvent = new SubtitleTrackEvent { Id = line };
                    line = reader.ReadLine();

                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    var time = Regex.Split(line, @"[\t ]*-->[\t ]*");

                    if (time.Length < 2)
                    {
                        // This occurs when subtitle text has an empty line as part of the text.
                        // Need to adjust the break statement below to resolve this.
                        _logger.LogWarning("Unrecognized line in srt: {0}", line);
                        continue;
                    }
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
                    subEvent.Text = string.Join(ParserValues.NewLine, multiline);
                    subEvent.Text = subEvent.Text.Replace(@"\N", ParserValues.NewLine, StringComparison.OrdinalIgnoreCase);
                    subEvent.Text = Regex.Replace(subEvent.Text, @"\{(?:\\\d?[\w.-]+(?:\([^\)]*\)|&H?[0-9A-Fa-f]+&|))+\}", string.Empty, RegexOptions.IgnoreCase);
                    subEvent.Text = Regex.Replace(subEvent.Text, "<", "&lt;", RegexOptions.IgnoreCase);
                    subEvent.Text = Regex.Replace(subEvent.Text, ">", "&gt;", RegexOptions.IgnoreCase);
                    subEvent.Text = Regex.Replace(subEvent.Text, "&lt;(\\/?(font|b|u|i|s))((\\s+(\\w|\\w[\\w\\-]*\\w)(\\s*=\\s*(?:\\\".*?\\\"|'.*?'|[^'\\\">\\s]+))?)+\\s*|\\s*)(\\/?)&gt;", "<$1$3$7>", RegexOptions.IgnoreCase);
                    trackEvents.Add(subEvent);
                }
            }
            trackInfo.TrackEvents = trackEvents.ToArray();
            return trackInfo;
        }

        long GetTicks(string time)
        {
            return TimeSpan.TryParseExact(time, @"hh\:mm\:ss\.fff", _usCulture, out var span)
                ? span.Ticks
                : (TimeSpan.TryParseExact(time, @"hh\:mm\:ss\,fff", _usCulture, out span)
                ? span.Ticks : 0);
        }
    }
}
