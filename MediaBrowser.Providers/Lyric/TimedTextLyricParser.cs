using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Jellyfin.Extensions;
using MediaBrowser.Controller.Lyrics;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Model.Lyrics;
using TtmlLyricParser;
using LyricLine = MediaBrowser.Model.Lyrics.LyricLine;

namespace MediaBrowser.Providers.Lyric;

/// <summary>
/// TTML Lyric Parser.
/// </summary>
public class TimedTextLyricParser : ILyricParser
{
    private readonly TtmlParser _ttmlParser;

    private static readonly string[] _supportedMediaTypes = [".ttml"];

    /// <summary>
    /// Initializes a new instance of the <see cref="TimedTextLyricParser"/> class.
    /// </summary>
    public TimedTextLyricParser()
    {
        _ttmlParser = new TtmlParser();
    }

    /// <inheritdoc />
    public string Name => "TtmlLyricProvider";

    /// <summary>
    /// Gets the priority.
    /// </summary>
    /// <value>The priority.</value>
    public ResolverPriority Priority => ResolverPriority.Fourth;

    /// <inheritdoc />
    public LyricDto? ParseLyrics(LyricFile lyrics)
    {
        if (!_supportedMediaTypes.Contains(Path.GetExtension(lyrics.Name.AsSpan()), StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        SongLyrics? lyricData;

        try
        {
            using MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(lyrics.Content), false);
            var result = _ttmlParser.Parse(stream);
            lyricData = result.Success ? result.Lyrics : null;
        }
        catch (Exception)
        {
            // Failed to parse, return null so the next parser will be tried
            return null;
        }

        if (lyricData is null)
        {
            return null;
        }

        List<TtmlLyricParser.LyricLine> lines = [.. lyricData.Lines];
        if (lines.Count == 0)
        {
            return null;
        }

        // Untimed documents resolve every line to the origin; treat those as unsynced lyrics
        var isSynced = lines.Any(x => x.Timing.Begin != TimeSpan.Zero || x.Timing.End is not null);
        if (isSynced)
        {
            lines = [.. lines.OrderBy(x => x.Timing.Begin)];
        }

        List<LyricLine> lyricList = [];
        foreach (var line in lines)
        {
            var text = new StringBuilder();
            var cues = new List<LyricLineCue>();
            if (isSynced)
            {
                AppendContent(line.Content, line.Timing.End, text, cues);
            }
            else
            {
                AppendContent(line.Content, null, text, null);
            }

            lyricList.Add(new LyricLine(
                text.ToString(),
                isSynced ? line.Timing.Begin.Ticks : null,
                isSynced ? cues : null));
        }

        return new LyricDto { Lyrics = lyricList };
    }

    /// <summary>
    /// Flattens lyric content into <paramref name="text"/>, adding a cue for each innermost timed span.
    /// Background vocals are kept so the line text matches what is sung.
    /// </summary>
    private static void AppendContent(IEnumerable<LyricContent> content, TimeSpan? parentEnd, StringBuilder text, List<LyricLineCue>? cues)
    {
        foreach (var item in content)
        {
            switch (item)
            {
                case LyricText lyricText:
                    text.Append(lyricText.Value);
                    break;
                case LyricBreak:
                    text.Append('\n');
                    break;
                case LyricSpan span:
                    var end = span.Timing.End ?? parentEnd;
                    if (span.Content.Any(x => x is LyricSpan))
                    {
                        AppendContent(span.Content, end, text, cues);
                        break;
                    }

                    var position = text.Length;
                    AppendContent(span.Content, end, text, null);
                    if (cues is not null && !string.IsNullOrWhiteSpace(text.ToString(position, text.Length - position)))
                    {
                        cues.Add(new LyricLineCue(
                            position: position,
                            endPosition: text.Length,
                            start: span.Timing.Begin.Ticks,
                            end: end?.Ticks));
                    }

                    break;
            }
        }
    }
}
