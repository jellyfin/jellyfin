using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            return BitConverter.ToUInt16(ReadAndReverseBytes(2), 0);
        }

        public override uint ReadUInt32()
        {
            return BitConverter.ToUInt32(ReadAndReverseBytes(4), 0);
        }

        private byte[] ReadAndReverseBytes(int count)
        {
            byte[] val = base.ReadBytes(count);
            Array.Reverse(val, 0, count);
            return val;
        }
    }
}
