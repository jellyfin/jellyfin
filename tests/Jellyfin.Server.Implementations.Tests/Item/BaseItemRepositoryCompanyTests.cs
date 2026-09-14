using System;
using System.Linq;
using Emby.Server.Implementations.Data;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller.Entities;
using Xunit;
using BaseItemKind = Jellyfin.Data.Enums.BaseItemKind;

namespace Jellyfin.Server.Implementations.Tests.Item;

/// <summary>
/// Companies are matched by id rather than by cleaned name, so unlike the item values behind the
/// other by-name endpoints the same name can stand for two different companies.
/// </summary>
public sealed class BaseItemRepositoryCompanyTests : SqliteDbTestFixture
{
    private readonly BaseItemRepository _repository;
    private readonly ItemTypeLookup _itemTypeLookup;

    public BaseItemRepositoryCompanyTests()
    {
        _itemTypeLookup = new ItemTypeLookup();
        _repository = CreateBaseItemRepository(_itemTypeLookup);
    }

    [Fact]
    public void GetCompanies_ReturnsTheCompaniesCreditedOnAnItem()
    {
        SeedSeries("Westworld", ("HBO", CompanyKind.Network), ("Bad Robot", CompanyKind.Studio));

        var result = _repository.GetCompanies(new InternalItemsQuery());

        Assert.Equal(2, result.TotalRecordCount);
        Assert.Equal(["Bad Robot", "HBO"], result.Items.Select(e => e.Name).Order());
    }

    [Fact]
    public void GetCompanies_FilteredByType_ReturnsOnlyThatKind()
    {
        SeedSeries("Westworld", ("HBO", CompanyKind.Network), ("Bad Robot", CompanyKind.Studio));

        var result = _repository.GetCompanies(new InternalItemsQuery { CompanyTypes = [CompanyKind.Network] });

        var company = Assert.Single(result.Items);
        Assert.Equal("HBO", company.Name);
    }

    [Fact]
    public void GetCompanies_SameNameDifferentKinds_IsOneCompanyUnderEitherKind()
    {
        SeedSeries("Westworld", ("HBO", CompanyKind.Network), ("HBO", CompanyKind.Studio));

        var all = _repository.GetCompanies(new InternalItemsQuery());
        var networks = _repository.GetCompanies(new InternalItemsQuery { CompanyTypes = [CompanyKind.Network] });
        var studios = _repository.GetCompanies(new InternalItemsQuery { CompanyTypes = [CompanyKind.Studio] });

        // One company, credited twice, so it is listed once and answers to either kind.
        Assert.Equal(1, all.TotalRecordCount);
        Assert.Equal("HBO", Assert.Single(networks.Items).Name);
        Assert.Equal("HBO", Assert.Single(studios.Items).Name);
    }

    [Fact]
    public void GetCompanies_UncreditedCompany_IsNotReturned()
    {
        SeedSeries("Westworld", ("HBO", CompanyKind.Network));
        SeedCompany("Dead Letters");

        var result = _repository.GetCompanies(new InternalItemsQuery());

        var company = Assert.Single(result.Items);
        Assert.Equal("HBO", company.Name);
    }

    [Fact]
    public void GetItemList_ByCompanyId_ReturnsOnlyWhatThatCompanyIsCreditedOn()
    {
        SeedSeries("Westworld", ("HBO", CompanyKind.Network));
        SeedSeries("Breaking Bad", ("AMC", CompanyKind.Network));

        var items = _repository.GetItemList(new InternalItemsQuery
        {
            CompanyIds = [Company.GetCompanyId("HBO")]
        });

        var item = Assert.Single(items);
        Assert.Equal("Westworld", item.Name);
    }

    [Fact]
    public void DeleteOrphanedCompanies_RemovesOnlyTheUncreditedOnes()
    {
        SeedSeries("Westworld", ("HBO", CompanyKind.Network));
        SeedCompany("Dead Letters");

        Assert.Equal(1, _repository.DeleteOrphanedCompanies());
        Assert.Equal(["HBO"], _repository.GetAllCompanies().Select(e => e.Name));
    }

    private void SeedCompany(string name)
    {
        using var ctx = CreateDbContext();

        ctx.Companies.Add(new CompanyEntity
        {
            Id = Company.GetCompanyId(name),
            Name = name,
            CleanName = name.ToLowerInvariant()
        });

        ctx.SaveChanges();
    }

    /// <summary>
    /// Creates a series credited to the given companies, along with the by-name item each company
    /// is listed as. The by-name item takes the company's own id.
    /// </summary>
    private void SeedSeries(string seriesName, params (string Name, CompanyKind Type)[] companies)
    {
        using var ctx = CreateDbContext();

        var seriesId = Guid.NewGuid();
        ctx.BaseItems.Add(new BaseItemEntity
        {
            Id = seriesId,
            Type = _itemTypeLookup.BaseItemKindNames[BaseItemKind.Series],
            Name = seriesName,
            CleanName = seriesName.ToLowerInvariant(),
            PresentationUniqueKey = seriesId.ToString("N"),
            IsFolder = true,
            IsVirtualItem = false
        });

        foreach (var (name, type) in companies)
        {
            var companyId = Company.GetCompanyId(name);

            if (ctx.Companies.Find(companyId) is null)
            {
                ctx.Companies.Add(new CompanyEntity
                {
                    Id = companyId,
                    Name = name,
                    CleanName = name.ToLowerInvariant()
                });

                ctx.BaseItems.Add(new BaseItemEntity
                {
                    Id = companyId,
                    Type = _itemTypeLookup.BaseItemKindNames[BaseItemKind.Company],
                    Name = name,
                    CleanName = name.ToLowerInvariant(),
                    PresentationUniqueKey = companyId.ToString("N"),
                    IsFolder = true,
                    IsVirtualItem = false
                });
            }

            ctx.CompanyBaseItemMap.Add(new CompanyBaseItemMap
            {
                Item = null!,
                ItemId = seriesId,
                Company = null!,
                CompanyId = companyId,
                Type = (CompanyKindEntity)type
            });
        }

        ctx.SaveChanges();
    }
}
