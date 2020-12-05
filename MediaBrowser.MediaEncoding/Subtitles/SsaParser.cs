using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using MediaBrowser.Model.MediaInfo;

namespace MediaBrowser.MediaEncoding.Subtitles
{
    /// <summary>
    /// <see href="https://github.com/SubtitleEdit/subtitleedit/blob/a299dc4407a31796364cc6ad83f0d3786194ba22/src/Logic/SubtitleFormats/SubStationAlpha.cs">Credit</see>.
    /// </summary>
    public class SsaParser : ISubtitleParser
    {
        /// <inheritdoc />
        public SubtitleTrackInfo Parse(Stream stream, CancellationToken cancellationToken)
        {
            var trackInfo = new SubtitleTrackInfo();
            var trackEvents = new List<SubtitleTrackEvent>();

            using (var reader = new StreamReader(stream))
            {
                bool eventsStarted = false;

                string[] format = "Marked, Start, End, Style, Name, MarginL, MarginR, MarginV, Effect, Text".Split(',');
                int indexLayer = 0;
                int indexStart = 1;
                int indexEnd = 2;
                int indexStyle = 3;
                int indexName = 4;
                int indexEffect = 8;
                int indexText = 9;
                int lineNumber = 0;

                var header = new StringBuilder();

                string line;

                while ((line = reader.ReadLine()) != null)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    lineNumber++;
                    if (!eventsStarted)
                    {
                        header.AppendLine(line);
                    }

                    if (string.Equals(line.Trim(), "[events]", StringComparison.OrdinalIgnoreCase))
                    {
                        eventsStarted = true;
                    }
                    else if (!string.IsNullOrEmpty(line) && line.Trim().StartsWith(';'))
                    {
                        // skip comment lines
                    }
                    else if (eventsStarted && line.Trim().Length > 0)
                    {
                        string s = line.Trim().ToLowerInvariant();
                        if (s.StartsWith("format:", StringComparison.Ordinal))
                        {
                            if (line.Length > 10)
                            {
                                format = line.ToLowerInvariant().Substring(8).Split(',');
                                for (int i = 0; i < format.Length; i++)
                                {
                                    if (string.Equals(format[i].Trim(), "layer", StringComparison.OrdinalIgnoreCase))
                                    {
                                        indexLayer = i;
                                    }
                                    else if (string.Equals(format[i].Trim(), "start", StringComparison.OrdinalIgnoreCase))
                                    {
                                        indexStart = i;
                                    }
                                    else if (string.Equals(format[i].Trim(), "end", StringComparison.OrdinalIgnoreCase))
                                    {
                                        indexEnd = i;
                                    }
                                    else if (string.Equals(format[i].Trim(), "text", StringComparison.OrdinalIgnoreCase))
                                    {
                                        indexText = i;
                                    }
                                    else if (string.Equals(format[i].Trim(), "effect", StringComparison.OrdinalIgnoreCase))
                                    {
                                        indexEffect = i;
                                    }
                                    else if (string.Equals(format[i].Trim(), "style", StringComparison.OrdinalIgnoreCase))
                                    {
                                        indexStyle = i;
                                    }
                                }
                            }
                        }
                        else if (!string.IsNullOrEmpty(s))
                        {
                            string text = string.Empty;
                            string start = string.Empty;
                            string end = string.Empty;
                            string style = string.Empty;
                            string layer = string.Empty;
                            string effect = string.Empty;
                            string name = string.Empty;

                            string[] splittedLine;

                            if (s.StartsWith("dialogue:", StringComparison.Ordinal))
                            {
                                splittedLine = line.Substring(10).Split(',');
                            }
                            else
                            {
                                splittedLine = line.Split(',');
                            }

                            for (int i = 0; i < splittedLine.Length; i++)
                            {
                                if (i == indexStart)
                                {
                                    start = splittedLine[i].Trim();
                                }
                                else if (i == indexEnd)
                                {
                                    end = splittedLine[i].Trim();
                                }
                                else if (i == indexLayer)
                                {
                                    layer = splittedLine[i];
                                }
                                else if (i == indexEffect)
                                {
                                    effect = splittedLine[i];
                                }
                                else if (i == indexText)
                                {
                                    text = splittedLine[i];
                                }
                                else if (i == indexStyle)
                                {
                                    style = splittedLine[i];
                                }
                                else if (i == indexName)
                                {
                                    name = splittedLine[i];
                                }
                                else if (i > indexText)
                                {
                                    text += "," + splittedLine[i];
                                }
                            }

                            try
                            {
                                trackEvents.Add(
                                    new SubtitleTrackEvent
                                    {
                                        StartPositionTicks = GetTimeCodeFromString(start),
                                        EndPositionTicks = GetTimeCodeFromString(end),
                                        Text = GetFormattedText(text)
                                    });
                            }
                            catch
                            {
                            }
                        }
                    }
                }

                // if (header.Length > 0)
                // subtitle.Header = header.ToString();

                // subtitle.Renumber(1);
            }

