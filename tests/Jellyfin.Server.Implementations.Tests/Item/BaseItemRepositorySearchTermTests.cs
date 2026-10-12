using System;
using System.Linq;
using Emby.Server.Implementations.Data;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller.Entities;
using Xunit;
using BaseItemKind = Jellyfin.Data.Enums.BaseItemKind;

namespace Jellyfin.Server.Implementations.Tests.Item;

/// <summary>
/// The search term filter matches <c>OriginalTitle</c>, which is stored in the case the metadata provider
/// wrote it in. The match ignores ASCII case only, treats <c>\</c> as an ordinary character and leaves
/// <c>%</c> and <c>_</c> as wildcards.
/// </summary>
public sealed class BaseItemRepositorySearchTermTests : SqliteDbTestFixture
{
    private readonly BaseItemRepository _repository;
    private readonly ItemTypeLookup _itemTypeLookup = new();

    public BaseItemRepositorySearchTermTests()
    {
        _repository = CreateBaseItemRepository(_itemTypeLookup);

        using var context = CreateDbContext();
        context.BaseItems.Add(CreateMovie("Item One", "hello World"));
        context.BaseItems.Add(CreateMovie("Item Two", "Émile Zola"));
        context.BaseItems.Add(CreateMovie("Item Three", "AC\\DC"));
        context.BaseItems.Add(CreateMovie("Item Four", "ACDC"));
        context.BaseItems.Add(CreateMovie("Item Five", "Alpha Omega"));
        context.BaseItems.Add(CreateMovie("Item Six", "Beta"));
        context.SaveChanges();
    }

    [Theory]
    [InlineData("WORLD")]
    [InlineData("world")]
    [InlineData("wOrLd")]
    public void GetItemList_SearchTermAgainstMixedCaseOriginalTitle_IgnoresAsciiCase(string term)
    {
        Assert.Equal(["hello World"], Search(term));
    }

    [Fact]
    public void GetItemList_SearchTermWithUpperCaseNonAsciiLetter_FindsOriginalTitle()
    {
        Assert.Equal(["Émile Zola"], Search("ÉMILE"));
    }

    [Fact]
    public void GetItemList_SearchTermWithLowerCaseNonAsciiLetter_DoesNotFindUpperCaseOriginalTitle()
    {
        Assert.Empty(Search("émile"));
    }

    [Fact]
    public void GetItemList_BackslashInSearchTerm_IsLiteral()
    {
        Assert.Equal(["AC\\DC"], Search("AC\\DC"));
    }

    [Theory]
    [InlineData("Al%ga", "Alpha Omega")]
    [InlineData("Al_ha", "Alpha Omega")]
    [InlineData("%eta", "Beta")]
    public void GetItemList_PercentAndUnderscoreInSearchTerm_StayWildcards(string term, string expected)
    {
        Assert.Equal([expected], Search(term));
    }

    private string[] Search(string term)
    {
        var result = _repository.GetItemList(new InternalItemsQuery(new User("test", "auth", "reset"))
        {
            IncludeItemTypes = [BaseItemKind.Movie],
            SearchTerm = term
        });

        return result.Select(i => i.OriginalTitle ?? string.Empty).Order(StringComparer.Ordinal).ToArray();
    }

    private BaseItemEntity CreateMovie(string name, string originalTitle)
    {
        var id = Guid.NewGuid();
        return new BaseItemEntity
        {
            Id = id,
            PresentationUniqueKey = id.ToString("N"),
            Type = _itemTypeLookup.BaseItemKindNames[BaseItemKind.Movie],
            Name = name,
            CleanName = name.ToLowerInvariant(),
            SortName = name.ToLowerInvariant(),
            OriginalTitle = originalTitle,
            MediaType = "Video",
            IsMovie = true,
            IsFolder = false,
            IsVirtualItem = false
        };
    }
}
