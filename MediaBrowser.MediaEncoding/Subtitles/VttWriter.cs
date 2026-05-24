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
        // Mapping of ASS alignment tags to WebVTT cue settings.
        // The ",end" suffix is only added to line:90% values because:
        // - ExoPlayer requires it for proper positioning at the bottom
        // - Other players might break when ",end" is used with other line values (e.g., line:50%,end)
        private static readonly Dictionary<string, string> _assTagToCuePosition = new()
        {
            ["{\\an1}"] = "position:20% line:90%,end",
            ["{\\an2}"] = "line:90%,end",
            ["{\\an3}"] = "position:80% line:90%,end",
            ["{\\an4}"] = "position:20% line:50%",
            ["{\\an5}"] = "line:50%",
            ["{\\an6}"] = "position:80% line:50%",
            ["{\\an7}"] = "position:20% line:10%",
            ["{\\an8}"] = "line:10%",
            ["{\\an9}"] = "position:80% line:10%",
        };

        // Matches any ASS block: {contents}
        private static readonly Regex _assBlockRegex = new Regex(@"\{([^}]*)\}", RegexOptions.Compiled);

        /// <summary>
        /// Regex to match ASS newline escapes (\n or \N). Case-insensitive to catch both.
        /// </summary>
        [GeneratedRegex(@"\\[nN]")]
        private static partial Regex NewlineEscapeRegex();

        /// <summary>
        /// Processes all ASS {...} blocks in the text, extracting alignment and
        /// converting formatting tags to HTML.
        /// Specifically:
        /// - Extracts the \anN alignment tag (first one found) to determine cue position.
        /// - Converts \i1/\i0, \b1/\b0, \u1/\u0 inside blocks to &lt;i&gt;/&lt;b&gt;/&lt;u&gt; HTML tags.
        /// - Strips unrecognized tags silently.
        /// - Blocks that only contained the alignment tag are removed entirely.
        /// Also handles HTML tags already present in the text (e.g. &lt;i&gt;, &lt;b&gt;).
        /// </summary>
        /// <param name="text">Raw subtitle text from the SRT/ASS track.</param>
        /// <param name="cuePosition">Output: WebVTT cue position string.</param>
        /// <returns>Cleaned text with HTML formatting tags.</returns>
        private static string ProcessAssBlocks(string text, out string cuePosition)
        {
            // out params cannot be captured in lambdas; use a local variable and assign at the end.
            string capturedPosition = "line:90%,end";

            var result = _assBlockRegex.Replace(text, match =>
            {
                var inner = match.Groups[1].Value;
                var output = new StringBuilder();

                int i = 0;
                while (i < inner.Length)
                {
                    if (inner[i] != '\\')
                    {
                        i++;
                        continue;
                    }

                    if (i + 1 >= inner.Length)
                    {
                        i++;
                        continue;
                    }

                    char tag = inner[i + 1];

                    // \anN — alignment tag
                    if (tag == 'a' && i + 3 < inner.Length && inner[i + 2] == 'n' && char.IsDigit(inner[i + 3]))
                    {
                        var key = $"{{\\an{inner[i + 3]}}}";
                        if (_assTagToCuePosition.TryGetValue(key, out var pos))
                        {
                            capturedPosition = pos;
                        }

                        i += 4;
                        continue;
                    }

                    // \i1, \i0, \b1, \b0, \u1, \u0
                    if ((tag == 'i' || tag == 'b' || tag == 'u') && i + 2 < inner.Length)
                    {
                        char val = inner[i + 2];
                        if (val == '1')
                        {
                            output.Append(tag == 'i' ? "<i>" : tag == 'b' ? "<b>" : "<u>");
                            i += 3;
                            continue;
                        }
                        else if (val == '0')
                        {
                            output.Append(tag == 'i' ? "</i>" : tag == 'b' ? "</b>" : "</u>");
                            i += 3;
                            continue;
                        }
                    }

                    // Unknown tag — skip backslash and keep going
                    i++;
                }

                return output.ToString();
            });

            cuePosition = capturedPosition;
            return result;
        }

        /// <summary>
        /// Automatically closes any unclosed HTML tags in the text using a LIFO stack,
        /// so nested tags like &lt;i&gt;&lt;b&gt;text are closed as &lt;/b&gt;&lt;/i&gt; in the correct order.
        /// </summary>
        private static string AutoCloseHtmlTags(string text)
        {
            var stack = new Stack<char>();
            int i = 0;
            while (i < text.Length)
            {
                if (text[i] == '<' && i + 2 < text.Length)
                {
                    bool isClosing = text[i + 1] == '/';
                    int tagStart = isClosing ? i + 2 : i + 1;
                    if (tagStart < text.Length
                        && (text[tagStart] == 'i' || text[tagStart] == 'b' || text[tagStart] == 'u')
                        && tagStart + 1 < text.Length && text[tagStart + 1] == '>')
                    {
                        char tag = text[tagStart];
                        if (!isClosing)
                        {
                            stack.Push(tag);
                            i = tagStart + 2;
                            continue;
                        }
                        else if (stack.Count > 0 && stack.Peek() == tag)
                        {
                            stack.Pop();
                            i = tagStart + 2;
                            continue;
                        }
                    }
                }

                i++;
            }

            if (stack.Count == 0)
            {
                return text;
            }

            var result = new StringBuilder(text);
            foreach (var tag in stack)
            {
                result.Append("</").Append(tag).Append('>');
            }

            return result.ToString();
        }

        /// <inheritdoc />
        public void Write(SubtitleTrackInfo info, Stream stream, CancellationToken cancellationToken)
        {
            using (var writer = new StreamWriter(stream, Encoding.UTF8, 1024, true))
            {
                writer.WriteLine("WEBVTT");
                writer.WriteLine();
                writer.WriteLine();
                foreach (var trackEvent in info.TrackEvents)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var startTime = TimeSpan.FromTicks(trackEvent.StartPositionTicks);
                    var endTime = TimeSpan.FromTicks(trackEvent.EndPositionTicks);

                    if (endTime.TotalMilliseconds <= startTime.TotalMilliseconds)
                    {
                        endTime = startTime.Add(TimeSpan.FromMilliseconds(1));
                    }

                    var text = trackEvent.Text;

                    // Replace ASS newline escapes (\n or \N) with spaces
                    text = NewlineEscapeRegex().Replace(text, " ");

                    // Process all ASS {...} blocks: extract alignment, convert formatting to HTML
                    text = ProcessAssBlocks(text, out var cuePosition);

                    // Ensure all HTML tags are properly closed
                    text = AutoCloseHtmlTags(text);

                    writer.WriteLine(@"{0:hh\:mm\:ss\.fff} --> {1:hh\:mm\:ss\.fff} {2}", startTime, endTime, cuePosition);
                    writer.WriteLine(text);
                    writer.WriteLine();
                }
            }
        }
    }
}
