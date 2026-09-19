using System;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using Moq;
using Xunit;

namespace Jellyfin.Controller.Tests.Entities;

[Collection("LibraryManagerTests")]
public class AggregateFolderTests
{
    [Fact]
    public void Children_ClearedAfterALibraryWasAdded_ListsTheNewLibrary()
    {
        var existing = new Folder { Id = Guid.NewGuid(), Path = "/libraries/movies" };
        var added = new Folder { Id = Guid.NewGuid(), Path = "/libraries/collections" };

        // What the repository holds grows once the new library has been resolved and stored.
        var stored = new List<BaseItem> { existing };

        var itemRepository = new Mock<IItemRepository>();
        itemRepository.Setup(x => x.GetItemList(It.IsAny<InternalItemsQuery>()))
            .Returns(() => stored.ToList());

        var libraryManager = new Mock<ILibraryManager>();
        libraryManager.Setup(x => x.GetItemById(It.IsAny<Guid>()))
            .Returns((Guid id) => stored.Find(i => i.Id.Equals(id)));

        BaseItem.ItemRepository = itemRepository.Object;
        BaseItem.LibraryManager = libraryManager.Object;

        var root = new AggregateFolder { Id = Guid.NewGuid(), Path = "/libraries" };

        Assert.Equal([existing.Id], root.Children.Select(i => i.Id));

        stored.Add(added);
        root.Children = null;

        // Null-forgiving: the setter takes null to mean "drop the cache", the getter reloads.
        Assert.Equal([existing.Id, added.Id], root.Children!.Select(i => i.Id));
    }

    [Fact]
    public void Children_AssignedASet_KeepsThatSet()
    {
        var itemRepository = new Mock<IItemRepository>(MockBehavior.Strict);
        BaseItem.ItemRepository = itemRepository.Object;

        var assigned = new Folder { Id = Guid.NewGuid(), Path = "/libraries/movies" };
        var root = new AggregateFolder { Id = Guid.NewGuid(), Path = "/libraries" };

        root.Children = [assigned];

        // Never goes to the repository, so the strict mock stays unused.
        Assert.Equal([assigned.Id], root.Children.Select(i => i.Id));
    }
}
