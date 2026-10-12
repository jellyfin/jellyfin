using System;
using System.Linq;
using Emby.Server.Implementations.Data;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Extensions;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller.Entities;
using Xunit;
using BaseItemKind = Jellyfin.Data.Enums.BaseItemKind;

namespace Jellyfin.Server.Implementations.Tests.Item;

/// <summary>
/// The name-contains filter looks in the normalized <c>CleanName</c> and in <c>OriginalTitle</c>, which keeps the
/// case it was written in.
/// </summary>
public sealed class BaseItemRepositoryNameContainsTests : JellyfinDbTestFixture
{
    private readonly BaseItemRepository _repository;
    private readonly ItemTypeLookup _itemTypeLookup = new();

    public BaseItemRepositoryNameContainsTests()
    {
        _repository = CreateBaseItemRepository(_itemTypeLookup);

        using (var context = CreateDbContext())
        {
            context.BaseItems.Add(CreateMovie("Star Wars", originalTitle: null));
            context.BaseItems.Add(CreateMovie("Item One", originalTitle: "hello World"));
            context.BaseItems.Add(CreateMovie("Émile Zola", originalTitle: null));
            context.BaseItems.Add(CreateMovie("Item Three", "AC\\DC"));
            context.BaseItems.Add(CreateMovie("Item Four", "ACDC"));
            context.BaseItems.Add(CreateMovie("Item Five", "Alpha Omega"));
            context.BaseItems.Add(CreateMovie("Item Six", "Spider-Man"));
            context.SaveChanges();
        }
    }

    [Theory]
    [InlineData("Star")]
    [InlineData("STAR")]
    [InlineData("star")]
    [InlineData("tar WARS")]
    [InlineData("Star  Wars")]
    public void GetItemList_NameContainsAgainstCleanName_IgnoresCase(string term)
    {
        Assert.Equal(["Star Wars"], Search(term));
    }

    [Theory]
    [InlineData("Émile")]
    [InlineData("emile")]
    public void GetItemList_NameContainsAgainstCleanName_IgnoresDiacritics(string term)
    {
        Assert.Equal(["Émile Zola"], Search(term));
    }

    [Theory]
    [InlineData("WORLD")]
    [InlineData("world")]
    [InlineData("wOrLd")]
    public void GetItemList_NameContainsAgainstMixedCaseOriginalTitle_IgnoresAsciiCase(string term)
    {
        Assert.Equal(["Item One"], Search(term));
    }

    [Fact]
    public void GetItemList_BackslashInNameContains_IsLiteral()
    {
        Assert.Equal(["Item Three"], Search("AC\\DC"));
    }

    [Theory]
    [InlineData("St%r", "Star Wars")]
    [InlineData("Al_ha", "Item Five")]
    [InlineData("%EGA", "Item Five")]
    public void GetItemList_PercentAndUnderscoreInNameContains_StayWildcards(string term, string expected)
    {
        Assert.Equal([expected], Search(term));
    }

    [Fact]
    public void GetItemList_NameContainsOfOnlyPunctuation_MatchesOriginalTitleOnly()
    {
        Assert.Equal(["Item Six"], Search("-"));
    }

    private string[] Search(string term)
    {
        var result = _repository.GetItemList(new InternalItemsQuery(new User("test", "auth", "reset"))
        {
            IncludeItemTypes = [BaseItemKind.Movie],
            NameContains = term
        });

        return result.Select(i => i.Name).Order(StringComparer.Ordinal).ToArray();
    }

    private BaseItemEntity CreateMovie(string name, string? originalTitle)
    {
        var id = Guid.NewGuid();
        return new BaseItemEntity
        {
            Id = id,
            PresentationUniqueKey = id.ToString("N"),
            Type = _itemTypeLookup.BaseItemKindNames[BaseItemKind.Movie],
            Name = name,
            CleanName = name.GetCleanValue(),
            SortName = name.ToLowerInvariant(),
            OriginalTitle = originalTitle,
            MediaType = "Video",
            IsMovie = true,
            IsFolder = false,
            IsVirtualItem = false
        };
    }
}