            trackInfo.TrackEvents = trackEvents.ToArray();
            return trackInfo;
        }

        private static long GetTimeCodeFromString(string time)
        {
            // h:mm:ss.cc
            string[] timeCode = time.Split(':', '.');
            return new TimeSpan(
                0,
                int.Parse(timeCode[0], CultureInfo.InvariantCulture),
                int.Parse(timeCode[1], CultureInfo.InvariantCulture),
                int.Parse(timeCode[2], CultureInfo.InvariantCulture),
                int.Parse(timeCode[3], CultureInfo.InvariantCulture) * 10).Ticks;
        }

        private static string GetFormattedText(string text)
        {
            text = text.Replace("\\n", ParserValues.NewLine, StringComparison.OrdinalIgnoreCase);

            for (int i = 0; i < 10; i++) // just look ten times...
            {
                if (text.Contains(@"{\fn", StringComparison.Ordinal))
                {
                    int start = text.IndexOf(@"{\fn", StringComparison.Ordinal);
                    int end = text.IndexOf('}', start);
                    if (end > 0 && !text.Substring(start).StartsWith("{\\fn}", StringComparison.Ordinal))
                    {
                        string fontName = text.Substring(start + 4, end - (start + 4));
                        string extraTags = string.Empty;
                        CheckAndAddSubTags(ref fontName, ref extraTags, out bool italic);
                        text = text.Remove(start, end - start + 1);
                        if (italic)
                        {
                            text = text.Insert(start, "<font face=\"" + fontName + "\"" + extraTags + "><i>");
                        }
                        else
                        {
                            text = text.Insert(start, "<font face=\"" + fontName + "\"" + extraTags + ">");
                        }

                        int indexOfEndTag = text.IndexOf("{\\fn}", start, StringComparison.Ordinal);
                        if (indexOfEndTag > 0)
                        {
                            text = text.Remove(indexOfEndTag, "{\\fn}".Length).Insert(indexOfEndTag, "</font>");
                        }
                        else
                        {
                            text += "</font>";
                        }
                    }
                }

                if (text.Contains(@"{\fs", StringComparison.Ordinal))
                {
                    int start = text.IndexOf(@"{\fs", StringComparison.Ordinal);
                    int end = text.IndexOf('}', start);
                    if (end > 0 && !text.Substring(start).StartsWith("{\\fs}", StringComparison.Ordinal))
                    {
                        string fontSize = text.Substring(start + 4, end - (start + 4));
                        string extraTags = string.Empty;
                        CheckAndAddSubTags(ref fontSize, ref extraTags, out bool italic);
                        if (IsInteger(fontSize))
                        {
                            text = text.Remove(start, end - start + 1);
                            if (italic)
                            {
                                text = text.Insert(start, "<font size=\"" + fontSize + "\"" + extraTags + "><i>");
                            }
                            else
                            {
                                text = text.Insert(start, "<font size=\"" + fontSize + "\"" + extraTags + ">");
                            }

                            int indexOfEndTag = text.IndexOf("{\\fs}", start, StringComparison.Ordinal);
                            if (indexOfEndTag > 0)
                            {
                                text = text.Remove(indexOfEndTag, "{\\fs}".Length).Insert(indexOfEndTag, "</font>");
                            }
                            else
                            {
                                text += "</font>";
                            }
                        }
                    }
                }

                if (text.Contains(@"{\c", StringComparison.Ordinal))
                {
                    int start = text.IndexOf(@"{\c", StringComparison.Ordinal);
                    int end = text.IndexOf('}', start);
                    if (end > 0 && !text.Substring(start).StartsWith("{\\c}", StringComparison.Ordinal))
                    {
                        string color = text.Substring(start + 4, end - (start + 4));
                        string extraTags = string.Empty;
                        CheckAndAddSubTags(ref color, ref extraTags, out bool italic);

                        color = color.Replace("&", string.Empty, StringComparison.Ordinal).TrimStart('H');
                        color = color.PadLeft(6, '0');

                        // switch to rrggbb from bbggrr
                        color = "#" + color.Remove(color.Length - 6) + color.Substring(color.Length - 2, 2) + color.Substring(color.Length - 4, 2) + color.Substring(color.Length - 6, 2);
                        color = color.ToLowerInvariant();

                        text = text.Remove(start, end - start + 1);
                        if (italic)
                        {
                            text = text.Insert(start, "<font color=\"" + color + "\"" + extraTags + "><i>");
                        }
                        else
                        {
                            text = text.Insert(start, "<font color=\"" + color + "\"" + extraTags + ">");
                        }

                        int indexOfEndTag = text.IndexOf("{\\c}", start, StringComparison.Ordinal);
                        if (indexOfEndTag > 0)
                        {
                            text = text.Remove(indexOfEndTag, "{\\c}".Length).Insert(indexOfEndTag, "</font>");
                        }
                        else
                        {
                            text += "</font>";
                        }
                    }
                }

                if (text.Contains(@"{\1c", StringComparison.Ordinal)) // "1" specifices primary color
                {
                    int start = text.IndexOf(@"{\1c", StringComparison.Ordinal);
                    int end = text.IndexOf('}', start);
                    if (end > 0 && !text.Substring(start).StartsWith("{\\1c}", StringComparison.Ordinal))
                    {
                        string color = text.Substring(start + 5, end - (start + 5));
                        string extraTags = string.Empty;
                        CheckAndAddSubTags(ref color, ref extraTags, out bool italic);

                        color = color.Replace("&", string.Empty, StringComparison.Ordinal).TrimStart('H');
                        color = color.PadLeft(6, '0');

                        // switch to rrggbb from bbggrr
                        color = "#" + color.Remove(color.Length - 6) + color.Substring(color.Length - 2, 2) + color.Substring(color.Length - 4, 2) + color.Substring(color.Length - 6, 2);
                        color = color.ToLowerInvariant();

                        text = text.Remove(start, end - start + 1);
                        if (italic)
                        {
                            text = text.Insert(start, "<font color=\"" + color + "\"" + extraTags + "><i>");
                        }
                        else
                        {
                            text = text.Insert(start, "<font color=\"" + color + "\"" + extraTags + ">");
                        }

                        text += "</font>";
                    }
                }
            }

            text = text.Replace(@"{\i1}", "<i>", StringComparison.Ordinal);
            text = text.Replace(@"{\i0}", "</i>", StringComparison.Ordinal);
            text = text.Replace(@"{\i}", "</i>", StringComparison.Ordinal);
            if (CountTagInText(text, "<i>") > CountTagInText(text, "</i>"))
            {
                text += "</i>";
            }

            text = text.Replace(@"{\u1}", "<u>", StringComparison.Ordinal);
            text = text.Replace(@"{\u0}", "</u>", StringComparison.Ordinal);
            text = text.Replace(@"{\u}", "</u>", StringComparison.Ordinal);
            if (CountTagInText(text, "<u>") > CountTagInText(text, "</u>"))
            {
                text += "</u>";
            }

            text = text.Replace(@"{\b1}", "<b>", StringComparison.Ordinal);
            text = text.Replace(@"{\b0}", "</b>", StringComparison.Ordinal);
            text = text.Replace(@"{\b}", "</b>", StringComparison.Ordinal);
            if (CountTagInText(text, "<b>") > CountTagInText(text, "</b>"))
            {
                text += "</b>";
            }

            return text;
        }

