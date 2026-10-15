using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Database.Testing.Conformance;

/// <summary>
/// Kit row: the server deletes a folder whose items form a <c>ParentId</c> chain deeper than 15, together with
/// everything below it and the rows that point at those items, and nothing else. Every link of the chain deletes in
/// cascade, which an engine may limit in depth.
/// </summary>
public abstract class FolderDeleteConformance : ConformanceTestBase
{
    private const int Depth = 20;

    [ConformanceFact]
    public void DeleteItem_FolderAboveAChainDeeperThan15_DeletesTheChainAndKeepsTheRest()
    {
        var (chain, other) = CreateChain();

        CreateService().DeleteItem([chain[0]]);

        AssertDeleted(chain);

        AssertKept([other], 0);
    }

    [ConformanceFact]
    public void DeleteItem_FolderInTheMiddleOfTheChain_DeletesWhatIsBelowItAndKeepsItsAncestors()
    {
        var (chain, other) = CreateChain();
        const int Middle = Depth / 2;

        CreateService().DeleteItem([chain[Middle]]);

        AssertDeleted(chain.Skip(Middle).ToList());

        // An item at depth d has a row for each of its d ancestors.
        AssertKept([.. chain.Take(Middle), other], Middle * (Middle - 1) / 2);
    }

    private (IReadOnlyList<Guid> Chain, Guid Other) CreateChain()
    {
        var items = Table<BaseItemEntity>();
        var ancestors = Table<AncestorId>();
        var userData = Table<UserData>();

        // Premise: a server that limits the depth of a cascade deletes the rows of one statement in the order of their key.
        // The cascade of an item stops at the first item below it that is already gone, so with random ids the cascade
        // ran the full depth of the chain only when the root happened to be deleted first. The ids rise from the root
        // down instead: they share a random prefix and differ only in their last byte, which is last in the text of an
        // id and in its bytes in either byte order.
        var idBytes = Guid.NewGuid().ToByteArray();
        var chain = new List<Guid>();
        for (var depth = 0; depth <= Depth; depth++)
        {
            idBytes[^1] = (byte)depth;
            var id = new Guid(idBytes);
            Rows.Save(items, new Dictionary<string, object?>
            {
                [nameof(BaseItemEntity.Id)] = id,
                [nameof(BaseItemEntity.ParentId)] = depth == 0 ? null : chain[^1],
                [nameof(BaseItemEntity.IsFolder)] = true
            });
            foreach (var ancestor in chain)
            {
                Rows.Save(ancestors, new Dictionary<string, object?> { [nameof(AncestorId.ItemId)] = id, [nameof(AncestorId.ParentItemId)] = ancestor });
            }

            Rows.Save(userData, new Dictionary<string, object?> { [nameof(UserData.ItemId)] = id });
            chain.Add(id);
        }

        // The server orders the keys the same way, so that its key order starts at the root on every engine.
        using (var context = CreateDbContext())
        {
            Assert.Equal(chain, context.BaseItems.Where(e => chain.Contains(e.Id)).OrderBy(e => e.Id).Select(e => e.Id).ToList());
        }

        var other = Guid.NewGuid();
        Rows.Save(items, new Dictionary<string, object?> { [nameof(BaseItemEntity.Id)] = other, [nameof(BaseItemEntity.ParentId)] = null });
        Rows.Save(userData, new Dictionary<string, object?> { [nameof(UserData.ItemId)] = other });
        return (chain, other);
    }

    private void AssertDeleted(IReadOnlyList<Guid> deleted)
    {
        using var context = CreateDbContext();
        Assert.Equal(0, context.BaseItems.Count(e => deleted.Contains(e.Id)));
        Assert.Equal(0, context.AncestorIds.Count(e => deleted.Contains(e.ItemId) || deleted.Contains(e.ParentItemId)));

        // The user data of a deleted item is kept, moved to the placeholder item, so that it comes back with the item.
        Assert.Equal(0, context.UserData.Count(e => deleted.Contains(e.ItemId)));
        Assert.Equal(deleted.Count, context.UserData.Count(e => e.ItemId.Equals(BaseItemRepository.PlaceholderId)));
    }

    private void AssertKept(IReadOnlyList<Guid> kept, int ancestorRows)
    {
        using var context = CreateDbContext();
        Assert.Equal(kept.Count, context.BaseItems.Count(e => kept.Contains(e.Id)));
        Assert.Equal(kept.Count, context.UserData.Count(e => kept.Contains(e.ItemId)));
        Assert.Equal(ancestorRows, context.AncestorIds.Count(e => kept.Contains(e.ItemId)));
    }

    private ItemPersistenceService CreateService()
        => new(CreateDbContextFactory(), DatabaseProvider, Mock.Of<IServerApplicationHost>(), NullLogger<ItemPersistenceService>.Instance);
}
