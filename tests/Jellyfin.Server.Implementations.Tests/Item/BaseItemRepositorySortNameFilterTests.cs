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
/// The sort name filters are given a sort name as it is stored, in mixed case. <c>NameStartsWith</c> compares the
/// sort name, with ASCII letters folded, with the lower-cased term, and takes <c>%</c>, <c>_</c> and <c>\</c> in the
/// term as they are. The two range filters compare the engine's <c>lower()</c> of the sort name with the lower-cased
/// term.
/// </summary>
public sealed class BaseItemRepositorySortNameFilterTests : JellyfinDbTestFixture
{
    private readonly BaseItemRepository _repository;
    private readonly ItemTypeLookup _itemTypeLookup = new();

    public BaseItemRepositorySortNameFilterTests()
    {
        _repository = CreateBaseItemRepository(_itemTypeLookup);
    }

    [Theory]
    [InlineData("alpha", new[] { "ALPHA BETA GAMMA", "Alpha", "alpha beta" })]
    [InlineData("ALPHA", new[] { "ALPHA BETA GAMMA", "Alpha", "alpha beta" })]
    [InlineData("AlPhA", new[] { "ALPHA BETA GAMMA", "Alpha", "alpha beta" })]
    [InlineData("alPHA B", new[] { "ALPHA BETA GAMMA", "alpha beta" })]
    public void GetItemList_NameStartsWithAsciiTerm_IgnoresCaseOfSortName(string prefix, string[] expected)
    {
        AddMovies("Alpha", "alpha beta", "ALPHA BETA GAMMA", "Bravo", "xalpha");

        Assert.Equal(expected, StartingWith(prefix));
    }

    [Theory]
    [InlineData("50%", new[] { "50% off" })]
    [InlineData("50% O", new[] { "50% off" })]
    [InlineData("5_", new string[0])]
    [InlineData("5", new[] { "50% off", "500 off", "5x off" })]
    [InlineData("%", new string[0])]
    [InlineData("_", new[] { "_zed" })]
    [InlineData("A_", new string[0])]
    [InlineData("AC\\", new[] { "AC\\DC" })]
    [InlineData("ac\\dc", new[] { "AC\\DC" })]
    [InlineData("AC\\\\", new string[0])]
    [InlineData("ACD", new[] { "ACDC" })]
    public void GetItemList_NameStartsWithPercentUnderscoreOrBackslash_MatchesThemLiterally(string prefix, string[] expected)
    {
        AddMovies("50% off", "500 off", "5x off", "AC\\DC", "ACDC", "ABC", "_zed");

        Assert.Equal(expected, StartingWith(prefix));
    }

    /// <summary>
    /// The term is lower-cased as a whole, but only ASCII letters of the sort name are, so a non-ASCII term finds the
    /// lower-case sort names and no other.
    /// </summary>
    [Theory]
    [InlineData("É", new[] { "émile" })]
    [InlineData("é", new[] { "émile" })]
    [InlineData("ÉMILE", new[] { "émile" })]
    [InlineData("Émi", new[] { "émile" })]
    [InlineData("Ö", new[] { "ölfass" })]
    [InlineData("ö", new[] { "ölfass" })]
    [InlineData("Ölf", new[] { "ölfass" })]
    public void GetItemList_NameStartsWithNonAsciiTerm_FindsLowerCaseSortNamesOnly(string prefix, string[] expected)
    {
        AddMovies("Émile", "émile", "Öland", "ölfass", "Emile");

        Assert.Equal(expected, StartingWith(prefix));
    }

    [Theory]
    [InlineData("É")]
    [InlineData("é")]
    [InlineData("émile")]
    [InlineData("Á")]
    [InlineData("á")]
    public void GetItemList_NameStartsWithNonAsciiTerm_DoesNotFindUpperCaseSortNames(string prefix)
    {
        AddMovies("Émile", "Ángel");

        Assert.Empty(StartingWith(prefix));
    }

    [Fact]
    public void GetItemList_NameStartsWithKelvinSign_FindsTheAsciiLetterAndNotTheSign()
    {
        AddMovies("K", "k", "\u212A");

        Assert.Equal(["K", "k"], StartingWith("\u212A"));
    }

