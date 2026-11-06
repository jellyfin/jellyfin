using System;
using System.IO;
using System.Threading.Tasks;
using Jellyfin.Server.Migrations.Routines;
using Jellyfin.Server.ServerSetupApp;
using MediaBrowser.Controller;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Tests.Migrations;

public sealed class RemoveEmptyLegacyTrickplayDirectoriesTests : IDisposable
{
    private readonly string _metadataDirectory;
    private readonly string _libraryDirectory;
    private readonly RemoveEmptyLegacyTrickplayDirectories _migration;

    public RemoveEmptyLegacyTrickplayDirectoriesTests()
    {
        _metadataDirectory = Directory.CreateTempSubdirectory("jellyfin-migration-test-").FullName;
        _libraryDirectory = Path.Combine(_metadataDirectory, "library");

        var serverPaths = new Mock<IServerApplicationPaths>();
        serverPaths.SetupGet(paths => paths.InternalMetadataPath).Returns(_metadataDirectory);
        _migration = new RemoveEmptyLegacyTrickplayDirectories(
            new StartupLogger<RemoveEmptyLegacyTrickplayDirectories>(NullLogger<RemoveEmptyLegacyTrickplayDirectories>.Instance),
            serverPaths.Object);
    }

    [Fact]
    public async Task PerformAsync_RemovesEmptyTrickplayDirectoryAndEmptyItemDirectory()
    {
        var itemDirectory = CreateItemDirectory("00112233445566778899aabbccddeeff");
        Directory.CreateDirectory(Path.Combine(itemDirectory, "trickplay"));

        await _migration.PerformAsync(TestContext.Current.CancellationToken);

        Assert.False(Directory.Exists(itemDirectory));
    }

    [Fact]
    public async Task PerformAsync_RemovesTrickplayDirectoryContainingOnlyEmptySubdirectories()
    {
        var itemDirectory = CreateItemDirectory("00112233445566778899aabbccddeeff");
        Directory.CreateDirectory(Path.Combine(itemDirectory, "trickplay", "320"));
        Directory.CreateDirectory(Path.Combine(itemDirectory, "trickplay", "320 - 10x10"));

        await _migration.PerformAsync(TestContext.Current.CancellationToken);

        Assert.False(Directory.Exists(itemDirectory));
    }

    [Fact]
    public async Task PerformAsync_KeepsItemDirectoryWithOtherContent()
    {
        var itemDirectory = CreateItemDirectory("00112233445566778899aabbccddeeff");
        Directory.CreateDirectory(Path.Combine(itemDirectory, "trickplay"));
        var chapterDirectory = Directory.CreateDirectory(Path.Combine(itemDirectory, "chapters")).FullName;

        await _migration.PerformAsync(TestContext.Current.CancellationToken);

        Assert.False(Directory.Exists(Path.Combine(itemDirectory, "trickplay")));
        Assert.True(Directory.Exists(chapterDirectory));
    }

    [Fact]
    public async Task PerformAsync_KeepsTrickplayDirectoryContainingFiles()
    {
        var itemDirectory = CreateItemDirectory("00112233445566778899aabbccddeeff");
        var tileDirectory = Directory.CreateDirectory(Path.Combine(itemDirectory, "trickplay", "320")).FullName;
        var tile = Path.Combine(tileDirectory, "0.jpg");
        await File.WriteAllBytesAsync(tile, [0xFF, 0xD8], TestContext.Current.CancellationToken);

        await _migration.PerformAsync(TestContext.Current.CancellationToken);

        Assert.True(File.Exists(tile));
    }

    [Fact]
    public async Task PerformAsync_SucceedsWhenLibraryDirectoryIsMissing()
    {
        await _migration.PerformAsync(TestContext.Current.CancellationToken);

        Assert.False(Directory.Exists(_libraryDirectory));
    }

    public void Dispose()
    {
        Directory.Delete(_metadataDirectory, true);
    }

    private string CreateItemDirectory(string id)
    {
        return Directory.CreateDirectory(Path.Combine(_libraryDirectory, id[..2], id)).FullName;
    }
}
