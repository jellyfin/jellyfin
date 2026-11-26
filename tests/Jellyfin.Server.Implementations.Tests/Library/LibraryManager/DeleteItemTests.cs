using System;
using System.Collections.Generic;
using System.IO;
using AutoFixture;
using AutoFixture.AutoMoq;
using Emby.Naming.Common;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Controller.Sorting;
using MediaBrowser.Model.IO;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Library.LibraryManager;

public sealed class DeleteItemTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly string _metadataPath;
    private readonly string _peoplePath;
    private readonly Emby.Server.Implementations.Library.LibraryManager _libraryManager;
    private readonly Mock<IItemPersistenceService> _persistenceServiceMock;
    private readonly IFileSystem? _previousFileSystem;
    private readonly IMediaSourceManager? _previousMediaSourceManager;
    private readonly IServerConfigurationManager? _previousConfigurationManager;

    public DeleteItemTests()
    {
        _previousFileSystem = BaseItem.FileSystem;
        _previousMediaSourceManager = BaseItem.MediaSourceManager;
        _previousConfigurationManager = BaseItem.ConfigurationManager;

        _tempRoot = Path.Combine(Path.GetTempPath(), "jellyfin-delete-tests-" + Guid.NewGuid().ToString("N"));
        _metadataPath = Path.Combine(_tempRoot, "metadata");
        _peoplePath = Path.Combine(_metadataPath, "People");
        Directory.CreateDirectory(_peoplePath);

        var fixture = new Fixture().Customize(new AutoMoqCustomization());
        fixture.Register(() => new NamingOptions());

        var configMock = fixture.Freeze<Mock<IServerConfigurationManager>>();
        configMock.Setup(c => c.ApplicationPaths.ProgramDataPath).Returns(_tempRoot);
        configMock.Setup(c => c.ApplicationPaths.DataPath).Returns(Path.Combine(_tempRoot, "data"));
        configMock.Setup(c => c.ApplicationPaths.CachePath).Returns(Path.Combine(_tempRoot, "cache"));
        configMock.Setup(c => c.ApplicationPaths.ConfigurationDirectoryPath).Returns(Path.Combine(_tempRoot, "config"));
        configMock.Setup(c => c.ApplicationPaths.RootFolderPath).Returns(Path.Combine(_tempRoot, "root"));
        configMock.Setup(c => c.ApplicationPaths.DefaultUserViewsPath).Returns(Path.Combine(_tempRoot, "root", "default"));
        configMock.Setup(c => c.ApplicationPaths.InternalMetadataPath).Returns(_metadataPath);
        configMock.Setup(c => c.ApplicationPaths.DefaultInternalMetadataPath).Returns(_metadataPath);
        configMock.Setup(c => c.ApplicationPaths.PeoplePath).Returns(_peoplePath);
        configMock.Setup(c => c.ApplicationPaths.ArtistsPath).Returns(Path.Combine(_metadataPath, "artists"));
        configMock.Setup(c => c.ApplicationPaths.GenrePath).Returns(Path.Combine(_metadataPath, "Genre"));
        configMock.Setup(c => c.ApplicationPaths.MusicGenrePath).Returns(Path.Combine(_metadataPath, "MusicGenre"));
        configMock.Setup(c => c.ApplicationPaths.StudioPath).Returns(Path.Combine(_metadataPath, "Studio"));
        configMock.Setup(c => c.ApplicationPaths.YearPath).Returns(Path.Combine(_metadataPath, "Year"));

        var itemRepositoryMock = fixture.Freeze<Mock<IItemRepository>>();
        itemRepositoryMock.Setup(i => i.RetrieveItem(It.IsAny<Guid>())).Returns<BaseItem>(null!);
        _persistenceServiceMock = fixture.Freeze<Mock<IItemPersistenceService>>();

        var fileSystemMock = fixture.Freeze<Mock<IFileSystem>>();
        fileSystemMock.Setup(f => f.GetFileSystemInfo(It.IsAny<string>()))
            .Returns<string>(path => new FileSystemMetadata
            {
                FullName = path,
                IsDirectory = Directory.Exists(path)
            });
        fileSystemMock.Setup(f => f.GetFileInfo(It.IsAny<string>()))
            .Returns<string>(path => new FileSystemMetadata { FullName = path });
        fileSystemMock.Setup(f => f.GetValidFilename(It.IsAny<string>()))
            .Returns<string>(name => name);

        _libraryManager = fixture.Build<Emby.Server.Implementations.Library.LibraryManager>()
            .Do(s => s.AddParts(
                fixture.Create<IEnumerable<IResolverIgnoreRule>>(),
                fixture.Create<IEnumerable<IItemResolver>>(),
                fixture.Create<IEnumerable<IIntroProvider>>(),
                fixture.Create<IEnumerable<IBaseItemComparer>>(),
                fixture.Create<IEnumerable<ILibraryPostScanTask>>()))
            .Create();

        BaseItem.FileSystem = fileSystemMock.Object;
        BaseItem.MediaSourceManager = fixture.Create<IMediaSourceManager>();
        BaseItem.ConfigurationManager = configMock.Object;
    }

    public void Dispose()
    {
        BaseItem.FileSystem = _previousFileSystem;
        BaseItem.MediaSourceManager = _previousMediaSourceManager;
        BaseItem.ConfigurationManager = _previousConfigurationManager;

        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, true);
        }
    }

    [Fact]
    public void DeleteItemsUnsafeFast_ProtectedPeopleRoot_PreservesPeopleDirectory()
    {
        var aliceFolder = Path.Combine(_peoplePath, "A", "Alice");
        var aliceImage = Path.Combine(aliceFolder, "folder.jpg");
        Directory.CreateDirectory(aliceFolder);
        File.WriteAllText(aliceImage, "image");

        var corruptPerson = new Person
        {
            Id = Guid.NewGuid(),
            Name = "??????",
            Path = _peoplePath
        };

        _libraryManager.DeleteItemsUnsafeFast([corruptPerson], deleteSourceFiles: true);

        Assert.True(Directory.Exists(_peoplePath));
        Assert.True(File.Exists(aliceImage));
        _persistenceServiceMock.Verify(
            r => r.DeleteItem(It.Is<IReadOnlyList<Guid>>(ids => ids.Count == 1 && ids[0].Equals(corruptPerson.Id))),
            Times.Once);
    }

    [Fact]
    public void DeleteItemsUnsafeFast_AncestorOfProtectedRoot_PreservesMetadataDirectory()
    {
        var aliceFolder = Path.Combine(_peoplePath, "A", "Alice");
        var aliceImage = Path.Combine(aliceFolder, "folder.jpg");
        Directory.CreateDirectory(aliceFolder);
        File.WriteAllText(aliceImage, "image");

        var corruptPerson = new Person
        {
            Id = Guid.NewGuid(),
            Name = "corrupt-ancestor",
            Path = _metadataPath
        };

        _libraryManager.DeleteItemsUnsafeFast([corruptPerson], deleteSourceFiles: true);

        Assert.True(Directory.Exists(_metadataPath));
        Assert.True(File.Exists(aliceImage));
    }

    [Fact]
    public void DeleteItem_PerPersonSubfolder_DeletesOnlyItemFolder()
    {
        var aliceFolder = Path.Combine(_peoplePath, "A", "Alice");
        var bobFolder = Path.Combine(_peoplePath, "B", "Bob");
        Directory.CreateDirectory(aliceFolder);
        Directory.CreateDirectory(bobFolder);
        File.WriteAllText(Path.Combine(aliceFolder, "folder.jpg"), "alice");
        File.WriteAllText(Path.Combine(bobFolder, "folder.jpg"), "bob");

        var alice = new Person
        {
            Id = Guid.NewGuid(),
            Name = "Alice",
            Path = aliceFolder
        };

        _libraryManager.DeleteItem(alice, new DeleteOptions { DeleteFileLocation = true }, false);

        Assert.False(Directory.Exists(aliceFolder));
        Assert.True(Directory.Exists(_peoplePath));
        Assert.True(Directory.Exists(bobFolder));
    }
}
