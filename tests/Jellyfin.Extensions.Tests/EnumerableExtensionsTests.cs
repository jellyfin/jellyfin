using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Jellyfin.Extensions.Tests
{
    public static class EnumerableExtensionsTests
    {
        [Flags]
        private enum TestFlags
        {
            A = 1,
            B = 2,
            C = 4
        }

        [Theory]
        [InlineData("b", StringComparison.Ordinal, true)]
        [InlineData("B", StringComparison.Ordinal, false)]
        [InlineData("B", StringComparison.OrdinalIgnoreCase, true)]
        [InlineData("d", StringComparison.OrdinalIgnoreCase, false)]
        public static void Contains_List_Correct(string value, StringComparison comparison, bool expected)
        {
            string[] source = ["a", "b", "c"];

            Assert.Equal(expected, source.Contains(value.AsSpan(), comparison));
        }

        [Theory]
        [InlineData("b", StringComparison.Ordinal, true)]
        [InlineData("B", StringComparison.Ordinal, false)]
        [InlineData("B", StringComparison.OrdinalIgnoreCase, true)]
        [InlineData("d", StringComparison.OrdinalIgnoreCase, false)]
        public static void Contains_Enumerable_Correct(string value, StringComparison comparison, bool expected)
        {
            IEnumerable<string> source = new[] { "a", "b", "c" }.Select(s => s);

            Assert.Equal(expected, source.Contains(value.AsSpan(), comparison));
        }

        [Fact]
        public static void Contains_Null_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => ((IEnumerable<string>)null!).Contains("a".AsSpan(), StringComparison.Ordinal));
        }

        [Fact]
        public static void SingleItemAsEnumerable_Valid_Correct()
        {
            Assert.Equal(["a"], "a".SingleItemAsEnumerable());
        }

        [Fact]
        public static void GetUniqueFlags_Combined_ReturnsEachSetFlag()
        {
            Assert.Equal([TestFlags.A, TestFlags.C], (TestFlags.A | TestFlags.C).GetUniqueFlags());
        }

        [Fact]
        public static void GetUniqueFlags_Single_ReturnsFlag()
        {
            Assert.Equal([TestFlags.B], TestFlags.B.GetUniqueFlags());
        }

        [Fact]
        public static void ToDictionaryLastWins_DuplicateKeys_KeepsLast()
        {
            (int Key, string Value)[] source = [(1, "a"), (2, "b"), (1, "c")];

            var result = source.ToDictionaryLastWins(e => e.Key);

            Assert.Equal(2, result.Count);
            Assert.Equal("c", result[1].Value);
            Assert.Equal("b", result[2].Value);
        }
    }
}
