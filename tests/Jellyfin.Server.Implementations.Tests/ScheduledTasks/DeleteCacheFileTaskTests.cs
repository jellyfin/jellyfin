using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Emby.Server.Implementations.ScheduledTasks.Tasks;
using MediaBrowser.Controller;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.ScheduledTasks;

public class DeleteCacheFileTaskTests
{
    private static readonly DateTime _staleLastWriteTimeUtc = DateTime.UtcNow.AddDays(-31);

    [Fact]
    public void ExecuteAsync_MetadataOutsideCache_DeletesStaleCacheFilesWithoutWarning()
    {
        var cachePath = GetTestPath("cache");
        var tempPath = GetTestPath("temp");
        var metadataPath = GetTestPath("metadata");
        var cacheFile = CreateFile(Path.Combine(cachePath, "cache.dat"));
        var fileSystem = CreateFileSystem(cachePath, tempPath, [cacheFile]);
        var logger = new Mock<ILogger<DeleteCacheFileTask>>();
        var task = CreateTask(cachePath, tempPath, metadataPath, fileSystem.Object, logger.Object);

        task.ExecuteAsync(Mock.Of<IProgress<double>>(), CancellationToken.None);

        fileSystem.Verify(f => f.DeleteFile(cacheFile.FullName), Times.Once);
        VerifyWarning(logger, Times.Never());
    }

    [Fact]
    public void ExecuteAsync_MetadataInsideCache_ExcludesMetadataButDeletesOtherCacheAndTempFiles()
    {
        var cachePath = GetTestPath("cache");
        var tempPath = GetTestPath("temp");
        var metadataPath = Path.Combine(cachePath, "metadata");
        var cacheFile = CreateFile(Path.Combine(cachePath, "cache.dat"));
        var metadataFile = CreateFile(Path.Combine(metadataPath, "poster.jpg"));
        var nestedMetadataFile = CreateFile(Path.Combine(metadataPath, "library", "backdrop.jpg"));
        var similarlyNamedFile = CreateFile(Path.Combine(cachePath, "metadata2", "cache.dat"));
        var tempFile = CreateFile(Path.Combine(tempPath, "temp.dat"));
        var fileSystem = CreateFileSystem(
            cachePath,
            tempPath,
            [cacheFile, metadataFile, nestedMetadataFile, similarlyNamedFile],
            [tempFile]);
        var logger = new Mock<ILogger<DeleteCacheFileTask>>();
        var task = CreateTask(cachePath, tempPath, metadataPath, fileSystem.Object, logger.Object);

        task.ExecuteAsync(Mock.Of<IProgress<double>>(), CancellationToken.None);

        fileSystem.Verify(f => f.DeleteFile(cacheFile.FullName), Times.Once);
        fileSystem.Verify(f => f.DeleteFile(similarlyNamedFile.FullName), Times.Once);
        fileSystem.Verify(f => f.DeleteFile(tempFile.FullName), Times.Once);
        fileSystem.Verify(f => f.DeleteFile(metadataFile.FullName), Times.Never());
        fileSystem.Verify(f => f.DeleteFile(nestedMetadataFile.FullName), Times.Never());
        VerifyWarning(logger, Times.Once());
    }

    [Fact]
    public void ExecuteAsync_MetadataEqualsCache_DoesNotDeleteCacheFiles()
    {
        var cachePath = GetTestPath("cache");
        var tempPath = GetTestPath("temp");
        var cacheFile = CreateFile(Path.Combine(cachePath, "cache.dat"));
        var nestedCacheFile = CreateFile(Path.Combine(cachePath, "nested", "cache.dat"));
        var fileSystem = CreateFileSystem(cachePath, tempPath, [cacheFile, nestedCacheFile]);
        var logger = new Mock<ILogger<DeleteCacheFileTask>>();
        var task = CreateTask(cachePath, tempPath, cachePath, fileSystem.Object, logger.Object);

        task.ExecuteAsync(Mock.Of<IProgress<double>>(), CancellationToken.None);

        fileSystem.Verify(f => f.DeleteFile(It.IsAny<string>()), Times.Never());
        VerifyWarning(logger, Times.Once());
    }

    [Fact]
    public void ExecuteAsync_CacheDirectoryDoesNotExist_CompletesProgress()
    {
        var cachePath = GetTestPath("cache");
        var tempPath = GetTestPath("temp");
        var metadataPath = GetTestPath("metadata");
        var fileSystem = CreateFileSystem(cachePath, tempPath, []);
        fileSystem.Setup(f => f.GetFiles(cachePath, true)).Throws<DirectoryNotFoundException>();
        var progress = new Mock<IProgress<double>>();
        var task = CreateTask(
            cachePath,
            tempPath,
            metadataPath,
            fileSystem.Object,
            NullLogger<DeleteCacheFileTask>.Instance);

        task.ExecuteAsync(progress.Object, CancellationToken.None);

        progress.Verify(p => p.Report(100), Times.Once);
    }

    private static DeleteCacheFileTask CreateTask(
        string cachePath,
        string tempPath,
        string metadataPath,
        IFileSystem fileSystem,
        ILogger<DeleteCacheFileTask> logger)
    {
        var applicationPaths = new Mock<IServerApplicationPaths>();
        applicationPaths.SetupGet(p => p.CachePath).Returns(cachePath);
        applicationPaths.SetupGet(p => p.TempDirectory).Returns(tempPath);
        applicationPaths.SetupGet(p => p.InternalMetadataPath).Returns(metadataPath);

        return new DeleteCacheFileTask(
            applicationPaths.Object,
            logger,
            fileSystem,
            Mock.Of<ILocalizationManager>());
    }

    private static Mock<IFileSystem> CreateFileSystem(
        string cachePath,
        string tempPath,
        IReadOnlyList<FileSystemMetadata> cacheFiles,
        IReadOnlyList<FileSystemMetadata>? tempFiles = null)
    {
        var fileSystem = new Mock<IFileSystem>();
        fileSystem.Setup(f => f.GetFiles(cachePath, true)).Returns(cacheFiles);
        fileSystem.Setup(f => f.GetFiles(tempPath, true)).Returns(tempFiles ?? []);
        fileSystem.Setup(f => f.GetLastWriteTimeUtc(It.IsAny<FileSystemMetadata>())).Returns(_staleLastWriteTimeUtc);
        fileSystem.Setup(f => f.GetDirectoryPaths(It.IsAny<string>(), It.IsAny<bool>())).Returns([]);
        return fileSystem;
    }

    private static FileSystemMetadata CreateFile(string path)
        => new()
        {
            FullName = path,
            Name = Path.GetFileName(path)
        };

    private static string GetTestPath(string directory)
        => Path.Combine(Path.GetTempPath(), nameof(DeleteCacheFileTaskTests), directory);

    private static void VerifyWarning(Mock<ILogger<DeleteCacheFileTask>> logger, Times times)
    {
        logger.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("excluded from cache cleanup", StringComparison.Ordinal)),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            times);
    }
}
