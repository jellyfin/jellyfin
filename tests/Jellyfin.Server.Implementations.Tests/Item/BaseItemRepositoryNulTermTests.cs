using System;
using System.Collections.Generic;
using System.Linq;
using Emby.Server.Implementations.Data;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Testing;
using Jellyfin.Extensions;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller.Entities;
using Xunit;
using BaseItemKind = Jellyfin.Data.Enums.BaseItemKind;
using ItemSortBy = Jellyfin.Data.Enums.ItemSortBy;
using SortOrder = Jellyfin.Database.Implementations.Enums.SortOrder;

namespace Jellyfin.Server.Implementations.Tests.Item;

/// <summary>
/// A NUL character in the text a caller filters or orders items by is ignored, so the query returns what it
/// returns without the NUL. Some engines reject a NUL in a query parameter.
/// </summary>
public sealed class BaseItemRepositoryNulTermTests : JellyfinDbTestFixture
{
    private static readonly string[] _allNames = ["ALICE", "Bob", "Rob", "Zoe", "bo", "bobby"];

    private readonly ItemTypeLookup _itemTypeLookup = new();
    private readonly BaseItemRepository _repository;

    public BaseItemRepositoryNulTermTests()
    {
        _repository = CreateBaseItemRepository(_itemTypeLookup);
    }

    [Theory]
    [InlineData("bob")]
    [InlineData("bo\0b")]
    [InlineData("bob\0")]
    [InlineData("\0bob")]
    public void GetItemList_NameStartsWithContainingNul_IgnoresTheNul(string prefix)
    {
        AddMovies("Bob", "bobby", "Rob", "ALICE", "bo", "Zoe");

        Assert.Equal(["Bob", "bobby"], Names(new InternalItemsQuery(new User("test", "auth", "reset"))
        {
            IncludeItemTypes = [BaseItemKind.Movie],
            NameStartsWith = prefix
        }));
    }

    [Theory]
    [InlineData("bob")]
    [InlineData("bo\0b")]
    [InlineData("bob\0")]
    [InlineData("\0bob")]
    public void GetItemList_NameLessThanContainingNul_IgnoresTheNul(string bound)
    {
        AddMovies("Bob", "bobby", "Rob", "ALICE", "bo", "Zoe");

        Assert.Equal(["ALICE", "bo"], Names(new InternalItemsQuery(new User("test", "auth", "reset"))
        {
            IncludeItemTypes = [BaseItemKind.Movie],
            NameLessThan = bound
        }));
    }

    [Theory]
    [InlineData("bob")]
    [InlineData("bo\0b")]
    [InlineData("bob\0")]
    [InlineData("\0bob")]
    public void GetItemList_NameStartsWithOrGreaterContainingNul_IgnoresTheNul(string bound)
    {
        AddMovies("Bob", "bobby", "Rob", "ALICE", "bo", "Zoe");

        Assert.Equal(["Bob", "Rob", "Zoe", "bobby"], Names(new InternalItemsQuery(new User("test", "auth", "reset"))
        {
            IncludeItemTypes = [BaseItemKind.Movie],
            NameStartsWithOrGreater = bound
        }));
    }

    [Fact]
    public void GetItemList_NameFiltersWithOnlyNul_AreIgnored()
    {
        AddMovies("Bob", "bobby", "Rob", "ALICE", "bo", "Zoe");

        Assert.Equal(_allNames, Names(new InternalItemsQuery(new User("test", "auth", "reset"))
        {
            IncludeItemTypes = [BaseItemKind.Movie],
            NameStartsWith = "\0"
        }));
        Assert.Equal(_allNames, Names(new InternalItemsQuery(new User("test", "auth", "reset"))
        {
            IncludeItemTypes = [BaseItemKind.Movie],
            NameLessThan = "\0"
        }));
        Assert.Equal(_allNames, Names(new InternalItemsQuery(new User("test", "auth", "reset"))
        {
            IncludeItemTypes = [BaseItemKind.Movie],
            NameStartsWithOrGreater = "\0"
        }));
    }

    [Theory]
    [InlineData("quasar")]
    [InlineData("quas\0ar")]
    [InlineData("quasar\0")]
    [InlineData("\0quasar")]
    public void GetItemList_SearchTermContainingNul_OrdersByRelevanceWithoutTheNul(string term)
    {
        // Inserted from the worst match to the best, so that a relevance order that
        // does not recognise the term leaves them in the wrong order.
        AddMovies("Quasarium", "Quasar Wave", "Quasar");

        var result = _repository.GetItemList(new InternalItemsQuery(new User("test", "auth", "reset"))
        {
            IncludeItemTypes = [BaseItemKind.Movie],
            SearchTerm = term
        });

        Assert.Equal(["Quasar", "Quasar Wave", "Quasarium"], result.Select(i => i.Name).ToArray());
    }

    private string[] Names(InternalItemsQuery query)
        => _repository.GetItemList(query).Select(i => i.Name).Order(StringComparer.Ordinal).ToArray();

    private void AddMovies(params string[] names)
    {
        using var context = CreateDbContext();
        foreach (var name in names)
        {
            var id = Guid.NewGuid();
            context.BaseItems.Add(new BaseItemEntity
            {
                Id = id,
                PresentationUniqueKey = id.ToString("N"),
                Type = _itemTypeLookup.BaseItemKindNames[BaseItemKind.Movie],
                Name = name,
                CleanName = name.GetCleanValue(),
                SortName = name.ToLowerInvariant(),
                MediaType = "Video",
                IsMovie = true,
                IsFolder = false,
                IsVirtualItem = false
            });
        }

        context.SaveChanges();
    }
}
