using System.Linq;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.XbmcMetadata.Savers;
using Xunit;

namespace Jellyfin.XbmcMetadata.Tests.Savers
{
    public class BaseNfoSaverTests
    {
        [Fact]
        public void GetActorsInSaveOrder_NoSortOrder_KeepsOriginalOrder()
        {
            // AniList/AniDB style: no SortOrder, list order is the billing order.
            var people = new[]
            {
                new PersonInfo { Name = "Zoe", Type = PersonKind.Actor },
                new PersonInfo { Name = "Mike", Type = PersonKind.Actor },
                new PersonInfo { Name = "Alice", Type = PersonKind.Actor },
            };

            var result = BaseNfoSaver.GetActorsInSaveOrder(people).Select(p => p.Name);

            Assert.Equal(new[] { "Zoe", "Mike", "Alice" }, result);
        }

        [Fact]
        public void GetActorsInSaveOrder_WithSortOrder_OrdersBySortOrder()
        {
            // TMDb style: SortOrder is set and takes precedence over list order.
            var people = new[]
            {
                new PersonInfo { Name = "Alice", Type = PersonKind.Actor, SortOrder = 2 },
                new PersonInfo { Name = "Zoe", Type = PersonKind.Actor, SortOrder = 0 },
                new PersonInfo { Name = "Mike", Type = PersonKind.Actor, SortOrder = 1 },
            };

            var result = BaseNfoSaver.GetActorsInSaveOrder(people).Select(p => p.Name);

            Assert.Equal(new[] { "Zoe", "Mike", "Alice" }, result);
        }

        [Fact]
        public void GetActorsInSaveOrder_SkipsDirectorsAndWriters()
        {
            var people = new[]
            {
                new PersonInfo { Name = "Director", Type = PersonKind.Director },
                new PersonInfo { Name = "Zoe", Type = PersonKind.Actor },
                new PersonInfo { Name = "Writer", Type = PersonKind.Writer },
                new PersonInfo { Name = "Alice", Type = PersonKind.GuestStar },
            };

            var result = BaseNfoSaver.GetActorsInSaveOrder(people).Select(p => p.Name);

            Assert.Equal(new[] { "Zoe", "Alice" }, result);
        }
    }
}
