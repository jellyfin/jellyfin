using System;
using System.Linq;
using System.Threading;
using Emby.Server.Implementations.Data;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using BaseItemKind = Jellyfin.Data.Enums.BaseItemKind;

namespace Jellyfin.Server.Implementations.Tests.Item;

/// <summary>
/// Covers ratings and gains that are not finite numbers: SQLite refuses to store NaN and neither NaN nor
/// infinity can be written as JSON, so they are stored and read back as no value.
/// </summary>
public sealed class BaseItemMapperNonFiniteRatingTests : SqliteDbTestFixture
{
    private readonly ItemTypeLookup _itemTypeLookup = new();
    private readonly ILibraryManager? _previousLibraryManager;
    private readonly IServerConfigurationManager? _previousConfigurationManager;

    public BaseItemMapperNonFiniteRatingTests()
    {
        // BaseItem resolves these through process-wide statics; restored in Dispose.
        _previousLibraryManager = BaseItem.LibraryManager;
        _previousConfigurationManager = BaseItem.ConfigurationManager;

        var libraryManager = new Mock<ILibraryManager>();
        libraryManager.Setup(l => l.GetCollectionFolders(It.IsAny<BaseItem>()))
            .Returns([]);
        BaseItem.LibraryManager = libraryManager.Object;

        var configurationManager = new Mock<IServerConfigurationManager>();
        configurationManager.Setup(c => c.Configuration).Returns(new ServerConfiguration());
        BaseItem.ConfigurationManager = configurationManager.Object;
    }

    protected override void Dispose(bool disposing)
    {
        BaseItem.LibraryManager = _previousLibraryManager!;
        BaseItem.ConfigurationManager = _previousConfigurationManager!;
        base.Dispose(disposing);
    }

    [Theory]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public void SaveItems_NonFiniteRatings_StoresNoValueForThem(float value)
    {
        var nonFinite = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var finite = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        // Saved in the same batch as an item whose values are finite, which must be stored as they are.
        new ItemPersistenceService(
                CreateDbContextFactory(),
                new Mock<IServerApplicationHost>().Object,
                NullLogger<ItemPersistenceService>.Instance)
            .SaveItems(
                [
                    new Book
                    {
                        Id = nonFinite,
                        Name = "Not finite",
                        CommunityRating = value,
                        CriticRating = value,
                        LUFS = value,
                        NormalizationGain = value
                    },
                    new Book
                    {
                        Id = finite,
                        Name = "Finite",
                        CommunityRating = 7.5f,
                        CriticRating = 82f,
                        LUFS = -14.2f,
                        NormalizationGain = -3.5f
                    }
                ],
                CancellationToken.None);

        using var context = CreateDbContext();
        var stored = context.BaseItems.Single(e => e.Id.Equals(nonFinite));
        Assert.Null(stored.CommunityRating);
        Assert.Null(stored.CriticRating);
        Assert.Null(stored.LUFS);
        Assert.Null(stored.NormalizationGain);

        var unchanged = context.BaseItems.Single(e => e.Id.Equals(finite));
        Assert.Equal(7.5f, unchanged.CommunityRating);
        Assert.Equal(82f, unchanged.CriticRating);
        Assert.Equal(-14.2f, unchanged.LUFS);
        Assert.Equal(-3.5f, unchanged.NormalizationGain);
    }

    [Fact]
    public void RetrieveItem_StoredInfiniteRatings_ReadsNoValueForThem()
    {
        // Unlike NaN, infinity is stored by SQLite, so a database can already hold it.
        var id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        using (var context = CreateDbContext())
        {
            context.BaseItems.Add(new BaseItemEntity
            {
                Id = id,
                Type = _itemTypeLookup.BaseItemKindNames[BaseItemKind.Book],
                Name = "Not finite",
                CommunityRating = float.PositiveInfinity,
                CriticRating = float.NegativeInfinity,
                LUFS = float.NegativeInfinity,
                NormalizationGain = float.PositiveInfinity
            });
            context.SaveChanges();
        }

        var item = CreateBaseItemRepository(_itemTypeLookup).RetrieveItem(id);

        Assert.NotNull(item);
        Assert.Null(item.CommunityRating);
        Assert.Null(item.CriticRating);
        Assert.Null(item.LUFS);
        Assert.Null(item.NormalizationGain);
    }
}
