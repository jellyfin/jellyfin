using System;
using System.Linq;
using Emby.Server.Implementations.Data;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Persistence;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Item;

public sealed class NextUpSeriesKeysTests : SqliteDbTestFixture
{
    [Theory]
    [InlineData(null, 0)]
    [InlineData(0, 0)]
    [InlineData(1, 0)]
    [InlineData(20, 0)]
    [InlineData(null, 1)]
    [InlineData(null, 3)]
    public void Keys_PreserveDateCutoffScopeAndLimit(int? limit, int cutoffDays)
    {
        using var context = CreateDbContext();
        var user = new User("test", "auth", "reset");
        var other = new User("other", "auth", "reset");
        context.Users.AddRange(user, other);
        var parent = Guid.NewGuid();
        var date = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);

        void Add(string? key, DateTime? played, Guid owner, Guid root, string type = "MediaBrowser.Controller.Entities.TV.Episode", Guid? itemId = null)
        {
            var id = itemId ?? Guid.NewGuid();
            var item = context.BaseItems.Find(id);
            if (item is null)
            {
                item = new BaseItemEntity { Id = id, Type = type };
                context.BaseItems.Add(item);
            }

            item.Type = type;
            item.Name = key ?? "null";
            item.SortName = key ?? "null";
            item.SeriesPresentationUniqueKey = key;
            item.TopParentId = root;
            item.PresentationUniqueKey = id.ToString("N");
            context.UserData.Add(new UserData
            {
                ItemId = id, UserId = owner, CustomDataKey = id.ToString("N"),
                LastPlayedDate = played, Item = null!, User = null!
            });
        }

        Add("newest", date.AddDays(2), user.Id, parent);
        Add("newest", date.AddDays(-1), user.Id, parent);
        Add("boundary", date, user.Id, parent);
        Add("old", date.AddDays(-1), user.Id, parent);
        Add("undated", null, user.Id, parent);
        Add("foreign-user", date.AddDays(5), other.Id, parent);
        Add("foreign-library", date.AddDays(5), user.Id, Guid.NewGuid());
        Add("movie", date.AddDays(5), user.Id, parent, "MediaBrowser.Controller.Entities.Movies.Movie");
        Add(null, date.AddDays(5), user.Id, parent);
        Add("placeholder", date.AddDays(5), user.Id, parent, itemId: BaseItemRepository.PlaceholderId);
        context.SaveChanges();

        var service = new NextUpService(CreateDbContextFactory(), new ItemTypeLookup(), Mock.Of<IItemQueryHelpers>());
        var result = service.GetNextUpSeriesKeys(
            new InternalItemsQuery(user)
            {
                TopParentIds = new[] { parent }, Limit = limit
            },
            date.AddDays(cutoffDays));
        var expected = cutoffDays == 0 ? new[] { "newest", "boundary" } : cutoffDays == 1 ? new[] { "newest" } : Array.Empty<string>();
        Assert.Equal(expected.Take(limit ?? int.MaxValue), result);
    }
}
