using System;
using System.Linq;
using Emby.Server.Implementations.Data;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Testing;
using Jellyfin.Extensions;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller.Entities;
using Xunit;
using BaseItemKind = Jellyfin.Data.Enums.BaseItemKind;

namespace Jellyfin.Server.Implementations.Tests.Item;

/// <summary>
/// The name-contains filter looks in the normalized <c>CleanName</c> and in <c>CleanOriginalTitle</c>, the form of
/// <c>OriginalTitle</c> without case, diacritics and punctuation.
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
            context.BaseItems.Add(CreateMovie("Item Seven", "Ångelholm Études"));
            context.BaseItems.Add(CreateMovie("Item Eight", "éclair Noir"));
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

    [Theory]
    [InlineData("ångelholm")]
    [InlineData("ÅNGELHOLM")]
    [InlineData("Ångelholm")]
    [InlineData("angelholm")]
    [InlineData("études")]
    [InlineData("ETUDES")]
    public void GetItemList_NameContainsAgainstUpperCaseAccentedOriginalTitle_IgnoresCaseAndDiacritics(string term)
    {
        Assert.Equal(["Item Seven"], Search(term));
    }

    [Theory]
    [InlineData("éclair")]
    [InlineData("ÉCLAIR")]
    [InlineData("Éclair")]
    [InlineData("eclair")]
    [InlineData("ECLAIR")]
    public void GetItemList_NameContainsAgainstLowerCaseAccentedOriginalTitle_IgnoresCaseAndDiacritics(string term)
    {
        Assert.Equal(["Item Eight"], Search(term));
    }

    [Theory]
    [InlineData("AC\\DC")]
    [InlineData("AC DC")]
    [InlineData("ac-dc")]
    public void GetItemList_PunctuationInNameContains_IsIgnoredLikeInTheTitle(string term)
    {
        Assert.Equal(["Item Three"], Search(term));
    }

    [Theory]
    [InlineData("Spider-Man")]
    [InlineData("spider man")]
    [InlineData("er-ma")]
    public void GetItemList_NameContainsAgainstOriginalTitleWithPunctuation_MatchesWithoutIt(string term)
    {
        Assert.Equal(["Item Six"], Search(term));
    }

    [Theory]
    [InlineData("St%r", "Star Wars")]
    [InlineData("Al_ha", "Item Five")]
    [InlineData("%EGA", "Item Five")]
    [InlineData("%CLAI", "Item Eight")]
    [InlineData("Ecl_ir", "Item Eight")]
    public void GetItemList_PercentAndUnderscoreInNameContains_StayWildcards(string term, string expected)
    {
        Assert.Equal([expected], Search(term));
    }

    [Theory]
    [InlineData("-")]
    [InlineData("...")]
    [InlineData("\\")]
    [InlineData("[]^")]
    public void GetItemList_NameContainsOfOnlyPunctuation_MatchesNothing(string term)
    {
        Assert.Empty(Search(term));
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
            CleanOriginalTitle = MatchNormalizer.CleanOriginalTitle(originalTitle),
            MediaType = "Video",
            IsMovie = true,
            IsFolder = false,
            IsVirtualItem = false
        };
    }
}
