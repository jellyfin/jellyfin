#pragma warning disable CS1591

using System.IO;

namespace DvdLib.Ifo
{
    public enum BlockMode
    {
        NotInBlock = 0,
        FirstCell = 1,
        InBlock = 2,
        LastCell = 3,
    }

    public enum BlockType
    {
        Normal = 0,
        Angle = 1,
    }

    public enum PlaybackMode
    {
        Normal = 0,
        StillAfterEachVOBU = 1,
    }

    public class CellPlaybackInfo
    {
        public readonly BlockMode Mode;
        public readonly BlockType Type;
        public readonly bool SeamlessPlay;
        public readonly bool Interleaved;
        public readonly bool STCDiscontinuity;
        public readonly bool SeamlessAngle;
        public readonly PlaybackMode PlaybackMode;
        public readonly bool Restricted;
        public readonly byte StillTime;
        public readonly byte CommandNumber;
        public readonly DvdTime PlaybackTime;
        public readonly uint FirstSector;
        public readonly uint FirstILVUEndSector;
        public readonly uint LastVOBUStartSector;
        public readonly uint LastSector;

        internal CellPlaybackInfo(BinaryReader br)
        {
            br.BaseStream.Seek(0x4, SeekOrigin.Current);
            PlaybackTime = new DvdTime(br.ReadBytes(4));
            br.BaseStream.Seek(0x10, SeekOrigin.Current);
        }
    }
}
