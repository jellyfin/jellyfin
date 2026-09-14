using System;
using System.Linq;
using System.Threading;
using Emby.Server.Implementations.Data;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Item;

public sealed class ItemPersistenceCompanyTests : SqliteDbTestFixture
{
    private readonly ItemPersistenceService _service;
    private readonly BaseItemRepository _repository;
    private readonly ILibraryManager? _previousLibraryManager;
    private readonly IServerConfigurationManager? _previousConfigurationManager;

    public ItemPersistenceCompanyTests()
    {
        // BaseItem resolves these through process-wide statics; restored in Dispose.
        _previousLibraryManager = BaseItem.LibraryManager;
        _previousConfigurationManager = BaseItem.ConfigurationManager;

        var libraryManager = new Mock<ILibraryManager>();
        libraryManager.Setup(l => l.GetCollectionFolders(It.IsAny<BaseItem>())).Returns([]);
        BaseItem.LibraryManager = libraryManager.Object;

        var configurationManager = new Mock<IServerConfigurationManager>();
        configurationManager.Setup(c => c.Configuration).Returns(new ServerConfiguration());
        BaseItem.ConfigurationManager = configurationManager.Object;

        _service = new ItemPersistenceService(
            CreateDbContextFactory(),
            new Mock<IServerApplicationHost>().Object,
            NullLogger<ItemPersistenceService>.Instance);
        _repository = CreateBaseItemRepository(new ItemTypeLookup());
    }

    [Fact]
    public void SaveItems_CreditedCompanies_AreStoredOnTheItemAndInTheMappingTable()
    {
        var id = Guid.NewGuid();

        _service.SaveItems([CreateSeries(id, ("HBO", CompanyKind.Network), ("Bad Robot", CompanyKind.Studio))], CancellationToken.None);

        using (var ctx = CreateDbContext())
        {
            Assert.Equal(2, ctx.Companies.Count());
            Assert.Equal(2, ctx.CompanyBaseItemMap.Count(e => e.ItemId.Equals(id)));
            Assert.Equal(
                (CompanyKindEntity)CompanyKind.Network,
                ctx.CompanyBaseItemMap.Single(e => e.CompanyId.Equals(Company.GetCompanyId("HBO"))).Type);
        }

        var saved = Assert.IsType<Series>(_repository.RetrieveItem(id));
        Assert.Equal(["Bad Robot"], saved.GetCompanyNames(CompanyKind.Studio));
        Assert.Equal(["HBO"], saved.GetCompanyNames(CompanyKind.Network));
    }

    [Fact]
    public void SaveItems_CompanyDropped_LosesItsMapping()
    {
        var id = Guid.NewGuid();

        _service.SaveItems([CreateSeries(id, ("HBO", CompanyKind.Network), ("Bad Robot", CompanyKind.Studio))], CancellationToken.None);
        _service.SaveItems([CreateSeries(id, ("HBO", CompanyKind.Network))], CancellationToken.None);

        using var ctx = CreateDbContext();
        var map = Assert.Single(ctx.CompanyBaseItemMap.Where(e => e.ItemId.Equals(id)));
        Assert.Equal(Company.GetCompanyId("HBO"), map.CompanyId);
    }

    [Fact]
    public void SaveItems_SameNameDifferentKinds_IsOneCompanyWithTwoCredits()
    {
        var id = Guid.NewGuid();

        _service.SaveItems([CreateSeries(id, ("HBO", CompanyKind.Network), ("HBO", CompanyKind.Studio))], CancellationToken.None);

        using var ctx = CreateDbContext();
        Assert.Equal(1, ctx.Companies.Count(e => e.Name == "HBO"));
        Assert.Equal(2, ctx.CompanyBaseItemMap.Count(e => e.ItemId.Equals(id)));
    }

    protected override void Dispose(bool disposing)
    {
        BaseItem.LibraryManager = _previousLibraryManager!;
        BaseItem.ConfigurationManager = _previousConfigurationManager!;
        base.Dispose(disposing);
    }

    private static Series CreateSeries(Guid id, params (string Name, CompanyKind Type)[] companies)
    {
        return new Series
        {
            Id = id,
            Name = "Westworld",
            Companies = [.. companies.Select(e => new CompanyInfo { Name = e.Name, Type = e.Type })]
        };
    }
}
