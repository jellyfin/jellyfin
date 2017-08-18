//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Text;

namespace SharpCifs.Util.Sharpen
{ // **************************************************************
// * Raw implementation of the MD5 hash algorithm
// * from RFC 1321.
// *
// * Written By: Reid Borsuk and Jenny Zheng
// * Copyright (c) Microsoft Corporation.  All rights reserved.
// **************************************************************

// Simple struct for the (a,b,c,d) which is used to compute the mesage digest.    
    struct AbcdStruct
    {
        public uint A;
        public uint B;
        public uint C;
        public uint D;
    }

    public sealed class Md5Core
    {
        //Prevent CSC from adding a default public constructor
        private Md5Core() { }

        public static byte[] GetHash(string input, Encoding encoding)
        {
            if (null == input)
                throw new ArgumentNullException("input", "Unable to calculate hash over null input data");
            if (null == encoding)
                throw new ArgumentNullException("encoding", "Unable to calculate hash over a string without a default encoding. Consider using the GetHash(string) overload to use UTF8 Encoding");

            byte[] target = encoding.GetBytes(input);

            return GetHash(target);
        }

        public static byte[] GetHash(string input)
        {
            return GetHash(input, new UTF8Encoding());
        }

        public static string GetHashString(byte[] input)
        {
            if (null == input)
                throw new ArgumentNullException("input", "Unable to calculate hash over null input data");

            string retval = BitConverter.ToString(GetHash(input));
            retval = retval.Replace("-", "");

            return retval;
        }

        public static string GetHashString(string input, Encoding encoding)
        {
            if (null == input)
                throw new ArgumentNullException("input", "Unable to calculate hash over null input data");
            if (null == encoding)
                throw new ArgumentNullException("encoding", "Unable to calculate hash over a string without a default encoding. Consider using the GetHashString(string) overload to use UTF8 Encoding");

            byte[] target = encoding.GetBytes(input);

            return GetHashString(target);
        }

        public static string GetHashString(string input)
        {
            return GetHashString(input, new UTF8Encoding());
        }

        public static byte[] GetHash(byte[] input)
        {
            if (null == input)
                throw new ArgumentNullException("input", "Unable to calculate hash over null input data");

            //Intitial values defined in RFC 1321
            AbcdStruct abcd = new AbcdStruct();
            abcd.A = 0x67452301;
            abcd.B = 0xefcdab89;
            abcd.C = 0x98badcfe;
            abcd.D = 0x10325476;

            //We pass in the input array by block, the final block of data must be handled specialy for padding & length embeding
            int startIndex = 0;
            while (startIndex <= input.Length - 64)
            {
                GetHashBlock(input, ref abcd, startIndex);
                startIndex += 64;
            }
            // The final data block. 
            return GetHashFinalBlock(input, startIndex, input.Length - startIndex, abcd, (Int64)input.Length * 8);
        }

        internal static byte[] GetHashFinalBlock(byte[] input, int ibStart, int cbSize, AbcdStruct abcd, Int64 len)
        {
            byte[] working = new byte[64];
            byte[] length = BitConverter.GetBytes(len);

            //Padding is a single bit 1, followed by the number of 0s required to make size congruent to 448 modulo 512. Step 1 of RFC 1321  
            //The CLR ensures that our buffer is 0-assigned, we don't need to explicitly set it. This is why it ends up being quicker to just
            //use a temporary array rather then doing in-place assignment (5% for small inputs)
            Array.Copy(input, ibStart, working, 0, cbSize);
            working[cbSize] = 0x80;

            //We have enough room to store the length in this chunk
            if (cbSize < 56)
            {
                Array.Copy(length, 0, working, 56, 8);
                GetHashBlock(working, ref abcd, 0);
            }
            else  //We need an aditional chunk to store the length
            {
                GetHashBlock(working, ref abcd, 0);
                //Create an entirely new chunk due to the 0-assigned trick mentioned above, to avoid an extra function call clearing the array
                working = new byte[64];
                Array.Copy(length, 0, working, 56, 8);
                GetHashBlock(working, ref abcd, 0);
            }
            byte[] output = new byte[16];
            Array.Copy(BitConverter.GetBytes(abcd.A), 0, output, 0, 4);
            Array.Copy(BitConverter.GetBytes(abcd.B), 0, output, 4, 4);
            Array.Copy(BitConverter.GetBytes(abcd.C), 0, output, 8, 4);
            Array.Copy(BitConverter.GetBytes(abcd.D), 0, output, 12, 4);
            return output;
        }

