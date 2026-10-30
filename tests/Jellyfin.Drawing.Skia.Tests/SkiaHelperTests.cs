using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging.Abstractions;
using SkiaSharp;
using Xunit;

namespace Jellyfin.Drawing.Skia.Tests;

/// <summary>
/// Reproduces GitHub issue #18321 where a multi-frame GIF causes an ArgumentException
/// to escape GetNextValidImage, leading to an endless retry loop.
/// </summary>
public class SkiaHelperTests
{
    private static SkiaEncoder CreateEncoder()
        => new(NullLogger<SkiaEncoder>.Instance, new FakeApplicationPaths());

    [Fact]
    public void GetNextValidImage_RealReportedGif_ReturnsNullInsteadOfThrowing()
    {
        // This test expects that folder.gif from issue #18321 has been downloaded
        // and placed in tests/Jellyfin.Drawing.Skia.Tests/TestData/folder.gif
        var gifPath = Path.Combine("TestData", "folder.gif");

        Assert.True(File.Exists(gifPath), $"File not found at path: {Path.GetFullPath(gifPath)}. Make sure to download it and place it in the TestData folder.");

        var encoder = CreateEncoder();
        var paths = new List<string> { gifPath };

        var bitmap = SkiaHelper.GetNextValidImage(encoder, paths, 0, out _);

        Assert.Null(bitmap);
        bitmap?.Dispose();
    }

    // ---------------------------------------------------------------------------
    // Minimal stub for IApplicationPaths
    // ---------------------------------------------------------------------------

    /// <summary>
    /// Minimal stub of <see cref="MediaBrowser.Common.Configuration.IApplicationPaths"/>
    /// that satisfies the <see cref="SkiaEncoder"/> constructor without requiring a
    /// mocking framework.  Only <c>TempDirectory</c> is accessed by the encoder
    /// (and only for paths with non-ASCII characters), so all other members are
    /// left unimplemented.
    /// </summary>
    private sealed class FakeApplicationPaths : MediaBrowser.Common.Configuration.IApplicationPaths
    {
        public string TempDirectory => Path.GetTempPath();

        public string ProgramDataPath => throw new NotImplementedException();

        public string WebPath => throw new NotImplementedException();

        public string ProgramSystemPath => throw new NotImplementedException();

        public string DataPath => throw new NotImplementedException();

        public string ImageCachePath => throw new NotImplementedException();

        public string PluginsPath => throw new NotImplementedException();

        public string PluginConfigurationsPath => throw new NotImplementedException();

        public string LogDirectoryPath => throw new NotImplementedException();

        public string ConfigurationDirectoryPath => throw new NotImplementedException();

        public string SystemConfigurationFilePath => throw new NotImplementedException();

        public string CachePath => throw new NotImplementedException();

        public string VirtualDataPath => throw new NotImplementedException();

        public string TrickplayPath => throw new NotImplementedException();

        public string BackupPath => throw new NotImplementedException();

        public void MakeSanityCheckOrThrow() => throw new NotImplementedException();

        public void CreateAndCheckMarker(string path, string markerName, bool recursive = false) => throw new NotImplementedException();
    }
}
