#nullable disable

using System.Collections.Generic;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Model.MediaInfo;

/// <summary>
/// Represents the result of BDInfo output.
/// </summary>
public class BlurayDiscInfo
{
    /// <summary>
    /// Gets or sets the media streams.
    /// </summary>
    /// <value>The media streams.</value>
    public IReadOnlyList<MediaStream> MediaStreams { get; set; }

    /// <summary>
    /// Gets or sets the run time ticks.
    /// </summary>
    /// <value>The run time ticks.</value>
    public long? RunTimeTicks { get; set; }

    /// <summary>
    /// Gets or sets the files.
    /// </summary>
    /// <value>The files.</value>
    public IReadOnlyList<string> Files { get; set; }

    /// <summary>
    /// Gets or sets the playlist name.
    /// </summary>
    /// <value>The playlist name.</value>
    public string PlaylistName { get; set; }

    /// <summary>
    /// Gets or sets the chapters.
    /// </summary>
    /// <value>The chapters.</value>
    public IReadOnlyList<double> Chapters { get; set; }
}