    [Theory]
    [InlineData("b")]
    [InlineData("B")]
    public void GetItemList_NameStartsWithOrGreaterAsciiTerm_IgnoresCaseOfSortName(string bound)
    {
        AddMovies("ALICE", "alice", "Bob", "bob", "BRAVO", "Carl", "_zed", "Zed");

        Assert.Equal(["BRAVO", "Bob", "Carl", "Zed", "bob"], Greater(bound));
    }

    [Theory]
    [InlineData("b")]
    [InlineData("B")]
    public void GetItemList_NameLessThanAsciiTerm_IgnoresCaseOfSortName(string bound)
    {
        AddMovies("ALICE", "alice", "Bob", "bob", "BRAVO", "Carl", "_zed", "Zed");

        Assert.Equal(["ALICE", "_zed", "alice"], Less(bound));
    }

    [Fact]
    public void GetItemList_NameRangeAsciiTerms_SelectTheLettersBetween()
    {
        AddMovies("ALICE", "alice", "Bob", "bob", "BRAVO", "Carl", "_zed", "Zed");

        var result = _repository.GetItemList(new InternalItemsQuery(new User("test", "auth", "reset"))
        {
            IncludeItemTypes = [BaseItemKind.Movie],
            NameStartsWithOrGreater = "B",
            NameLessThan = "D"
        });

        Assert.Equal(["BRAVO", "Bob", "Carl", "bob"], result.Select(i => i.Name).Order(StringComparer.Ordinal).ToArray());
    }

    /// <summary>
    /// The range filters leave lower-casing the sort name to the engine, and MySQL and MariaDB lower-case non-ASCII
    /// letters where SQLite and PostgreSQL leave them alone, so a non-ASCII bound selects different items there.
    /// </summary>
    [Fact]
    public void GetItemList_NameStartsWithOrGreaterNonAsciiTerm_LeavesNonAsciiLettersOfSortNameAsStored()
    {
        AddMovies("Émile", "émile", "Öland", "ölfass", "Emile");

        OnMySqlFamily(() => AssertHelpers.AssertSameNames(["émile", "ölfass"], Greater("É")));
    }

    [Fact]
    public void GetItemList_NameLessThanNonAsciiTerm_LeavesNonAsciiLettersOfSortNameAsStored()
    {
        AddMovies("Émile", "émile", "Öland", "ölfass", "Emile");

        OnMySqlFamily(() => AssertHelpers.AssertSameNames(["Emile", "Émile", "Öland"], Less("É")));
    }

    private static void OnMySqlFamily(Action body)
    {
        KnownGap.On(
            KnownGaps.SortNameRangeLowersNonAsciiOnMySql,
            () => KnownGap.On(KnownGaps.SortNameRangeLowersNonAsciiOnMariaDb, body));
    }

    private string[] StartingWith(string prefix) => Names(new InternalItemsQuery(new User("test", "auth", "reset"))
    {
        IncludeItemTypes = [BaseItemKind.Movie],
        NameStartsWith = prefix
    });

    private string[] Greater(string bound) => Names(new InternalItemsQuery(new User("test", "auth", "reset"))
    {
        IncludeItemTypes = [BaseItemKind.Movie],
        NameStartsWithOrGreater = bound
    });

    private string[] Less(string bound) => Names(new InternalItemsQuery(new User("test", "auth", "reset"))
    {
        IncludeItemTypes = [BaseItemKind.Movie],
        NameLessThan = bound
    });

    private string[] Names(InternalItemsQuery query)
        => _repository.GetItemList(query).Select(i => i.Name).Order(StringComparer.Ordinal).ToArray();

    private void AddMovies(params string[] sortNames)
    {
        using var context = CreateDbContext();
        foreach (var sortName in sortNames)
        {
            var id = Guid.NewGuid();
            context.BaseItems.Add(new BaseItemEntity
            {
                Id = id,
                PresentationUniqueKey = id.ToString("N"),
                Type = _itemTypeLookup.BaseItemKindNames[BaseItemKind.Movie],
                Name = sortName,
                SortName = sortName,
                MediaType = "Video",
                IsMovie = true,
                IsFolder = false,
                IsVirtualItem = false
            });
        }

        context.SaveChanges();
    }
}
