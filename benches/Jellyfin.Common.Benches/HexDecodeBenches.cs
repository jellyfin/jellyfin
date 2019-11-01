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
        private string _data;

        [Params(0, 10, 100, 1000, 10000, 1000000)]
        public int N { get; set; }

        [GlobalSetup]
        public void GlobalSetup()
        {
            var bytes = new byte[N];
            new Random(42).NextBytes(bytes);
            _data = Hex.Encode(bytes);
        }

        [Benchmark]
        public byte[] Decode() => Hex.Decode(_data);

        [Benchmark]
        public byte[] DecodeSubString() => DecodeSubString(_data);

        private static byte[] DecodeSubString(string str)
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
    }
}
