using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using MediaBrowser.Common;

namespace Jellyfin.Common.Benches
{
    [MemoryDiagnoser]
    public class HexEncodeBenches
    {
        private const int N = 1000;
        private readonly byte[] data;

        public HexEncodeBenches()
        {
            data = new byte[N];
            new Random(42).NextBytes(data);
        }

        [Benchmark]
        public string HexEncode() => Hex.Encode(data);

        [Benchmark]
        public string BitConverterToString() => BitConverter.ToString(data);

        [Benchmark]
        public string BitConverterToStringWithReplace() => BitConverter.ToString(data).Replace("-", "");
    }
}
