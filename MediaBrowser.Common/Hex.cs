using System;
using System.Diagnostics.CodeAnalysis;

namespace MediaBrowser.Common
{
    /// <summary>
    /// Encoding and decoding hex strings.
    /// </summary>
    public static class Hex
    {
        internal const string HexCharsLower = "0123456789abcdef";
        internal const string HexCharsUpper = "0123456789ABCDEF";

        internal const int LastHexSymbol = 0x66; // 102: f

        /// <summary>
        /// Gets a map from an ASCII char to its hex value shifted,
        /// e.g. <c>b</c> -> 11. 0xFF means it's not a hex symbol.
        /// </summary>
        internal static ReadOnlySpan<byte> HexLookup => new byte[]
        {
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f
        };

        /// <summary>
        /// Encodes each element of the specified bytes as its hexadecimal string representation.
        /// </summary>
        /// <param name="bytes">An array of bytes.</param>
        /// <param name="lowercase"><c>true</c> to use lowercase hexadecimal characters; otherwise <c>false</c>.</param>
        /// <returns><c>bytes</c> as a hex string.</returns>
        public static string Encode(ReadOnlySpan<byte> bytes, bool lowercase = true)
        {
            var hexChars = lowercase ? HexCharsLower : HexCharsUpper;

            // TODO: use string.Create when it's supports spans
            // Ref: https://github.com/dotnet/corefx/issues/29120
            char[] s = new char[bytes.Length * 2];
            int j = 0;
            for (int i = 0; i < bytes.Length; i++)
            {
                s[j++] = hexChars[bytes[i] >> 4];
                s[j++] = hexChars[bytes[i] & 0x0f];
            }

            return new string(s);
        }

        /// <summary>
        /// Decodes a hex string into bytes.
        /// </summary>
        /// <param name="str">The <see cref="string" />.</param>
        /// <returns>The decoded bytes.</returns>
        public static byte[] Decode(ReadOnlySpan<char> str)
        {
            if (str.Length == 0)
            {
                return Array.Empty<byte>();
            }

            var unHex = HexLookup;

            int byteLen = str.Length / 2;
            byte[] bytes = new byte[byteLen];
            int i = 0;
            for (int j = 0; j < byteLen; j++)
            {
                byte a;
                byte b;
                if (str[i] > LastHexSymbol
                    || (a = unHex[str[i++]]) == 0xFF
                    || str[i] > LastHexSymbol
                    || (b = unHex[str[i++]]) == 0xFF)
                {
                    ThrowArgumentException(nameof(str));
                    break; // Unreachable
                }

                bytes[j] = (byte)((a * 16) | b);
            }

            return bytes;
        }

        [DoesNotReturn]
        private static void ThrowArgumentException(string paramName)
            => throw new ArgumentException("Character is not a hex symbol.", paramName);
    }
}
