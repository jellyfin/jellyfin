using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using Moq;
using Xunit;

namespace Jellyfin.Controller.Tests.Entities;

public class BaseItemParentsTests
{
    [Fact]
    public async Task GetParents_CyclicParentChain_Terminates()
    {
        var saved = BaseItem.LibraryManager;
        try
        {
            var a = new Video { Id = Guid.NewGuid() };
            var b = new Video { Id = Guid.NewGuid() };
            a.ParentId = b.Id;
            b.ParentId = a.Id; // cycle: a -> b -> a

            BaseItem.LibraryManager = BuildFakeLibraryManager(a, b);

            // Before the cycle guard, this enumeration never ends (WaitAsync would time out).
            // With the guard it yields b, then a, then stops — count == 2.
            var count = await Task.Run(() => a.GetParents().Count()).WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            Assert.Equal(2, count);
        }
        finally
        {
            BaseItem.LibraryManager = saved;
        }
    }

    [Fact]
    public async Task GetParents_NonCyclicChain_ReturnsAllAncestors()
    {
        var saved = BaseItem.LibraryManager;
        try
        {
            var root = new Video { Id = Guid.NewGuid() };
            var grand = new Video { Id = Guid.NewGuid(), ParentId = root.Id };
            var parent = new Video { Id = Guid.NewGuid(), ParentId = grand.Id };
            var child = new Video { Id = Guid.NewGuid(), ParentId = parent.Id };

            BaseItem.LibraryManager = BuildFakeLibraryManager(root, grand, parent);

            var ancestors = await Task.Run(() => child.GetParents().ToList()).WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            // child -> parent -> grand -> root (root.ParentId empty -> stop)
            Assert.Equal(new[] { parent.Id, grand.Id, root.Id }, ancestors.Select(p => p.Id).ToArray());
        }
        finally
        {
            BaseItem.LibraryManager = saved;
        }
    }

    private static ILibraryManager BuildFakeLibraryManager(params BaseItem[] items)
    {
        var lookup = new Dictionary<Guid, BaseItem>();
        foreach (var item in items)
        {
            lookup[item.Id] = item;
        }

        var mock = new Mock<ILibraryManager>();
        mock.Setup(x => x.GetItemById(It.IsAny<Guid>()))
            .Returns((Guid id) => lookup.TryGetValue(id, out var item) ? item : null);
        return mock.Object;
    }
}
