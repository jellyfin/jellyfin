using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using MediaBrowser.Common;

namespace Jellyfin.Common.Benches
{
    [MemoryDiagnoser]
    public class HexEncodeBenches
    {
        private byte[] _data;

        [Params(0, 10, 100, 1000, 10000, 1000000)]
        public int N { get; set; }

        [GlobalSetup]
        public void GlobalSetup()
        {
            _data = new byte[N];
            new Random(42).NextBytes(_data);
        }

        [Benchmark]
        public string HexEncode() => Hex.Encode(_data);

        [Benchmark]
        public string BitConverterToString() => BitConverter.ToString(_data);

        [Benchmark]
        public string BitConverterToStringWithReplace() => BitConverter.ToString(_data).Replace("-", "");
    }
}
