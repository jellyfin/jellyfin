using System.Collections.Generic;
using System.Linq;

namespace MediaBrowser.Model.Lyrics;

/// <summary>
/// LyricResponse model.
/// </summary>
public class LyricDto
{
    /// <summary>
    /// Gets or sets Metadata for the lyrics.
    /// </summary>
    public LyricMetadata Metadata { get; set; } = new();

    /// <summary>
    /// Gets or sets the lyrics tracks.
    /// </summary>
    public IReadOnlyList<LyricTrack> Tracks { get; set; } = [];

    /// <summary>
    /// Gets or sets a collection of individual lyric lines.
    /// </summary>
    public IReadOnlyList<LyricLine> Lyrics
    {
        get => Tracks.FirstOrDefault(i => i.Type == LyricTrackType.Main)?.Lines ?? [];
        set
        {
            var tracks = Tracks.ToList();
            var mainTrackIndex = tracks.FindIndex(i => i.Type == LyricTrackType.Main);
            if (mainTrackIndex >= 0)
            {
                tracks[mainTrackIndex].Lines = value;
            }
            else
            {
                tracks.Insert(0, new LyricTrack { Type = LyricTrackType.Main, Lines = value });
            }

            Tracks = tracks;
        }
    }
}