        private static bool IsInteger(string s)
            => int.TryParse(s, out _);

        private static int CountTagInText(string text, string tag)
        {
            int count = 0;
            int index = text.IndexOf(tag, StringComparison.Ordinal);
            while (index >= 0)
            {
                count++;
                if (index == text.Length)
                {
                    return count;
                }

                index = text.IndexOf(tag, index + 1, StringComparison.Ordinal);
            }

            return count;
        }

        private static void CheckAndAddSubTags(ref string tagName, ref string extraTags, out bool italic)
        {
            italic = false;
            int indexOfSPlit = tagName.IndexOf('\\', StringComparison.Ordinal);
            if (indexOfSPlit > 0)
            {
                string rest = tagName.Substring(indexOfSPlit).TrimStart('\\');
                tagName = tagName.Remove(indexOfSPlit);

                for (int i = 0; i < 10; i++)
                {
                    if (rest.StartsWith("fs", StringComparison.Ordinal) && rest.Length > 2)
                    {
                        indexOfSPlit = rest.IndexOf('\\', StringComparison.Ordinal);
                        string fontSize = rest;
                        if (indexOfSPlit > 0)
                        {
                            fontSize = rest.Substring(0, indexOfSPlit);
                            rest = rest.Substring(indexOfSPlit).TrimStart('\\');
                        }
                        else
                        {
                            rest = string.Empty;
                        }

                        extraTags += " size=\"" + fontSize.Substring(2) + "\"";
                    }
                    else if (rest.StartsWith("fn", StringComparison.Ordinal) && rest.Length > 2)
                    {
                        indexOfSPlit = rest.IndexOf('\\', StringComparison.Ordinal);
                        string fontName = rest;
                        if (indexOfSPlit > 0)
                        {
                            fontName = rest.Substring(0, indexOfSPlit);
                            rest = rest.Substring(indexOfSPlit).TrimStart('\\');
                        }
                        else
                        {
                            rest = string.Empty;
                        }

                        extraTags += " face=\"" + fontName.Substring(2) + "\"";
                    }
                    else if (rest.StartsWith("c", StringComparison.Ordinal) && rest.Length > 2)
                    {
                        indexOfSPlit = rest.IndexOf('\\', StringComparison.Ordinal);
                        string fontColor = rest;
                        if (indexOfSPlit > 0)
                        {
                            fontColor = rest.Substring(0, indexOfSPlit);
                            rest = rest.Substring(indexOfSPlit).TrimStart('\\');
                        }
                        else
                        {
                            rest = string.Empty;
                        }

                        string color = fontColor.Substring(2);
                        color = color.Replace("&", string.Empty, StringComparison.Ordinal).TrimStart('H');
                        color = color.PadLeft(6, '0');
                        // switch to rrggbb from bbggrr
                        color = "#" + color.Remove(color.Length - 6) + color.Substring(color.Length - 2, 2) + color.Substring(color.Length - 4, 2) + color.Substring(color.Length - 6, 2);
                        color = color.ToLowerInvariant();

                        extraTags += " color=\"" + color + "\"";
                    }
                    else if (rest.StartsWith("i1", StringComparison.Ordinal) && rest.Length > 1)
                    {
                        indexOfSPlit = rest.IndexOf('\\', StringComparison.Ordinal);
                        italic = true;
                        if (indexOfSPlit > 0)
                        {
                            rest = rest.Substring(indexOfSPlit).TrimStart('\\');
                        }
                        else
                        {
                            rest = string.Empty;
                        }
                    }
                    else if (rest.Length > 0 && rest.Contains('\\', StringComparison.Ordinal))
                    {
                        indexOfSPlit = rest.IndexOf('\\', StringComparison.Ordinal);
                        rest = rest.Substring(indexOfSPlit).TrimStart('\\');
                    }
                }
            }
        }
    }
}
