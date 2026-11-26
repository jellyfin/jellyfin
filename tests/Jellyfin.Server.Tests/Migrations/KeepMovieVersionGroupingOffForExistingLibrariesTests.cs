using System;
using System.Threading.Tasks;
using Jellyfin.Server.Migrations.Routines;
using Jellyfin.Server.ServerSetupApp;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Tests.Migrations;

public sealed class KeepMovieVersionGroupingOffForExistingLibrariesTests : IDisposable
{
    private readonly IXmlSerializer _previousSerializer = CollectionFolder.XmlSerializer;
    private readonly Mock<IXmlSerializer> _serializer = new();
    private readonly Mock<ILibraryManager> _libraryManager = new();

    public KeepMovieVersionGroupingOffForExistingLibrariesTests()
    {
        CollectionFolder.XmlSerializer = _serializer.Object;
    }

    public void Dispose()
    {
        CollectionFolder.XmlSerializer = _previousSerializer;
    }

    [Fact]
    public async Task PerformAsync_ExistingLibraryWithTheDefault_TurnsGroupingOff()
    {
        var enabled = AddLibrary("Movies", new LibraryOptions());
        var alreadyOff = AddLibrary("Movies 4K", new LibraryOptions { EnableAutomaticMovieVersionGrouping = false });
        _libraryManager.Setup(x => x.GetVirtualFolders(false)).Returns([enabled, alreadyOff]);

        await CreateMigration().PerformAsync(TestContext.Current.CancellationToken);

        Assert.False(enabled.LibraryOptions.EnableAutomaticMovieVersionGrouping);
        _serializer.Verify(
            x => x.SerializeToFile(It.Is<object>(o => o is LibraryOptions && !((LibraryOptions)o).EnableAutomaticMovieVersionGrouping), It.IsAny<string>()),
            Times.Once);
    }

    private VirtualFolderInfo AddLibrary(string name, LibraryOptions options)
    {
        var folder = new CollectionFolder { Id = Guid.NewGuid(), Name = name, Path = "/config/root/default/" + name };
        _libraryManager.Setup(x => x.GetItemById<CollectionFolder>(folder.Id)).Returns(folder);
        return new VirtualFolderInfo
        {
            Name = name,
            ItemId = folder.Id.ToString("N"),
            LibraryOptions = options
        };
    }

    private KeepMovieVersionGroupingOffForExistingLibraries CreateMigration()
        => new(
            _libraryManager.Object,
            new StartupLogger<KeepMovieVersionGroupingOffForExistingLibraries>(NullLogger<KeepMovieVersionGroupingOffForExistingLibraries>.Instance),
            NullLogger<KeepMovieVersionGroupingOffForExistingLibraries>.Instance);
}
