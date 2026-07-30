using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Emby.Server.Implementations.ScheduledTasks.Tasks;
using Jellyfin.Server.Implementations.SystemBackupService;
using MediaBrowser.Controller;
using MediaBrowser.Controller.SystemBackupService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.ScheduledTasks;

public sealed class FullSystemBackupTaskTests
{
    [Fact]
    public async Task ExecuteAsync_CreatesMirrorsAndRetainsDatabaseBackups()
    {
        var root = Path.Combine(Path.GetTempPath(), "jellyfin-scheduled-backup-tests", Guid.NewGuid().ToString("N"));
        var backupDirectory = Path.Combine(root, "backups");
        var mirrorDirectory = Path.Combine(root, "mirror");
        Directory.CreateDirectory(backupDirectory);
        try
        {
            var backupPath = Path.Combine(backupDirectory, "jellyfin-backup-current.zip");
            await File.WriteAllTextAsync(backupPath, "backup", TestContext.Current.CancellationToken);
            var expiredPath = Path.Combine(backupDirectory, "jellyfin-backup-expired.zip");
            await File.WriteAllTextAsync(expiredPath, "old", TestContext.Current.CancellationToken);
            File.SetLastWriteTimeUtc(expiredPath, DateTime.UtcNow.AddDays(-3));

            var backupService = new Mock<IBackupService>();
            backupService
                .Setup(service => service.CreateBackupAsync(It.Is<BackupOptionsDto>(options => options.Database)))
                .ReturnsAsync(new BackupManifestDto
                {
                    ServerVersion = new Version(10, 12),
                    BackupEngineVersion = new Version(0, 2),
                    DateCreated = DateTimeOffset.UtcNow,
                    Path = backupPath,
                    Options = new BackupOptionsDto { Database = true }
                });
            var applicationPaths = new Mock<IServerApplicationPaths>();
            applicationPaths.SetupGet(paths => paths.BackupPath).Returns(backupDirectory);
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["CustomNetflix:BackupMirrorPath"] = mirrorDirectory,
                    ["CustomNetflix:BackupRetentionDays"] = "1"
                })
                .Build();
            var task = new FullSystemBackupTask(
                backupService.Object,
                applicationPaths.Object,
                configuration,
                NullLogger<FullSystemBackupTask>.Instance);

            await task.ExecuteAsync(new Progress<double>(), TestContext.Current.CancellationToken);

            Assert.True(File.Exists(Path.Combine(mirrorDirectory, Path.GetFileName(backupPath))));
            Assert.False(File.Exists(expiredPath));
            backupService.VerifyAll();
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public async Task ExecuteAsync_MirrorFailureStillAppliesLocalRetention()
    {
        var root = Path.Combine(Path.GetTempPath(), "jellyfin-scheduled-backup-tests", Guid.NewGuid().ToString("N"));
        var backupDirectory = Path.Combine(root, "backups");
        Directory.CreateDirectory(backupDirectory);
        try
        {
            var backupPath = Path.Combine(backupDirectory, "jellyfin-backup-current.zip");
            await File.WriteAllTextAsync(backupPath, "backup", TestContext.Current.CancellationToken);
            var expiredPath = Path.Combine(backupDirectory, "jellyfin-backup-expired.zip");
            await File.WriteAllTextAsync(expiredPath, "old", TestContext.Current.CancellationToken);
            File.SetLastWriteTimeUtc(expiredPath, DateTime.UtcNow.AddDays(-3));

            var invalidMirrorPath = Path.Combine(root, "mirror-file");
            await File.WriteAllTextAsync(invalidMirrorPath, "not a directory", TestContext.Current.CancellationToken);
            var backupService = new Mock<IBackupService>();
            backupService
                .Setup(service => service.CreateBackupAsync(It.Is<BackupOptionsDto>(options => options.Database)))
                .ReturnsAsync(new BackupManifestDto
                {
                    ServerVersion = new Version(10, 12),
                    BackupEngineVersion = new Version(0, 2),
                    DateCreated = DateTimeOffset.UtcNow,
                    Path = backupPath,
                    Options = new BackupOptionsDto { Database = true }
                });
            var applicationPaths = new Mock<IServerApplicationPaths>();
            applicationPaths.SetupGet(paths => paths.BackupPath).Returns(backupDirectory);
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["CustomNetflix:BackupMirrorPath"] = invalidMirrorPath,
                    ["CustomNetflix:BackupRetentionDays"] = "1"
                })
                .Build();
            var task = new FullSystemBackupTask(
                backupService.Object,
                applicationPaths.Object,
                configuration,
                NullLogger<FullSystemBackupTask>.Instance);

            await Assert.ThrowsAnyAsync<IOException>(
                () => task.ExecuteAsync(new Progress<double>(), TestContext.Current.CancellationToken));

            Assert.False(File.Exists(expiredPath));
            backupService.VerifyAll();
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }
}
