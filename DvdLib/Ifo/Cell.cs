#pragma warning disable CS1591

using System.IO;

namespace DvdLib.Ifo
{
    public class Cell
    {
        public Cell(BinaryReader br)
        {
            PositionInfo = new CellPositionInfo(br);
            PlaybackInfo = new CellPlaybackInfo(br);
        }

        public CellPlaybackInfo PlaybackInfo { get; private set; }

        public CellPositionInfo PositionInfo { get; private set; }
    }
}
