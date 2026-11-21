using System;
using System.IO;
using Emby.Server.Implementations.Library;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.IO;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Library;

public class ExternalDataManagerTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DeleteExternalItemFiles_PreservesAdjacentTrickplayExceptWhenMediaDeleted(bool deleteMediaTrickplay)
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var mediaFolder = Path.Combine(root, "media");
        var mediaTrickplay = Path.Combine(mediaFolder, "video.trickplay");
        var internalCache = Path.Combine(root, "cache", "trickplay");

        try
        {
            Directory.CreateDirectory(mediaTrickplay);
            Directory.CreateDirectory(internalCache);
            File.WriteAllText(Path.Combine(mediaTrickplay, "0.jpg"), "user-owned media-side thumbnail");
            File.WriteAllText(Path.Combine(internalCache, "0.jpg"), "internal cache");

            var movie = new Movie { Path = Path.Combine(mediaFolder, "video.mkv") };
            var paths = new Mock<IPathManager>();
            paths.Setup(p => p.GetTrickplayDirectory(movie, true)).Returns(mediaTrickplay);
            paths.Setup(p => p.GetExtractedDataPaths(movie))
                .Returns([internalCache, mediaTrickplay]);
            var manager = new ExternalDataManager(
                null!,
                null!,
                paths.Object,
                null!,
                null!,
                NullLogger<ExternalDataManager>.Instance);

            manager.DeleteExternalItemFiles(movie, deleteMediaTrickplay);

            Assert.Equal(!deleteMediaTrickplay, Directory.Exists(mediaTrickplay));
            Assert.False(Directory.Exists(internalCache));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, true);
            }
        }
    }
}
