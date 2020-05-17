#pragma warning disable CS1591

using System.Buffers.Binary;
using System.IO;

namespace DvdLib
{
    public class BigEndianBinaryReader : BinaryReader
    {
        public BigEndianBinaryReader(Stream input)
            : base(input)
        {
        }

        public override ushort ReadUInt16()
        {
            return BinaryPrimitives.ReadUInt16BigEndian(base.ReadBytes(2));
        }

        public override uint ReadUInt32()
        {
            return BinaryPrimitives.ReadUInt32BigEndian(base.ReadBytes(4));
        }
    }
}
