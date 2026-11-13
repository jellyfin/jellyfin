using System;
using System.Collections.Generic;
using System.Linq;
using Emby.Server.Implementations.Collections;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Collections;

public sealed class CollectionManagerTests
{
    private readonly User _user = new("collapse-test", "default", "default");
    private readonly List<BoxSet> _collections = new();
    private readonly CollectionManager _collectionManager;

    public CollectionManagerTests()
    {
        var collectionsFolder = new Mock<Folder>();
        collectionsFolder
            .Setup(f => f.GetChildren(_user, true, null))
            .Returns(() => _collections.Cast<BaseItem>().ToList());

        var rootFolder = new Mock<AggregateFolder>();
        rootFolder.Setup(f => f.Children).Returns(() => new BaseItem[] { collectionsFolder.Object });

        var libraryManager = new Mock<ILibraryManager>();
        libraryManager.Setup(l => l.RootFolder).Returns(rootFolder.Object);
        libraryManager
            .Setup(l => l.GetLocalAlternateVersionIds(It.IsAny<Video>()))
            .Returns(Array.Empty<Guid>());

        var fileSystem = new Mock<IFileSystem>();
        fileSystem.Setup(f => f.AreEqual(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

        var appPaths = new Mock<IApplicationPaths>();
        appPaths.Setup(p => p.DataPath).Returns("/data");

        _collectionManager = new CollectionManager(
            libraryManager.Object,
            appPaths.Object,
            Mock.Of<ILocalizationManager>(),
            fileSystem.Object,
            Mock.Of<ILibraryMonitor>(),
            NullLoggerFactory.Instance,
            Mock.Of<IProviderManager>(),
            Mock.Of<ILinkedChildrenService>());
    }

    [Fact]
    public void CollapseItemsWithinBoxSets_DirectMember_ReturnsBoxSet()
    {
        var collection = AddCollection("Collection");
        var movie = CreateMovie();
        Link(collection, movie);

        Assert.Equal([collection.Id], Collapse(movie));
    }

    [Fact]
    public void CollapseItemsWithinBoxSets_ItemInNoBoxSet_ReturnsItem()
    {
        AddCollection("Unrelated");
        var movie = CreateMovie();

        Assert.Equal([movie.Id], Collapse(movie));
    }

    [Fact]
    public void CollapseItemsWithinBoxSets_NestedBoxSet_RollsUpToRoot()
    {
        // The member sits in the nested collection, but only the outermost one may surface.
        var root = AddCollection("Root");
        var nested = AddCollection("Nested");
        var movie = CreateMovie();

        Link(nested, movie);
        Link(root, nested);

        Assert.Equal([root.Id], Collapse(movie));
    }

    [Fact]
    public void CollapseItemsWithinBoxSets_DeeplyNestedBoxSet_RollsUpToOutermostRoot()
    {
        var root = AddCollection("Root");
        var middle = AddCollection("Middle");
        var inner = AddCollection("Inner");
        var movie = CreateMovie();

        Link(inner, movie);
        Link(middle, inner);
        Link(root, middle);

        Assert.Equal([root.Id], Collapse(movie));
    }

    [Fact]
    public void CollapseItemsWithinBoxSets_ItemReachableTwiceUnderSameRoot_ReturnsRootOnce()
    {
        // Diamond: both nested collections carry the same movie.
        var root = AddCollection("Root");
        var left = AddCollection("Left");
        var right = AddCollection("Right");
        var movie = CreateMovie();

        Link(left, movie);
        Link(right, movie);
        Link(root, left, right);

        Assert.Equal([root.Id], Collapse(movie));
    }

    [Fact]
    public void CollapseItemsWithinBoxSets_ItemInSeparateRootBoxSets_ReturnsEveryRoot()
    {
        var first = AddCollection("First");
        var second = AddCollection("Second");
        var movie = CreateMovie();

        Link(first, movie);
        Link(second, movie);

        Assert.Equal([first.Id, second.Id], Collapse(movie));
    }

    [Fact]
    public void CollapseItemsWithinBoxSets_NestingCycleBelowRoot_RollsUpWithoutHanging()
    {
        var root = AddCollection("Root");
        var first = AddCollection("First");
        var second = AddCollection("Second");
        var movie = CreateMovie();

        Link(second, movie);
        Link(first, second);
        Link(second, first, movie);
        Link(root, first);

        Assert.Equal([root.Id], Collapse(movie));
    }

    [Fact]
    public void CollapseItemsWithinBoxSets_UnnestedBoxSets_KeepsEachOne()
    {
        var first = AddCollection("First");
        var second = AddCollection("Second");
        var inFirst = CreateMovie();
        var inSecond = CreateMovie();
        var loose = CreateMovie();

        Link(first, inFirst);
        Link(second, inSecond);

        Assert.Equal([first.Id, second.Id, loose.Id], Collapse(inFirst, inSecond, loose));
    }

    private static Movie CreateMovie()
        => new Movie { Id = Guid.NewGuid() };

    private static void Link(BoxSet parent, params BaseItem[] children)
        => parent.LinkedChildren = children.Select(c => new LinkedChild { ItemId = c.Id }).ToArray();

    private BoxSet AddCollection(string name)
    {
        var boxSet = new BoxSet { Id = Guid.NewGuid(), Name = name };
        _collections.Add(boxSet);
        return boxSet;
    }

    private List<Guid> Collapse(params BaseItem[] items)
        => _collectionManager.CollapseItemsWithinBoxSets(items, _user).Select(i => i.Id).ToList();
}
