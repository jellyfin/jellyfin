namespace Jellyfin.MediaEncoding.Keyframes.Matroska.Models;

/// <summary>
/// The matroska SeekHead segment. All positions are relative to the Segment container.
/// </summary>
internal class SeekHead
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SeekHead"/> class.
    /// </summary>
    /// <param name="infoPosition">The relative file position of the info segment.</param>
    /// <param name="tracksPosition">The relative file position of the tracks segment.</param>
    /// <param name="cuesPosition">The relative file position of the cues segment.</param>
    public SeekHead(long infoPosition, long tracksPosition, long cuesPosition)
    {
        InfoPosition = infoPosition;
        TracksPosition = tracksPosition;
        CuesPosition = cuesPosition;
    }

    /// <summary>
    /// Gets relative file position of the info segment.
    /// </summary>
    public long InfoPosition { get; }

    /// <summary>
    /// Gets the relative file position of the tracks segment.
    /// </summary>
    public long TracksPosition { get; }

    /// <summary>
    /// Gets the relative file position of the cues segment.
    /// </summary>
    public long CuesPosition { get; }
}
