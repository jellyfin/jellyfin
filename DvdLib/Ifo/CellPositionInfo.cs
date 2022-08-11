#pragma warning disable CS1591

using System.IO;

namespace DvdLib.Ifo
{
    public class CellPositionInfo
    {
        public readonly ushort VOBId;
        public readonly byte CellId;

        internal CellPositionInfo(BinaryReader br)
        {
            VOBId = br.ReadUInt16();
            br.ReadByte();
            CellId = br.ReadByte();
        }
    }
}
