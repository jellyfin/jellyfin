using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Item;

/// <summary>
/// The cleaned name and the cleaned value are derived from the text without its NUL characters.
/// </summary>
public sealed class ItemPersistenceNulMatchingTests : SqliteDbTestFixture
{
    private readonly ItemPersistenceService _service;
    private readonly ILibraryManager? _previousLibraryManager;
    private readonly IServerConfigurationManager? _previousConfigurationManager;
    private readonly IRecordingsManager? _previousRecordingsManager;

    public ItemPersistenceNulMatchingTests()
    {
        // BaseItem resolves these through process-wide statics; restored in Dispose.
        _previousLibraryManager = BaseItem.LibraryManager;
        _previousConfigurationManager = BaseItem.ConfigurationManager;
        _previousRecordingsManager = Video.RecordingsManager;

        var libraryManager = new Mock<ILibraryManager>();
        libraryManager.Setup(l => l.GetCollectionFolders(It.IsAny<BaseItem>()))
            .Returns([]);
        BaseItem.LibraryManager = libraryManager.Object;

        var configurationManager = new Mock<IServerConfigurationManager>();
        configurationManager.Setup(c => c.Configuration).Returns(new ServerConfiguration());
        BaseItem.ConfigurationManager = configurationManager.Object;

        Video.RecordingsManager = new Mock<IRecordingsManager>().Object;

        _service = new ItemPersistenceService(
            CreateDbContextFactory(),
            new Mock<IServerApplicationHost>().Object,
            NullLogger<ItemPersistenceService>.Instance);
    }

    protected override void Dispose(bool disposing)
    {
        BaseItem.LibraryManager = _previousLibraryManager!;
        BaseItem.ConfigurationManager = _previousConfigurationManager!;
        Video.RecordingsManager = _previousRecordingsManager!;
        base.Dispose(disposing);
    }

    [Fact]
    public void SaveItems_NameWithNul_StoresTheCleanNameAndCleanValueWithoutIt()
    {
        var item = CreateBook(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), name: "Mid\0dle", genres: ["Mid\0dle"]);

        _service.SaveItems([item], CancellationToken.None);

        using var ctx = CreateDbContext();
        var entity = Assert.Single(ctx.BaseItems.Where(e => e.Id.Equals(item.Id)));
        Assert.Equal("middle", entity.CleanName);
        Assert.Equal("middle", Assert.Single(ctx.ItemValues).CleanValue);
    }

    private static Book CreateBook(Guid id, string name = "Book", string[]? genres = null, Dictionary<string, string>? providerIds = null)
    {
        return new Book
        {
            Id = id,
            Name = name,
            Genres = genres ?? [],
            ProviderIds = providerIds ?? []
        };
    }
}
