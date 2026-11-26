#nullable disable

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
    public MediaStream[] MediaStreams { get; set; }

    /// <summary>
    /// Gets or sets the run time ticks.
    /// </summary>
    /// <value>The run time ticks.</value>
    public long? RunTimeTicks { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the disc was read from an ISO image.
    /// </summary>
    /// <value><c>true</c> when the disc was read from an ISO image.</value>
    public bool IsIso { get; set; }

    /// <summary>
    /// Gets or sets the files.
    /// </summary>
    /// <value>The files.</value>
    public string[] Files { get; set; }

    /// <summary>
    /// Gets or sets the playlist name.
    /// </summary>
    /// <value>The playlist name.</value>
    public string PlaylistName { get; set; }

    /// <summary>
    /// Gets or sets the chapters.
    /// </summary>
    /// <value>The chapters.</value>
    public double[] Chapters { get; set; }
}
