using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketHttpListener.Net
{
    // we use this static class as a helper class to encode/decode HTTP headers.
    // what we need is a 1-1 correspondence between a char in the range U+0000-U+00FF
    // and a byte in the range 0x00-0xFF (which is the range that can hit the network).
    // The Latin-1 encoding (ISO-88591-1) (GetEncoding(28591)) works for byte[] to string, but is a little slow.
    // It doesn't work for string -> byte[] because of best-fit-mapping problems.
    internal static class WebHeaderEncoding
    {
        // We don't want '?' replacement characters, just fail.
        private static readonly Encoding s_utf8Decoder = Encoding.GetEncoding("utf-8", EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);

        internal static unsafe string GetString(byte[] bytes, int byteIndex, int byteCount)
        {
            fixed (byte* pBytes = bytes)
                return GetString(pBytes + byteIndex, byteCount);
        }

        internal static unsafe string GetString(byte* pBytes, int byteCount)
        {
            if (byteCount < 1)
                return "";

            string s = new string('\0', byteCount);

            fixed (char* pStr = s)
            {
                char* pString = pStr;
                while (byteCount >= 8)
                {
                    pString[0] = (char)pBytes[0];
                    pString[1] = (char)pBytes[1];
                    pString[2] = (char)pBytes[2];
                    pString[3] = (char)pBytes[3];
                    pString[4] = (char)pBytes[4];
                    pString[5] = (char)pBytes[5];
                    pString[6] = (char)pBytes[6];
                    pString[7] = (char)pBytes[7];
                    pString += 8;
                    pBytes += 8;
                    byteCount -= 8;
                }
                for (int i = 0; i < byteCount; i++)
                {
                    pString[i] = (char)pBytes[i];
                }
            }

            return s;
        }

        internal static int GetByteCount(string myString)
        {
            return myString.Length;
        }
        internal static unsafe void GetBytes(string myString, int charIndex, int charCount, byte[] bytes, int byteIndex)
        {
            if (myString.Length == 0)
            {
                return;
            }
            fixed (byte* bufferPointer = bytes)
            {
                byte* newBufferPointer = bufferPointer + byteIndex;
                int finalIndex = charIndex + charCount;
                while (charIndex < finalIndex)
                {
                    *newBufferPointer++ = (byte)myString[charIndex++];
                }
            }
        }
        internal static unsafe byte[] GetBytes(string myString)
        {
            byte[] bytes = new byte[myString.Length];
            if (myString.Length != 0)
            {
                GetBytes(myString, 0, myString.Length, bytes, 0);
            }
            return bytes;
        }

        // The normal client header parser just casts bytes to chars (see GetString).
        // Check if those bytes were actually utf-8 instead of ASCII.
        // If not, just return the input value.
        internal static string DecodeUtf8FromString(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return input;
            }

            bool possibleUtf8 = false;
            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] > (char)255)
                {
                    return input; // This couldn't have come from the wire, someone assigned it directly.
                }
                else if (input[i] > (char)127)
                {
                    possibleUtf8 = true;
                    break;
                }
            }
            if (possibleUtf8)
            {
                byte[] rawBytes = new byte[input.Length];
                for (int i = 0; i < input.Length; i++)
                {
                    if (input[i] > (char)255)
                    {
                        return input; // This couldn't have come from the wire, someone assigned it directly.
                    }
                    rawBytes[i] = (byte)input[i];
                }
                try
                {
                    return s_utf8Decoder.GetString(rawBytes);
                }
                catch (ArgumentException) { } // Not actually Utf-8
            }
            return input;
        }
    }
}
