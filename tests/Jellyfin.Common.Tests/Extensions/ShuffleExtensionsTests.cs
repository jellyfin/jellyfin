using System;
using MediaBrowser.Common.Extensions;
using Xunit;

namespace Jellyfin.Common.Tests.Extensions
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
