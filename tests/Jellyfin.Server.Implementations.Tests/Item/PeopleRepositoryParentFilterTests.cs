using System;
using System.Linq;
using Emby.Server.Implementations.Data;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Persistence;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Item;

/// <summary>
/// Covers <see cref="InternalPeopleQuery.ParentId"/>, which limits /Persons to the people credited below a folder.
/// </summary>
public sealed class PeopleRepositoryParentFilterTests : SqliteDbTestFixture
{
    private readonly PeopleRepository _people;

    private readonly Guid _library = Guid.NewGuid();
    private readonly Guid _series = Guid.NewGuid();
    private readonly Guid _episode = Guid.NewGuid();
    private readonly Guid _secondEpisode = Guid.NewGuid();
    private readonly Guid _otherSeries = Guid.NewGuid();
    private readonly Guid _otherEpisode = Guid.NewGuid();
    private readonly Guid _emptyFolder = Guid.NewGuid();

    public PeopleRepositoryParentFilterTests()
    {
        var lookup = new ItemTypeLookup();
        using (var context = CreateDbContext())
        {
            var folder = lookup.BaseItemKindNames[BaseItemKind.Folder];
            var series = lookup.BaseItemKindNames[BaseItemKind.Series];
            var episode = lookup.BaseItemKindNames[BaseItemKind.Episode];
            context.BaseItems.Add(new BaseItemEntity { Id = _library, Type = folder, Name = "Shows", IsFolder = true });
            context.BaseItems.Add(new BaseItemEntity { Id = _series, Type = series, Name = "Series", IsFolder = true });
            context.BaseItems.Add(new BaseItemEntity { Id = _episode, Type = episode, Name = "Episode 1" });
            context.BaseItems.Add(new BaseItemEntity { Id = _secondEpisode, Type = episode, Name = "Episode 2" });
            context.BaseItems.Add(new BaseItemEntity { Id = _otherSeries, Type = series, Name = "Other series", IsFolder = true });
            context.BaseItems.Add(new BaseItemEntity { Id = _otherEpisode, Type = episode, Name = "Other episode" });
            context.BaseItems.Add(new BaseItemEntity { Id = _emptyFolder, Type = folder, Name = "Empty", IsFolder = true });

            AddAncestors(context, _series, _library);
            AddAncestors(context, _episode, _series, _library);
            AddAncestors(context, _secondEpisode, _series, _library);
            AddAncestors(context, _otherSeries, _library);
            AddAncestors(context, _otherEpisode, _otherSeries, _library);
            AddAncestors(context, _emptyFolder, _library);
            context.SaveChanges();
        }

        _people = new PeopleRepository(CreateDbContextFactory(), lookup, Mock.Of<IItemQueryHelpers>());
        _people.UpdatePeople(_episode, [Actor("Lead"), Actor("Guest")]);
        _people.UpdatePeople(_secondEpisode, [Actor("Lead")]);
        _people.UpdatePeople(_otherEpisode, [Actor("Stranger")]);
    }

    [Fact]
    public void ParentId_ReturnsEachPersonCreditedBelowTheParentOnce()
    {
        var result = _people.GetPeople(new InternalPeopleQuery([], []) { ParentId = _series });

        Assert.Equal(["Guest", "Lead"], result.Items.Select(p => p.Name).Distinct().Order());
        Assert.Equal(2, result.TotalRecordCount);
    }

    [Fact]
    public void ParentId_SpanningSeveralFolders_ReturnsTheirUnion()
    {
        var result = _people.GetPeople(new InternalPeopleQuery([], []) { ParentId = _library });

        Assert.Equal(["Guest", "Lead", "Stranger"], result.Items.Select(p => p.Name).Distinct().Order());
        Assert.Equal(3, result.TotalRecordCount);
    }

    [Fact]
    public void ParentId_WithoutCreditsBelowIt_ReturnsNobody()
    {
        var result = _people.GetPeople(new InternalPeopleQuery([], []) { ParentId = _emptyFolder });

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalRecordCount);
    }

    private static PersonInfo Actor(string name) => new() { Name = name, Type = PersonKind.Actor };

    private static void AddAncestors(JellyfinDbContext context, Guid itemId, params Guid[] ancestorIds)
    {
        foreach (var ancestorId in ancestorIds)
        {
            context.AncestorIds.Add(new AncestorId
            {
                ItemId = itemId,
                ParentItemId = ancestorId,
                Item = null!,
                ParentItem = null!
            });
        }
    }
}
