using System;
using System.IO;
using System.IO.Compression;
using MediaBrowser.MediaEncoding.BdInfo;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using Moq;
using Xunit;

namespace Jellyfin.MediaEncoding.Tests.BdInfo;

public class BdInfoExaminerTests
{
    [Fact]
    public void GetDiscInfo_UdfBluRayIso_ReadsPlaylistStreamLanguage()
    {
        var compressedPath = Path.Combine(
            AppContext.BaseDirectory,
            "Test Data",
            "BDInfo",
            "udf-bdmv.iso.gz");
        var path = Path.Combine(
            Path.GetTempPath(),
            "jellyfin-udf-bdmv-" + Guid.NewGuid().ToString("N") + ".iso");

        try
        {
            using (var compressedStream = File.OpenRead(compressedPath))
            using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
            using (var outputStream = File.Create(path))
            {
                gzipStream.CopyTo(outputStream);
            }

            var metadata = new FileSystemMetadata
            {
                Exists = true,
                FullName = path,
                Name = Path.GetFileName(path),
                Extension = Path.GetExtension(path),
                IsDirectory = false
            };
            var fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
            fileSystem.Setup(fs => fs.GetFileSystemInfo(path))
                .Returns(metadata);

            var result = new BdInfoExaminer(fileSystem.Object).GetDiscInfo(path);

            Assert.True(result.IsIso);
            Assert.Equal("00001.MPLS", result.PlaylistName);
            Assert.Empty(result.Files);
            var mediaStream = Assert.Single(result.MediaStreams);
            Assert.Equal(0, mediaStream.Index);
            Assert.Equal(MediaStreamType.Subtitle, mediaStream.Type);
            Assert.Equal("pgssub", mediaStream.Codec, StringComparer.OrdinalIgnoreCase);
            Assert.Equal("eng", mediaStream.Language);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
