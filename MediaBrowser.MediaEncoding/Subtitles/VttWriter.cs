using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using MediaBrowser.Model.MediaInfo;

namespace MediaBrowser.MediaEncoding.Subtitles
{
    /// <summary>
    /// Subtitle writer for the WebVTT format.
    /// </summary>
    public partial class VttWriter : ISubtitleWriter
    {
        private static readonly Dictionary<string, string> _assTagToCuePosition = new()
        {
            ["{\\an1}"] = "position:20% line:90%,end",
            ["{\\an3}"] = "position:80% line:90%,end",
            ["{\\an4}"] = "position:20% line:50%,center",
            ["{\\an5}"] = "line:50%,center",
            ["{\\an6}"] = "position:80% line:50%,center",
            ["{\\an7}"] = "position:20% line:10%,start",
            ["{\\an8}"] = "line:10%,start",
            ["{\\an9}"] = "position:80% line:10%,start",
        };

        [GeneratedRegex(@"\\n", RegexOptions.IgnoreCase)]
        private static partial Regex NewlineEscapeRegex();

        [GeneratedRegex(@"^\{\\an\d\}")]
        private static partial Regex AssAlignTagRegex();

        private static string GetCuePositionFromAssTag(string text)
        {
            foreach (var (tag, position) in _assTagToCuePosition)
            {
                if (text.StartsWith(tag, StringComparison.Ordinal))
                {
                    return position;
                }
            }

            return "region:subtitle line:90%,end";
        }

        /// <inheritdoc />
        public void Write(SubtitleTrackInfo info, Stream stream, CancellationToken cancellationToken)
        {
            using (var writer = new StreamWriter(stream, Encoding.UTF8, 1024, true))
            {
                writer.WriteLine("WEBVTT");
                writer.WriteLine();
                writer.WriteLine("Region: id:subtitle width:80% lines:3 regionanchor:50%,100% viewportanchor:50%,90%");
                writer.WriteLine();
                foreach (var trackEvent in info.TrackEvents)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var startTime = TimeSpan.FromTicks(trackEvent.StartPositionTicks);
                    var endTime = TimeSpan.FromTicks(trackEvent.EndPositionTicks);
                    // make sure the start and end times are different and sequential
                    if (endTime.TotalMilliseconds <= startTime.TotalMilliseconds)
                    {
                        endTime = startTime.Add(TimeSpan.FromMilliseconds(1));
                    }

                    var text = trackEvent.Text;
                    // TODO: Not sure how to handle these
                    text = NewlineEscapeRegex().Replace(text, " ");
                    var cuePosition = GetCuePositionFromAssTag(text);
                    text = AssAlignTagRegex().Replace(text, string.Empty);
                    writer.WriteLine(@"{0:hh\:mm\:ss\.fff} --> {1:hh\:mm\:ss\.fff} {2}", startTime, endTime, cuePosition);
                    writer.WriteLine(text);
                    writer.WriteLine();
                }
            }
        }
    }
}