        // Performs a single block transform of MD5 for a given set of ABCD inputs
        /* If implementing your own hashing framework, be sure to set the initial ABCD correctly according to RFC 1321:
    //    A = 0x67452301;
    //    B = 0xefcdab89;
    //    C = 0x98badcfe;
    //    D = 0x10325476;
    */
        internal static void GetHashBlock(byte[] input, ref AbcdStruct abcdValue, int ibStart)
        {
            uint[] temp = Converter(input, ibStart);
            uint a = abcdValue.A;
            uint b = abcdValue.B;
            uint c = abcdValue.C;
            uint d = abcdValue.D;

            a = R1(a, b, c, d, temp[0], 7, 0xd76aa478);
            d = R1(d, a, b, c, temp[1], 12, 0xe8c7b756);
            c = R1(c, d, a, b, temp[2], 17, 0x242070db);
            b = R1(b, c, d, a, temp[3], 22, 0xc1bdceee);
            a = R1(a, b, c, d, temp[4], 7, 0xf57c0faf);
            d = R1(d, a, b, c, temp[5], 12, 0x4787c62a);
            c = R1(c, d, a, b, temp[6], 17, 0xa8304613);
            b = R1(b, c, d, a, temp[7], 22, 0xfd469501);
            a = R1(a, b, c, d, temp[8], 7, 0x698098d8);
            d = R1(d, a, b, c, temp[9], 12, 0x8b44f7af);
            c = R1(c, d, a, b, temp[10], 17, 0xffff5bb1);
            b = R1(b, c, d, a, temp[11], 22, 0x895cd7be);
            a = R1(a, b, c, d, temp[12], 7, 0x6b901122);
            d = R1(d, a, b, c, temp[13], 12, 0xfd987193);
            c = R1(c, d, a, b, temp[14], 17, 0xa679438e);
            b = R1(b, c, d, a, temp[15], 22, 0x49b40821);

            a = R2(a, b, c, d, temp[1], 5, 0xf61e2562);
            d = R2(d, a, b, c, temp[6], 9, 0xc040b340);
            c = R2(c, d, a, b, temp[11], 14, 0x265e5a51);
            b = R2(b, c, d, a, temp[0], 20, 0xe9b6c7aa);
            a = R2(a, b, c, d, temp[5], 5, 0xd62f105d);
            d = R2(d, a, b, c, temp[10], 9, 0x02441453);
            c = R2(c, d, a, b, temp[15], 14, 0xd8a1e681);
            b = R2(b, c, d, a, temp[4], 20, 0xe7d3fbc8);
            a = R2(a, b, c, d, temp[9], 5, 0x21e1cde6);
            d = R2(d, a, b, c, temp[14], 9, 0xc33707d6);
            c = R2(c, d, a, b, temp[3], 14, 0xf4d50d87);
            b = R2(b, c, d, a, temp[8], 20, 0x455a14ed);
            a = R2(a, b, c, d, temp[13], 5, 0xa9e3e905);
            d = R2(d, a, b, c, temp[2], 9, 0xfcefa3f8);
            c = R2(c, d, a, b, temp[7], 14, 0x676f02d9);
            b = R2(b, c, d, a, temp[12], 20, 0x8d2a4c8a);

            a = R3(a, b, c, d, temp[5], 4, 0xfffa3942);
            d = R3(d, a, b, c, temp[8], 11, 0x8771f681);
            c = R3(c, d, a, b, temp[11], 16, 0x6d9d6122);
            b = R3(b, c, d, a, temp[14], 23, 0xfde5380c);
            a = R3(a, b, c, d, temp[1], 4, 0xa4beea44);
            d = R3(d, a, b, c, temp[4], 11, 0x4bdecfa9);
            c = R3(c, d, a, b, temp[7], 16, 0xf6bb4b60);
            b = R3(b, c, d, a, temp[10], 23, 0xbebfbc70);
            a = R3(a, b, c, d, temp[13], 4, 0x289b7ec6);
            d = R3(d, a, b, c, temp[0], 11, 0xeaa127fa);
            c = R3(c, d, a, b, temp[3], 16, 0xd4ef3085);
            b = R3(b, c, d, a, temp[6], 23, 0x04881d05);
            a = R3(a, b, c, d, temp[9], 4, 0xd9d4d039);
            d = R3(d, a, b, c, temp[12], 11, 0xe6db99e5);
            c = R3(c, d, a, b, temp[15], 16, 0x1fa27cf8);
            b = R3(b, c, d, a, temp[2], 23, 0xc4ac5665);

            a = R4(a, b, c, d, temp[0], 6, 0xf4292244);
            d = R4(d, a, b, c, temp[7], 10, 0x432aff97);
            c = R4(c, d, a, b, temp[14], 15, 0xab9423a7);
            b = R4(b, c, d, a, temp[5], 21, 0xfc93a039);
            a = R4(a, b, c, d, temp[12], 6, 0x655b59c3);
            d = R4(d, a, b, c, temp[3], 10, 0x8f0ccc92);
            c = R4(c, d, a, b, temp[10], 15, 0xffeff47d);
            b = R4(b, c, d, a, temp[1], 21, 0x85845dd1);
            a = R4(a, b, c, d, temp[8], 6, 0x6fa87e4f);
            d = R4(d, a, b, c, temp[15], 10, 0xfe2ce6e0);
            c = R4(c, d, a, b, temp[6], 15, 0xa3014314);
            b = R4(b, c, d, a, temp[13], 21, 0x4e0811a1);
            a = R4(a, b, c, d, temp[4], 6, 0xf7537e82);
            d = R4(d, a, b, c, temp[11], 10, 0xbd3af235);
            c = R4(c, d, a, b, temp[2], 15, 0x2ad7d2bb);
            b = R4(b, c, d, a, temp[9], 21, 0xeb86d391);

            abcdValue.A = unchecked(a + abcdValue.A);
            abcdValue.B = unchecked(b + abcdValue.B);
            abcdValue.C = unchecked(c + abcdValue.C);
            abcdValue.D = unchecked(d + abcdValue.D);
        }

