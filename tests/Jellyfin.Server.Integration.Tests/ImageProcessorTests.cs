using System;
using System.Globalization;
using System.IO;
using Jellyfin.Drawing;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Integration.Tests;

public sealed class ImageProcessorTests : IDisposable
{
    private const string CacheRoot = "image-cache";
    private const string OriginalPath = "/media/poster.jpg";
    private const string NoOverlayCacheKey = "/media/poster.jpg,quality=90,datemodified=638800000000000000,f=Jpg,width=200,height=300,maxwidth=400,maxheight=500,fillwidth=600,fillheight=700,blur=2,b=000000,fl=layer,v=4";
    private static readonly DateTime _dateModified = new(638800000000000000, DateTimeKind.Utc);
    private readonly ImageProcessor _imageProcessor;

    public ImageProcessorTests()
    {
        var applicationPaths = new Mock<IServerApplicationPaths>();
        applicationPaths.SetupGet(paths => paths.ImageCachePath).Returns(CacheRoot);

        var configurationManager = new Mock<IServerConfigurationManager>();
        configurationManager
            .SetupGet(manager => manager.Configuration)
            .Returns(new ServerConfiguration { ParallelImageEncodingLimit = 1 });

        _imageProcessor = new ImageProcessor(
            NullLogger<ImageProcessor>.Instance,
            applicationPaths.Object,
            Mock.Of<IFileSystem>(),
            Mock.Of<IImageEncoder>(),
            configurationManager.Object);
    }

    [Fact]
    public void GetCacheFilePath_DifferentOverlayTypes_ReturnDifferentPaths()
    {
        var percentPlayedPath = GetCacheFilePath(percentPlayed: 1);
        var unwatchedCountPath = GetCacheFilePath(unwatchedCount: 1);

        Assert.NotEqual(percentPlayedPath, unwatchedCountPath);
    }

    [Fact]
    public void GetCacheFilePath_DifferentPercentPlayedValues_ReturnDifferentPaths()
    {
        var firstPath = GetCacheFilePath(percentPlayed: 12.5);
        var secondPath = GetCacheFilePath(percentPlayed: 75.5);

        Assert.NotEqual(firstPath, secondPath);
    }

    [Fact]
    public void GetCacheFilePath_DifferentUnwatchedCountValues_ReturnDifferentPaths()
    {
        var firstPath = GetCacheFilePath(unwatchedCount: 1);
        var secondPath = GetCacheFilePath(unwatchedCount: 2);

        Assert.NotEqual(firstPath, secondPath);
    }

    [Fact]
    public void GetCacheFilePath_DifferentCultures_ReturnSamePath()
    {
        var originalCulture = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            var expectedPath = GetCacheFilePath(percentPlayed: 12.5);

            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            var actualPath = GetCacheFilePath(percentPlayed: 12.5);

            Assert.Equal(expectedPath, actualPath);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public void GetCacheFilePath_NoOverlay_UsesVersionFourWithExistingSerialization()
    {
        var expectedPath = _imageProcessor.GetCachePath(
            Path.Combine(CacheRoot, "resized-images"),
            NoOverlayCacheKey,
            ".jpg");

        Assert.Equal(expectedPath, GetCacheFilePath());
    }

    public void Dispose()
    {
        _imageProcessor.Dispose();
    }

    private string GetCacheFilePath(double percentPlayed = 0, int? unwatchedCount = null)
        => _imageProcessor.GetCacheFilePath(
            OriginalPath,
            width: 200,
            height: 300,
            maxWidth: 400,
            maxHeight: 500,
            fillWidth: 600,
            fillHeight: 700,
            quality: 90,
            dateModified: _dateModified,
            format: ImageFormat.Jpg,
            percentPlayed,
            unwatchedCount,
            blur: 2,
            backgroundColor: "000000",
            foregroundLayer: "layer");
}
