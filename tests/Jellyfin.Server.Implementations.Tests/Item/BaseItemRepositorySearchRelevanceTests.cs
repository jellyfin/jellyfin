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
/// Items found through <c>CleanOriginalTitle</c> are ordered by how well the title matches the search term: the
/// whole title, then the start of the title followed by a word boundary, then the start of the title, then anywhere.
/// </summary>
public sealed class BaseItemRepositorySearchRelevanceTests : JellyfinDbTestFixture
{
    private readonly BaseItemRepository _repository;
    private readonly ItemTypeLookup _itemTypeLookup = new();

    public BaseItemRepositorySearchRelevanceTests()
    {
        _repository = CreateBaseItemRepository(_itemTypeLookup);

        using var context = CreateDbContext();
        context.BaseItems.Add(CreateMovie("Item One", "Un Émile"));
        context.BaseItems.Add(CreateMovie("Item Two", "Émilea"));
        context.BaseItems.Add(CreateMovie("Item Three", "Émile Zola"));
        context.BaseItems.Add(CreateMovie("Item Four", "Émile"));
        context.SaveChanges();
    }

    [Theory]
    [InlineData("émile")]
    [InlineData("ÉMILE")]
    [InlineData("Emile")]
    [InlineData("  émile ")]
    public void GetItemList_SearchTermMatchingOriginalTitles_OrdersThemByRelevance(string term)
    {
        var result = _repository.GetItemList(new InternalItemsQuery(new User("test", "auth", "reset"))
        {
            IncludeItemTypes = [BaseItemKind.Movie],
            SearchTerm = term
        });

        Assert.Equal(["Item Four", "Item Three", "Item Two", "Item One"], result.Select(i => i.Name).ToArray());
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