        //Manually unrolling these equations nets us a 20% performance improvement
        private static uint R1(uint a, uint b, uint c, uint d, uint x, int s, uint t)
        {
            //                  (b + LSR((a + F(b, c, d) + x + t), s))
            //F(x, y, z)        ((x & y) | ((x ^ 0xFFFFFFFF) & z))
            return unchecked(b + Lsr((a + ((b & c) | ((b ^ 0xFFFFFFFF) & d)) + x + t), s));
        }

        private static uint R2(uint a, uint b, uint c, uint d, uint x, int s, uint t)
        {
            //                  (b + LSR((a + G(b, c, d) + x + t), s))
            //G(x, y, z)        ((x & z) | (y & (z ^ 0xFFFFFFFF)))
            return unchecked(b + Lsr((a + ((b & d) | (c & (d ^ 0xFFFFFFFF))) + x + t), s));
        }

        private static uint R3(uint a, uint b, uint c, uint d, uint x, int s, uint t)
        {
            //                  (b + LSR((a + H(b, c, d) + k + i), s))
            //H(x, y, z)        (x ^ y ^ z)
            return unchecked(b + Lsr((a + (b ^ c ^ d) + x + t), s));
        }

        private static uint R4(uint a, uint b, uint c, uint d, uint x, int s, uint t)
        {
            //                  (b + LSR((a + I(b, c, d) + k + i), s))
            //I(x, y, z)        (y ^ (x | (z ^ 0xFFFFFFFF)))
            return unchecked(b + Lsr((a + (c ^ (b | (d ^ 0xFFFFFFFF))) + x + t), s));
        }

        // Implementation of left rotate
        // s is an int instead of a uint becuase the CLR requires the argument passed to >>/<< is of 
        // type int. Doing the demoting inside this function would add overhead.
        private static uint Lsr(uint i, int s)
        {
            return ((i << s) | (i >> (32 - s)));
        }

        //Convert input array into array of UInts
        private static uint[] Converter(byte[] input, int ibStart)
        {
            if (null == input)
                throw new ArgumentNullException("input", "Unable convert null array to array of uInts");

            uint[] result = new uint[16];

            for (int i = 0; i < 16; i++)
            {
                result[i] = input[ibStart + i * 4];
                result[i] += (uint)input[ibStart + i * 4 + 1] << 8;
                result[i] += (uint)input[ibStart + i * 4 + 2] << 16;
                result[i] += (uint)input[ibStart + i * 4 + 3] << 24;
            }

            return result;
        }
    }
}