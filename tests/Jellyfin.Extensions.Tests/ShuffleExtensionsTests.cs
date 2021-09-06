using System;
using Xunit;

namespace Jellyfin.Extensions.Tests
{
    public static class ShuffleExtensionsTests
    {
        private static readonly Random _rng = new Random();

        [Fact]
        public static void Shuffle_Valid_Correct()
        {
            byte[] original = new byte[1 << 6];
            _rng.NextBytes(original);
            byte[] shuffled = (byte[])original.Clone();
            shuffled.Shuffle();

            Assert.NotEqual(original, shuffled);
        }
    }
}
