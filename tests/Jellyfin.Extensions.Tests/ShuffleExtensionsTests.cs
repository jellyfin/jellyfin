using System;
using Xunit;

namespace Jellyfin.Extensions.Tests
{
    public static class ShuffleExtensionsTests
    {
        [Fact]
        public static void Shuffle_Valid_Correct()
        {
            byte[] original = new byte[1 << 6];
            Random.Shared.NextBytes(original);
            byte[] shuffled = (byte[])original.Clone();
            shuffled.Shuffle();

            Assert.NotEqual(original, shuffled);
        }
    }
}
