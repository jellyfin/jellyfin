using System;
using System.Globalization;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using MediaBrowser.Common;

namespace Jellyfin.Common.Benches
{
    [MemoryDiagnoser]
    public class HexDecodeBenches
    {
        private const int N = 1000000;
        private readonly string data;

        public HexDecodeBenches()
        {
            var tmp = new byte[N];
            new Random(42).NextBytes(tmp);
            data = Hex.Encode(tmp);
        }

        public static byte[] DecodeSubString(string str)
        {
            byte[] bytes = new byte[str.Length / 2];
            for (int i = 0; i < str.Length; i += 2)
            {
                bytes[i / 2] = byte.Parse(
                    str.Substring(i, 2),
                    NumberStyles.HexNumber,
                    CultureInfo.InvariantCulture);
            }

            return bytes;
        }

        [Benchmark]
        public byte[] Decode() => Hex.Decode(data);

        [Benchmark]
        public byte[] DecodeSubString() => DecodeSubString(data);
    }
}
