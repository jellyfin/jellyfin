using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BDInfo
{
    using System.Diagnostics;
    using System.Text;
    using System;
    using Microsoft.Win32;

    /// <devdoc>
    ///    <para>Provides a simple light bit vector with easy integer or Boolean access to
    ///       a 32 bit storage.</para>
    /// </devdoc>
    public struct BitVector32
    {
        private uint data;

        /// <devdoc>
        /// <para>Initializes a new instance of the BitVector32 structure with the specified internal data.</para>
        /// </devdoc>
        public BitVector32(int data)
        {
            this.data = (uint)data;
        }

        /// <devdoc>
        /// <para>Initializes a new instance of the BitVector32 structure with the information in the specified 
        ///    value.</para>
        /// </devdoc>
        public BitVector32(BitVector32 value)
        {
            this.data = value.data;
        }

        /// <devdoc>
        ///    <para>Gets or sets a value indicating whether all the specified bits are set.</para>
        /// </devdoc>
        public bool this[int bit]
        {
            get
            {
                return (data & bit) == (uint)bit;
            }
            set
            {
                if (value)
                {
                    data |= (uint)bit;
                }
                else
                {
                    data &= ~(uint)bit;
                }
            }
        }

        /// <devdoc>
        ///    <para>Gets or sets the value for the specified section.</para>
        /// </devdoc>
        public int this[Section section]
        {
            get
            {
                return (int)((data & (uint)(section.Mask << section.Offset)) >> section.Offset);
            }
            set
            {
#if DEBUG
                if ((value & section.Mask) != value) {
                    Debug.Fail("Value out of bounds on BitVector32 Section Set!");
                }
#endif
                value <<= section.Offset;
                int offsetMask = (0xFFFF & (int)section.Mask) << section.Offset;
                data = (data & ~(uint)offsetMask) | ((uint)value & (uint)offsetMask);
            }
        }

        /// <devdoc>
        ///    returns the raw data stored in this bit vector...
        /// </devdoc>
        public int Data
        {
            get
            {
                return (int)data;
            }
        }

        private static short CountBitsSet(short mask)
        {

            // yes, I know there are better algorithms, however, we know the
            // bits are always right aligned, with no holes (i.e. always 00000111,
            // never 000100011), so this is just fine...
            //
            short value = 0;
            while ((mask & 0x1) != 0)
            {
                value++;
                mask >>= 1;
            }
            return value;
        }

        /// <devdoc>
        ///    <para> Creates the first mask in a series.</para>
        /// </devdoc>
        public static int CreateMask()
        {
            return CreateMask(0);
        }

        /// <devdoc>
        ///     Creates the next mask in a series.
        /// </devdoc>
        public static int CreateMask(int previous)
        {
            if (previous == 0)
            {
                return 1;
            }

            if (previous == unchecked((int)0x80000000))
            {
                throw new InvalidOperationException("Bit vector full");
            }

            return previous << 1;
        }

        /// <devdoc>
        ///     Given a highValue, creates the mask
        /// </devdoc>
        private static short CreateMaskFromHighValue(short highValue)
        {
            short required = 16;
            while ((highValue & 0x8000) == 0)
            {
                required--;
                highValue <<= 1;
            }

            ushort value = 0;
            while (required > 0)
            {
                required--;
                value <<= 1;
                value |= 0x1;
            }

            return unchecked((short)value);
        }

        /// <devdoc>
        ///    <para>Creates the first section in a series, with the specified maximum value.</para>
        /// </devdoc>
        public static Section CreateSection(short maxValue)
        {
            return CreateSectionHelper(maxValue, 0, 0);
        }

        /// <devdoc>
        ///    <para>Creates the next section in a series, with the specified maximum value.</para>
        /// </devdoc>
        public static Section CreateSection(short maxValue, Section previous)
        {
            return CreateSectionHelper(maxValue, previous.Mask, previous.Offset);
        }

        private static Section CreateSectionHelper(short maxValue, short priorMask, short priorOffset)
        {
            if (maxValue < 1)
            {
                throw new ArgumentOutOfRangeException("maxValue");
            }
#if DEBUG
            int maskCheck = CreateMaskFromHighValue(maxValue);
            int offsetCheck = priorOffset + CountBitsSet(priorMask);
            Debug.Assert(maskCheck <= short.MaxValue && offsetCheck < 32, "Overflow on BitVector32");
#endif
            short offset = (short)(priorOffset + CountBitsSet(priorMask));
            if (offset >= 32)
            {
                throw new InvalidOperationException("Bit vector full");
            }
            return new Section(CreateMaskFromHighValue(maxValue), offset);
        }

        public override bool Equals(object o)
        {
            if (!(o is BitVector32))
            {
                return false;
            }

            return data == ((BitVector32)o).data;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <devdoc>
        /// </devdoc>
        public static string ToString(BitVector32 value)
        {
            StringBuilder sb = new StringBuilder(/*"BitVector32{".Length*/12 + /*32 bits*/32 + /*"}".Length"*/1);
            sb.Append("BitVector32{");
            int locdata = (int)value.data;
            for (int i = 0; i < 32; i++)
            {
                if ((locdata & 0x80000000) != 0)
                {
                    sb.Append("1");
                }
                else
                {
                    sb.Append("0");
                }
                locdata <<= 1;
            }
            sb.Append("}");
            return sb.ToString();
        }

        /// <devdoc>
        /// </devdoc>
        public override string ToString()
        {
            return BitVector32.ToString(this);
        }

        /// <devdoc>
        ///    <para> 
        ///       Represents an section of the vector that can contain a integer number.</para>
        /// </devdoc>
        public struct Section
        {
            private readonly short mask;
            private readonly short offset;

            internal Section(short mask, short offset)
            {
                this.mask = mask;
                this.offset = offset;
            }

            public short Mask
            {
                get
                {
                    return mask;
                }
            }

            public short Offset
            {
                get
                {
                    return offset;
                }
            }

            public override bool Equals(object o)
            {
                if (o is Section)
                    return Equals((Section)o);
                else
                    return false;
            }

            public bool Equals(Section obj)
            {
                return obj.mask == mask && obj.offset == offset;
            }

            public static bool operator ==(Section a, Section b)
            {
                return a.Equals(b);
            }

            public static bool operator !=(Section a, Section b)
            {
                return !(a == b);
            }

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }

            /// <devdoc>
            /// </devdoc>
            public static string ToString(Section value)
            {
                return "Section{0x" + Convert.ToString(value.Mask, 16) + ", 0x" + Convert.ToString(value.Offset, 16) + "}";
            }

            /// <devdoc>
            /// </devdoc>
            public override string ToString()
            {
                return Section.ToString(this);
            }

        }
    }
}
