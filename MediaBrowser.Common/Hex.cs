using System;
using System.Globalization;

namespace MediaBrowser.Common
{
    /// <summary>
    /// Encoding and decoding hex strings.
    /// </summary>
    public static class Hex
    {
        internal const string HexCharsLower = "0123456789abcdef";
        internal const string HexCharsUpper = "0123456789ABCDEF";

        /// <summary>
        /// Encodes <c>bytes</c> as a hex string.
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="lowercase"></param>
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
            byte[] bytes = new byte[str.Length / 2];
            int j = 0;
            for (int i = 0; i < str.Length; i += 2)
            {
                bytes[j++] = byte.Parse(
                    str.Slice(i, 2),
                    NumberStyles.HexNumber,
                    CultureInfo.InvariantCulture);
            }

            return bytes;
        }
    }
}
