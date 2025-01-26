using System;
using Emby.Server.Implementations.Sorting;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Sorting
{
    public class PremiereDateComparerTests
    {
        private readonly PremiereDateComparer _cmp = new PremiereDateComparer();

        [Theory]
        [ClassData(typeof(PremiereDateTestData))]
        public void PremiereDateCompareTest(BaseItem x, BaseItem y, int expected)
        {
            Assert.Equal(expected, _cmp.Compare(x, y));
            Assert.Equal(-expected, _cmp.Compare(y, x));
        }

        private sealed class PremiereDateTestData : TheoryData<BaseItem, BaseItem, int>
        {
            public PremiereDateTestData()
            {
                // Happy case - Both have premier date
                // Expected: x listed first
                Add(
                    new Movie { PremiereDate = new DateTime(2018, 1, 1) },
                    new Movie { PremiereDate = new DateTime(2018, 1, 3) },
                    -1);

                // Both have premiere date, but y has invalid date
                // Expected: y listed first
                Add(
                    new Movie { PremiereDate = new DateTime(2019, 1, 1) },
                    new Movie { PremiereDate = new DateTime(03, 1, 1) },
                    1);

                // Only x has premiere date, with earlier year than y
                // Expected: x listed first
                Add(
                    new Movie { PremiereDate = new DateTime(2020, 1, 1) },
                    new Movie { ProductionYear = 2021 },
                    -1);

                // Only x has premiere date, with same year as y
                // Expected: y listed first
                Add(
                    new Movie { PremiereDate = new DateTime(2022, 1, 2) },
                    new Movie { ProductionYear = 2022 },
                    1);

                // Only x has a premiere date, with later year than y
                // Expected: y listed first
                Add(
                    new Movie { PremiereDate = new DateTime(2024, 3, 1) },
                    new Movie { ProductionYear = 2023 },
                    1);

                // Only x has a premiere date, y has an invalid year
                // Expected: y listed first
                Add(
                    new Movie { PremiereDate = new DateTime(2025, 1, 1) },
                    new Movie { ProductionYear = 0 },
                    1);

                // Only x has a premiere date, y has neither date nor year
                // Expected: y listed first
                Add(
                    new Movie { PremiereDate = new DateTime(2026, 1, 1) },
                    new Movie(),
                    1);
            }
        }
    }
}
