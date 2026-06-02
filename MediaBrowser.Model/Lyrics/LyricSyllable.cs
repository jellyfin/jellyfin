using System;
using System.Collections.Generic;

namespace MediaBrowser.Model.Lyrics;

/// <summary>
/// Syllable-level lyric timing.
/// </summary>
public class LyricSyllable
{
    /// <summary>
    /// Gets or sets the syllable text.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the start time in ticks.
    /// </summary>
    public long Start { get; set; }

    /// <summary>
    /// Gets or sets the end time in ticks.
    /// </summary>
    public long? End { get; set; }

    /// <summary>
    /// Gets or sets the phonetic text.
    /// </summary>
    public string? Phonetic { get; set; }

    internal static IReadOnlyList<LyricSyllable> FromCues(string text, IReadOnlyList<LyricLineCue> cues)
    {
        var syllables = new List<LyricSyllable>(cues.Count);
        foreach (var cue in cues)
        {
            if (cue.Position < 0
                || cue.EndPosition < cue.Position
                || cue.Position >= text.Length)
            {
                continue;
            }

            var length = Math.Min(cue.EndPosition, text.Length) - cue.Position;
            if (length <= 0)
            {
                continue;
            }

            syllables.Add(new LyricSyllable
            {
                Text = text.Substring(cue.Position, length),
                Start = cue.Start,
                End = cue.End
            });
        }

        return syllables;
    }
}
