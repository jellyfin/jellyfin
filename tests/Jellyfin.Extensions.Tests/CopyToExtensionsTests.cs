using System;
using System.Collections.Generic;
using Xunit;

namespace Jellyfin.Extensions.Tests
{
    public static class CopyToExtensionsTests
    {
        public static TheoryData<IReadOnlyList<int>, IList<int>, int, IList<int>> CopyTo_Valid_Correct_TestData()
        {
            var data = new TheoryData<IReadOnlyList<int>, IList<int>, int, IList<int>>
            {
                { new[] { 0, 1, 2, 3, 4, 5 }, new[] { 0, 0, 0, 0, 0, 0 }, 0, new[] { 0, 1, 2, 3, 4, 5 } },
                { new[] { 0, 1, 2 }, new[] { 5, 4, 3, 2, 1, 0 }, 2, new[] { 5, 4, 0, 1, 2, 0 } }
            };

            return data;
        }

        [Theory]
        [MemberData(nameof(CopyTo_Valid_Correct_TestData))]
        public static void CopyTo_Valid_Correct(IReadOnlyList<int> source, IList<int> destination, int index, IList<int> expected)
        {
            source.CopyTo(destination, index);
            Assert.Equal(expected, destination);
        }

        public static TheoryData<IReadOnlyList<int>, IList<int>, int> CopyTo_Invalid_ThrowsArgumentOutOfRangeException_TestData()
        {
            var data = new TheoryData<IReadOnlyList<int>, IList<int>, int>
            {
                { new[] { 0, 1, 2, 3, 4, 5 }, new[] { 0, 0, 0, 0, 0, 0 }, -1 },
                { new[] { 0, 1, 2 }, new[] { 5, 4, 3, 2, 1, 0 }, 6 },
                { new[] { 0, 1, 2 }, Array.Empty<int>(), 0 },
                { new[] { 0, 1, 2, 3, 4, 5 }, new[] { 0 }, 0 },
                { new[] { 0, 1, 2, 3, 4, 5 }, new[] { 0, 0, 0, 0, 0, 0 }, 1 }
            };

            return data;
        }

        [Theory]
        [MemberData(nameof(CopyTo_Invalid_ThrowsArgumentOutOfRangeException_TestData))]
        public static void CopyTo_Invalid_ThrowsArgumentOutOfRangeException(IReadOnlyList<int> source, IList<int> destination, int index)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => source.CopyTo(destination, index));
        }
    }
}
