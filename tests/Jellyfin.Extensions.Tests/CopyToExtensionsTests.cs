using System;
using System.Collections.Generic;
using Xunit;

namespace Jellyfin.Extensions.Tests
{
    public static class CopyToExtensionsTests
    {
        public static IEnumerable<object[]> CopyTo_Valid_Correct_TestData()
        {
            yield return new object[] { new[] { 0, 1, 2, 3, 4, 5 }, new[] { 0, 0, 0, 0, 0, 0 }, 0, new[] { 0, 1, 2, 3, 4, 5 } };
            yield return new object[] { new[] { 0, 1, 2 }, new[] { 5, 4, 3, 2, 1, 0 }, 2, new[] { 5, 4, 0, 1, 2, 0 } };
        }

        [Theory]
        [MemberData(nameof(CopyTo_Valid_Correct_TestData))]
        public static void CopyTo_Valid_Correct<T>(IReadOnlyList<T> source, IList<T> destination, int index, IList<T> expected)
        {
            source.CopyTo(destination, index);
            Assert.Equal(expected, destination);
        }

        public static IEnumerable<object[]> CopyTo_Invalid_ThrowsArgumentOutOfRangeException_TestData()
        {
            yield return new object[] { new[] { 0, 1, 2, 3, 4, 5 }, new[] { 0, 0, 0, 0, 0, 0 }, -1 };
            yield return new object[] { new[] { 0, 1, 2 }, new[] { 5, 4, 3, 2, 1, 0 }, 6 };
            yield return new object[] { new[] { 0, 1, 2 }, Array.Empty<int>(), 0 };
            yield return new object[] { new[] { 0, 1, 2, 3, 4, 5 }, new[] { 0 }, 0 };
            yield return new object[] { new[] { 0, 1, 2, 3, 4, 5 }, new[] { 0, 0, 0, 0, 0, 0 }, 1 };
        }

        [Theory]
        [MemberData(nameof(CopyTo_Invalid_ThrowsArgumentOutOfRangeException_TestData))]
        public static void CopyTo_Invalid_ThrowsArgumentOutOfRangeException<T>(IReadOnlyList<T> source, IList<T> destination, int index)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => source.CopyTo(destination, index));
        }
    }
}
