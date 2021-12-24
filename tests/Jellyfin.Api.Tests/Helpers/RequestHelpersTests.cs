using System;
using System.Collections.Generic;
using Jellyfin.Api.Helpers;
using Jellyfin.Data.Enums;
using Xunit;

namespace Jellyfin.Api.Tests.Helpers
{
    public static class RequestHelpersTests
    {
        [Theory]
        [MemberData(nameof(GetOrderBy_Success_TestData))]
        public static void GetOrderBy_Success(IReadOnlyList<string> sortBy, IReadOnlyList<SortOrder> requestedSortOrder, (string, SortOrder)[] expected)
        {
            Assert.Equal(expected, RequestHelpers.GetOrderBy(sortBy, requestedSortOrder));
        }

        public static TheoryData<IReadOnlyList<string>, IReadOnlyList<SortOrder>, (string, SortOrder)[]> GetOrderBy_Success_TestData()
        {
            var data = new TheoryData<IReadOnlyList<string>, IReadOnlyList<SortOrder>, (string, SortOrder)[]>();

            data.Add(
                Array.Empty<string>(),
                Array.Empty<SortOrder>(),
                Array.Empty<(string, SortOrder)>());

            data.Add(
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
                });

            data.Add(
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
                });

            return data;
        }
    }
}
