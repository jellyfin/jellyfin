using System;
using System.Collections.Generic;
using Jellyfin.Api.Helpers;
using Jellyfin.Data.Enums;
using Xunit;

namespace Jellyfin.Api.Tests.Helpers
{
    public class RequestHelpersTests
    {
        [Theory]
        [MemberData(nameof(GetOrderBy_Success_TestData))]
        public void GetOrderBy_Success(IReadOnlyList<string> sortBy, IReadOnlyList<SortOrder> requestedSortOrder, (string, SortOrder)[] expected)
        {
            Assert.Equal(expected, RequestHelpers.GetOrderBy(sortBy, requestedSortOrder));
        }

        public static IEnumerable<object[]> GetOrderBy_Success_TestData()
        {
            yield return new object[]
            {
                Array.Empty<string>(),
                Array.Empty<SortOrder>(),
                Array.Empty<(string, SortOrder)>()
            };
            yield return new object[]
            {
                new string[]
                {
                    "IsFavoriteOrLiked",
                    "Random"
                },
                Array.Empty<SortOrder>(),
                new (string, SortOrder)[]
                {
                    ("IsFavoriteOrLiked", SortOrder.Ascending),
                    ("Random", SortOrder.Ascending),
                }
            };
            yield return new object[]
            {
                new string[]
                {
                    "SortName",
                    "ProductionYear"
                },
                new SortOrder[]
                {
                    SortOrder.Descending
                },
                new (string, SortOrder)[]
                {
                    ("SortName", SortOrder.Descending),
                    ("ProductionYear", SortOrder.Descending),
                }
            };
        }
    }
}
