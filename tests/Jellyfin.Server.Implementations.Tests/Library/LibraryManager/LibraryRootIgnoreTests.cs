using System;
using System.IO;
using AutoFixture;
using AutoFixture.AutoMoq;
using Emby.Naming.Common;
using Emby.Server.Implementations.IO;
using Emby.Server.Implementations.Library;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Sorting;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Library.LibraryManager;

public sealed class LibraryRootIgnoreTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _userViewsPath;
    private readonly string _libraryRoot;
    private readonly FileSystemMetadata _movieFile;

    public LibraryRootIgnoreTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _userViewsPath = Path.Combine(_tempDir, "root", "default");
        _libraryRoot = Path.Combine(_tempDir, "media", "Movies");
        var movieDir = Path.Combine(_libraryRoot, "Movie (2020)");
        Directory.CreateDirectory(movieDir);

        // Ignores everything below it, but sits above the library root
        File.WriteAllText(Path.Combine(_tempDir, ".ignore"), string.Empty);

        _movieFile = new FileSystemMetadata
        {
            FullName = Path.Combine(movieDir, "Movie (2020).mkv"),
            IsDirectory = false
        };
    }

    public void Dispose()
    {
        Directory.Delete(_tempDir, true);
    }

    [Fact]
    public void IgnoreFile_IgnoreFileAboveMblinkLibraryRoot_IsNotConsulted()
    {
        var libraryDir = Path.Combine(_userViewsPath, "Movies");
        Directory.CreateDirectory(libraryDir);
        File.WriteAllText(Path.Combine(libraryDir, "Movies.mblink"), _libraryRoot);

        Assert.False(CreateLibraryManager().IgnoreFile(_movieFile, null));
    }

    [Fact]
    public void IgnoreFile_UserViewsPathMissing_WalksToFilesystemRoot()
    {
        // Before root/default exists there are no library roots, so the .ignore above still applies
        Assert.False(Directory.Exists(_userViewsPath));
        Assert.True(CreateLibraryManager().IgnoreFile(_movieFile, null));
    }

    private Emby.Server.Implementations.Library.LibraryManager CreateLibraryManager()
    {
        var fixture = new Fixture().Customize(new AutoMoqCustomization());
        fixture.Register(() => new NamingOptions());
        var configMock = fixture.Freeze<Mock<IServerConfigurationManager>>();
        configMock.Setup(c => c.ApplicationPaths.ProgramDataPath).Returns(_tempDir);
        configMock.Setup(c => c.ApplicationPaths.DefaultUserViewsPath).Returns(_userViewsPath);
        fixture.Freeze<Mock<IServerApplicationHost>>()
            .Setup(h => h.ExpandVirtualPath(It.IsAny<string>()))
            .Returns<string>(path => path);
        fixture.Inject<IFileSystem>(new ManagedFileSystem(
            NullLogger<ManagedFileSystem>.Instance,
            configMock.Object.ApplicationPaths,
            [new MbLinkShortcutHandler()]));

        // The same instance LibraryManager hands the roots to must be the one evaluating the rules
        var dotIgnoreRule = new DotIgnoreIgnoreRule();
        fixture.Inject(dotIgnoreRule);

        return fixture.Build<Emby.Server.Implementations.Library.LibraryManager>().Do(s => s.AddParts(
                [dotIgnoreRule],
                [],
                [],
                Array.Empty<IBaseItemComparer>(),
                []))
            .Create();
    }
}
