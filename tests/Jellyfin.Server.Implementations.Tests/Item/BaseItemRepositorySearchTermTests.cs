using System;
using System.Linq;
using Emby.Server.Implementations.Data;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Testing;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller.Entities;
using Xunit;
using BaseItemKind = Jellyfin.Data.Enums.BaseItemKind;

namespace Jellyfin.Server.Implementations.Tests.Item;

/// <summary>
/// The search term filter matches <c>CleanOriginalTitle</c>, the form of <c>OriginalTitle</c> without case,
/// diacritics and punctuation, so the match ignores them in the term and in the title. <c>%</c> and <c>_</c>
/// stay wildcards.
/// </summary>
public sealed class BaseItemRepositorySearchTermTests : JellyfinDbTestFixture
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
        context.BaseItems.Add(CreateMovie("Item Seven", "élan Vital"));
        context.BaseItems.Add(CreateMovie("Item Eight", "Ångström"));
        context.BaseItems.Add(CreateMovie("Item Nine", null));
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

    [Theory]
    [InlineData("ÉMILE")]
    [InlineData("émile")]
    [InlineData("Émile")]
    [InlineData("emile")]
    [InlineData("EMILE")]
    public void GetItemList_SearchTermWithOrWithoutAccentsInAnyCase_FindsAccentedOriginalTitle(string term)
    {
        Assert.Equal(["Émile Zola"], Search(term));
    }

    [Theory]
    [InlineData("ÉLAN")]
    [InlineData("élan")]
    [InlineData("elan")]
    [InlineData("ELAN")]
    public void GetItemList_SearchTermInAnyCase_FindsLowerCaseAccentedOriginalTitle(string term)
    {
        Assert.Equal(["élan Vital"], Search(term));
    }

    [Theory]
    [InlineData("ångström")]
    [InlineData("ÅNGSTRÖM")]
    [InlineData("angstrom")]
    public void GetItemList_SearchTermAgainstUpperCaseAccentedOriginalTitle_IgnoresCaseAndDiacritics(string term)
    {
        Assert.Equal(["Ångström"], Search(term));
    }

    [Theory]
    [InlineData("AC\\DC")]
    [InlineData("AC DC")]
    [InlineData("ac-dc")]
    public void GetItemList_PunctuationInSearchTerm_IsIgnoredLikeInTheTitle(string term)
    {
        Assert.Equal(["AC\\DC"], Search(term));
    }

    [Theory]
    [InlineData("Al%ga", "Alpha Omega")]
    [InlineData("Al_ha", "Alpha Omega")]
    [InlineData("%eta", "Beta")]
    public void GetItemList_PercentAndUnderscoreInSearchTerm_StayWildcards(string term, string expected)
    {
        Assert.Equal([expected], Search(term));
    }

    [Fact]
    public void GetItemList_SearchTermWithoutBackslashInTheTerm_DoesNotFindTitleWithBackslash()
    {
        Assert.Equal(["ACDC"], Search("ACDC"));
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData("-")]
    [InlineData("...")]
    [InlineData("\\")]
    [InlineData("[]^")]
    public void GetItemList_SearchTermWithoutLetterDigitOrWildcard_MatchesNothing(string term)
    {
        Assert.Empty(Search(term));
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

    private BaseItemEntity CreateMovie(string name, string? originalTitle)
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
            CleanOriginalTitle = MatchNormalizer.CleanOriginalTitle(originalTitle),
            MediaType = "Video",
            IsMovie = true,
            IsFolder = false,
            IsVirtualItem = false
        };
    }
}
