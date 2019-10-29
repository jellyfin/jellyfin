#pragma warning disable CS1591

using System;
using System.Globalization;

namespace MediaBrowser.Common
{
    public static class HexHelper
    {
        public static byte[] FromHexString(string str)
        {
            byte[] bytes = new byte[str.Length / 2];
            for (int i = 0; i < str.Length; i += 2)
            {
                bytes[i / 2] = byte.Parse(str.Substring(i, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            }

            return bytes;
        }

        public static string ToHexString(byte[] bytes)
            => BitConverter.ToString(bytes).Replace("-", "");
    }
}
