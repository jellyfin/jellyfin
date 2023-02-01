using System.IO;

namespace DvdLib.Ifo;

/// <summary>
/// A cell.
/// </summary>
public class Cell
{
    /// <summary>
    /// The cell playback information.
    /// </summary>
    public CellPlaybackInfo PlaybackInfo { get; private set; }

    /// <summary>
    /// The cell position information.
    /// </summary>
    public CellPositionInfo PositionInfo { get; private set; }

    internal void ParsePlayback(BinaryReader br)
    {
        PlaybackInfo = new CellPlaybackInfo(br);
    }

    internal void ParsePosition(BinaryReader br)
    {
        PositionInfo = new CellPositionInfo(br);
    }
}
