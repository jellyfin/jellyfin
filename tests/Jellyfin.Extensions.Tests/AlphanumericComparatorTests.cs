using System;
using System.Linq;
using Xunit;

namespace Jellyfin.Extensions.Tests
{
    public class AlphanumericComparatorTests
    {
        // InlineData is pre-sorted
        [Theory]
        [InlineData(null, "", "1", "9", "10", "a", "z")]
        [InlineData("50F", "100F", "SR9", "SR100")]
        [InlineData("image-1.jpg", "image-02.jpg", "image-4.jpg", "image-9.jpg", "image-10.jpg", "image-11.jpg", "image-22.jpg")]
        [InlineData("Hard drive 2GB", "Hard drive 20GB")]
        [InlineData("b", "e", "è", "ě", "f", "g", "k")]
        [InlineData("123456789", "123456789a", "abc", "abcd")]
        [InlineData("12345678912345678912345678913234567891", "123456789123456789123456789132345678912")]
        [InlineData("12345678912345678912345678913234567891", "12345678912345678912345678913234567891")]
        [InlineData("12345678912345678912345678913234567891", "12345678912345678912345678913234567892")]
        [InlineData("12345678912345678912345678913234567891a", "12345678912345678912345678913234567891a")]
        [InlineData("12345678912345678912345678913234567891a", "12345678912345678912345678913234567891b")]
        [InlineData("a5", "a11")]
        [InlineData("a05a", "a5b")]
        [InlineData("a5a", "a05b")]
        [InlineData("6xxx", "007asdf")]
        [InlineData("00042Q", "42s")]
        public void AlphanumericComparatorTest(params string?[] strings)
        {
            var copy = strings.Reverse().ToArray();
            Array.Sort(copy, new AlphanumericComparator());
            Assert.Equal(strings, copy);
        }
    }
}
