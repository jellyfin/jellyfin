using System;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Extensions.Json;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Model.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Item;

public class ItemPersistenceMediaInfoTests : SqliteDbTestFixture
{
    private readonly ItemPersistenceService _service;

    public ItemPersistenceMediaInfoTests()
    {
        var host = new Mock<IServerApplicationHost>();
        host.Setup(h => h.ReverseVirtualPath(It.IsAny<string>())).Returns((string path) => path);
        _service = new ItemPersistenceService(CreateDbContextFactory(), host.Object, NullLogger<ItemPersistenceService>.Instance);
    }

    [Fact]
    public async Task SaveMediaInfo_StaleDescription_DoesNotOverwriteMetadataOrRelationships()
    {
        var id = Guid.NewGuid();
        var primary = Guid.NewGuid();
        var stamp = DateTime.UtcNow.AddDays(-10);
        var key = JsonDefaults.Options.PropertyNamingPolicy?.ConvertName(nameof(Video.PrimaryVersionId)) ?? nameof(Video.PrimaryVersionId);
        var stored = new JsonObject { [key] = primary.ToString(), ["unrecognizedPluginData"] = "keep" };
        using (var context = CreateDbContext())
        {
            context.BaseItems.Add(new BaseItemEntity
            {
                Id = id, Type = typeof(Movie).FullName!, Path = "/media/movie.mkv", Name = "Edited", Genres = "Edited",
                ProductionLocations = string.Empty, IsLocked = true, DateLastRefreshed = stamp, Data = stored.ToJsonString(),
                PrimaryVersionId = primary, LockedFields = [new BaseItemMetadataField { Id = (int)MetadataField.Genres, ItemId = id, Item = null! }]
            });
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var stale = new Movie
        {
            Id = id, Path = "/media/movie.mkv", Name = "Old", Genres = ["Old"], ProductionLocations = ["Old"],
            RunTimeTicks = TimeSpan.TicksPerHour, Container = "mkv", Width = 1920, Height = 1080
        };
        await _service.SaveMediaInfoAsync(stale, TestContext.Current.CancellationToken);
        using (var context = CreateDbContext())
        {
            var saved = context.BaseItems.Include(e => e.LockedFields).Single(e => e.Id.Equals(id));
            Assert.Equal("Edited", saved.Name);
            Assert.Equal("Edited", saved.Genres);
            Assert.Equal(string.Empty, saved.ProductionLocations);
            Assert.True(saved.IsLocked);
            Assert.Equal(primary, saved.PrimaryVersionId);
            Assert.Equal((int)MetadataField.Genres, Assert.Single(saved.LockedFields!).Id);
            Assert.Equal(stamp, saved.DateLastRefreshed);
            Assert.Equal(TimeSpan.TicksPerHour, saved.RunTimeTicks);
            Assert.Equal(1920, saved.Width);
            var data = JsonNode.Parse(saved.Data!)!;
            Assert.Equal(primary.ToString(), data[key]!.GetValue<string>());
            Assert.Equal("keep", data["unrecognizedPluginData"]!.GetValue<string>());
        }
    }
}
