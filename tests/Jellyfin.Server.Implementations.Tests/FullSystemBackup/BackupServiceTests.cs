using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Jellyfin.Server.Implementations.FullSystemBackup;
using MediaBrowser.Controller;
using MediaBrowser.Controller.SystemBackupService;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.FullSystemBackup
{
    public sealed class BackupServiceTests : IDisposable
    {
        private readonly string _testRoot;
        private bool _disposed;

        public BackupServiceTests()
        {
            _testRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_testRoot);
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(100, 120)]
        [InlineData(101, 122)]
        public void AddHeadroom_ReturnsExpectedValue(long input, long expected)
        {
            Assert.Equal(expected, BackupService.AddHeadroom(input));
        }

        [Fact]
        public void EstimateBackupRequirement_IncludesExpectedFilesAndManifestOverhead()
        {
            CreateFile(Path.Combine("config", "system.xml"), 10);
            CreateFile(Path.Combine("config", "network.json"), 11);
            CreateFile(Path.Combine("config", "ignore.txt"), 999);
            CreateFile(Path.Combine("config", "users", "user.db"), 7);
            CreateFile(Path.Combine("config", "ScheduledTasks", "task.json"), 9);
            CreateFile(Path.Combine("root", "library.xml"), 13);
            CreateFile(Path.Combine("data", "collections", "collections.json"), 17);
            CreateFile(Path.Combine("data", "playlists", "playlist.m3u"), 19);
            CreateFile(Path.Combine("data", "ScheduledTasks", "state.json"), 23);
            CreateFile(Path.Combine("data", "subtitles", "subtitle.srt"), 29);
            CreateFile(Path.Combine("data", "trickplay", "thumb.bin"), 31);
            CreateFile(Path.Combine("metadata-custom", "poster.jpg"), 37);
            CreateFile(Path.Combine("metadata-default", "backdrop.jpg"), 41);

            var applicationPaths = CreateApplicationPaths();
            var options = new BackupOptionsDto
            {
                Database = false,
                Metadata = true,
                Subtitles = true,
                Trickplay = true
            };

            var requirement = BackupService.EstimateBackupRequirement(applicationPaths, options);

            Assert.Equal((128 * 1024) + 10 + 11 + 7 + 9 + 13 + 17 + 19 + 23 + 29 + 31 + 37 + 41, requirement);
        }

        [Fact]
        public void EstimateBackupRequirement_DoesNotDoubleCountSharedMetadataPath()
        {
            CreateFile(Path.Combine("metadata-shared", "poster.jpg"), 37);

            var applicationPaths = CreateApplicationPaths(defaultMetadataPath: Path.Combine(_testRoot, "metadata-shared"), internalMetadataPath: Path.Combine(_testRoot, "metadata-shared"));
            var options = new BackupOptionsDto
            {
                Database = false,
                Metadata = true,
                Subtitles = false,
                Trickplay = false
            };

            var requirement = BackupService.EstimateBackupRequirement(applicationPaths, options);

            Assert.Equal((128 * 1024) + 37, requirement);
        }

        [Fact]
        public void EstimateRestoreRequirements_MapsArchiveContentToExpectedPaths()
        {
            var zipPath = Path.Combine(_testRoot, "restore.zip");
            CreateArchive(zipPath, new Dictionary<string, int>
            {
                ["Config/system.json"] = 10,
                ["Root/item.bin"] = 20,
                ["Data/collections/collection.bin"] = 30,
                ["Data/metadata/poster.bin"] = 40,
                ["Data/metadata-default/backdrop.bin"] = 50,
                ["Database/TypedBaseItemRepository.json"] = 60
            });

            var applicationPaths = CreateApplicationPaths();
            using var archive = ZipFile.OpenRead(zipPath);

            var requirements = BackupService.EstimateRestoreRequirements(
                archive,
                applicationPaths,
                includeDatabase: true,
                dbStoragePath: Path.Combine(_testRoot, "database"));

            Assert.Equal(10, requirements[applicationPaths.ConfigurationDirectoryPath]);
            Assert.Equal(20, requirements[applicationPaths.RootFolderPath]);
            Assert.Equal(30, requirements[applicationPaths.DataPath]);
            Assert.Equal(40, requirements[applicationPaths.InternalMetadataPath]);
            Assert.Equal(50, requirements[applicationPaths.DefaultInternalMetadataPath]);
            Assert.Equal(60, requirements[Path.Combine(_testRoot, "database")]);
        }

        [Fact]
        public void EstimateRestoreRequirements_AddsDatabaseRequirementToExistingPath()
        {
            var zipPath = Path.Combine(_testRoot, "restore-shared-path.zip");
            CreateArchive(zipPath, new Dictionary<string, int>
            {
                ["Data/playlists/playlist.bin"] = 30,
                ["Database/TypedBaseItemRepository.json"] = 60
            });

            var applicationPaths = CreateApplicationPaths();
            using var archive = ZipFile.OpenRead(zipPath);

            var requirements = BackupService.EstimateRestoreRequirements(
                archive,
                applicationPaths,
                includeDatabase: true,
                dbStoragePath: applicationPaths.DataPath);

            Assert.Equal(90, requirements[applicationPaths.DataPath]);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed || !disposing)
            {
                return;
            }

            if (Directory.Exists(_testRoot))
            {
                Directory.Delete(_testRoot, true);
            }

            _disposed = true;
        }

        private IServerApplicationPaths CreateApplicationPaths(string? defaultMetadataPath = null, string? internalMetadataPath = null)
        {
            var applicationPaths = new Mock<IServerApplicationPaths>();
            applicationPaths.SetupGet(x => x.ConfigurationDirectoryPath).Returns(Path.Combine(_testRoot, "config"));
            applicationPaths.SetupGet(x => x.RootFolderPath).Returns(Path.Combine(_testRoot, "root"));
            applicationPaths.SetupGet(x => x.DataPath).Returns(Path.Combine(_testRoot, "data"));
            applicationPaths.SetupGet(x => x.InternalMetadataPath).Returns(internalMetadataPath ?? Path.Combine(_testRoot, "metadata-custom"));
            applicationPaths.SetupGet(x => x.DefaultInternalMetadataPath).Returns(defaultMetadataPath ?? Path.Combine(_testRoot, "metadata-default"));
            return applicationPaths.Object;
        }

        private void CreateFile(string relativePath, int length)
        {
            var path = Path.Combine(_testRoot, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllBytes(path, new byte[length]);
        }

        private static void CreateArchive(string zipPath, IReadOnlyDictionary<string, int> entries)
        {
            using var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create);
            foreach (var entry in entries)
            {
                var zipEntry = archive.CreateEntry(entry.Key);
                using var stream = zipEntry.Open();
                stream.Write(new byte[entry.Value]);
            }
        }
    }
}
