using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
