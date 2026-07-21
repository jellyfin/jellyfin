using System;
using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using AutoFixture.AutoMoq;
using Emby.Naming.Common;
using Emby.Server.Implementations.Library;
using Emby.Server.Implementations.Sorting;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Enums;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Controller.Sorting;
using MediaBrowser.Model.IO;
using Moq;
using Xunit;
using BaseItem = MediaBrowser.Controller.Entities.BaseItem;

namespace Jellyfin.Server.Implementations.Tests.Library;

public class LibraryManagerSortTests
{
    [Fact]
    public void Sort_UserDependentKey_NullUser_ThrowsArgumentException()
    {
        var libraryManager = CreateLibraryManager(
            new IBaseItemComparer[] { new PlayCountComparer(), new SortNameComparer() });

        BaseItem[] items =
        {
            new Audio { Name = "Zulu", SortName = "Zulu", Id = Guid.NewGuid() },
            new Audio { Name = "Alpha", SortName = "Alpha", Id = Guid.NewGuid() },
        };

        // A user-dependent sort key with no user is a caller contract violation — throw,
        // don't silently fall back to a different key (review feedback on #17395).
        Assert.Throws<ArgumentException>(() => libraryManager.Sort(
            items,
            user: null,
            new[] { (ItemSortBy.PlayCount, SortOrder.Descending) }).ToArray());
    }

    [Fact]
    public void Sort_DateLastContentAdded_NullUser_OrdersByDateNotSortName()
    {
        // DateLastMediaAddedComparer does not use User (its GetDate is static), so it must NOT be
        // treated as a user-dependent comparer: with no user it should still sort by date, not fall
        // back to SortName.
        var libraryManager = CreateLibraryManager(
            new IBaseItemComparer[] { new DateLastMediaAddedComparer(), new SortNameComparer() });

        // Names are chosen so date-descending and SortName-descending DISAGREE: Alpha is newest
        // (date-desc rank 1), but Zulu sorts last alphabetically (SortName-desc rank 1). If the
        // comparer were still tagged IUserBaseItemComparer, the null-user SortName fallback would
        // return [Zulu, Mike, Alpha] and this assertion would fail.
        BaseItem[] items =
        {
            MakeFolder("Alpha", new DateTime(2026, 1, 1)),
            MakeFolder("Mike", new DateTime(2025, 1, 1)),
            MakeFolder("Zulu", new DateTime(2024, 1, 1))
        };

        var sorted = libraryManager.Sort(
            items,
            user: null,
            new[] { (ItemSortBy.DateLastContentAdded, SortOrder.Descending) }).ToArray();

        // Descending by date => newest first: Alpha, Mike, Zulu. (SortName-desc would be Zulu, Mike, Alpha.)
        Assert.Equal(new[] { "Alpha", "Mike", "Zulu" }, sorted.Select(i => i.Name));
    }

    private static Folder MakeFolder(string name, DateTime dateLastMediaAdded)
        => new() { Name = name, Id = Guid.NewGuid(), DateLastMediaAdded = dateLastMediaAdded };

    private static Emby.Server.Implementations.Library.LibraryManager CreateLibraryManager(IReadOnlyCollection<IBaseItemComparer> comparers)
    {
        var fixture = new Fixture().Customize(new AutoMoqCustomization());
        fixture.Register(() => new NamingOptions());
        var configMock = fixture.Freeze<Mock<IServerConfigurationManager>>();
        configMock.Setup(c => c.ApplicationPaths.ProgramDataPath).Returns("/data");
        // BaseItem.SortName/CreateSortName dereference this static; set it so SortName-fallback
        // paths don't NRE in-process (mirrors AudioResolverTests in the sibling test project).
        BaseItem.ConfigurationManager ??= configMock.Object;
        var itemRepository = fixture.Freeze<Mock<IItemRepository>>();
        itemRepository.Setup(i => i.RetrieveItem(It.IsAny<Guid>())).Returns<BaseItem>(null);
        var fileSystemMock = fixture.Freeze<Mock<IFileSystem>>();
        fileSystemMock.Setup(f => f.GetFileInfo(It.IsAny<string>())).Returns<string>(path => new FileSystemMetadata { FullName = path });

        return fixture.Build<Emby.Server.Implementations.Library.LibraryManager>().Do(s => s.AddParts(
                fixture.Create<IEnumerable<IResolverIgnoreRule>>(),
                fixture.Create<IEnumerable<IItemResolver>>(),
                fixture.Create<IEnumerable<IIntroProvider>>(),
                comparers,
                fixture.Create<IEnumerable<ILibraryPostScanTask>>()))
            .Create();
    }
}
