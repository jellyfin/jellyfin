using System;
using System.IO;
using Jellyfin.Api.Controllers;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Common.Updates;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Jellyfin.Api.Tests.Controllers;

// Covers the path-traversal validation in GetPluginImage: a plugin's manifest ImagePath
// must resolve to a file inside the plugin's own directory.
public sealed class PluginsControllerTests
{
    private readonly Mock<IPluginManager> _pluginManager = new();
    private readonly string _pluginPath;

    public PluginsControllerTests()
    {
        _pluginPath = Path.Combine(Path.GetTempPath(), "jellyfin-plugin-image-tests");
        Directory.CreateDirectory(_pluginPath);
    }

    private PluginsController CreateController() =>
        new PluginsController(Mock.Of<IInstallationManager>(), _pluginManager.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

    private void SetupPlugin(Guid id, Version version, string? imagePath)
    {
        var manifest = new PluginManifest { Id = id, Name = "Test", Version = version.ToString(), ImagePath = imagePath };
        _pluginManager.Setup(p => p.GetPlugin(id, version))
            .Returns(new LocalPlugin(_pluginPath, true, manifest));
    }

    [Fact]
    public void GetPluginImage_UnknownPlugin_ReturnsNotFound()
    {
        var result = CreateController().GetPluginImage(Guid.NewGuid(), new Version(1, 0));

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public void GetPluginImage_ImageInsidePluginPath_ReturnsFile()
    {
        var id = Guid.NewGuid();
        var version = new Version(1, 0);
        File.WriteAllBytes(Path.Combine(_pluginPath, "logo.png"), Array.Empty<byte>());
        SetupPlugin(id, version, "logo.png");

        var result = CreateController().GetPluginImage(id, version);

        Assert.IsType<PhysicalFileResult>(result);
    }

    [Fact]
    public void GetPluginImage_ImageInsidePluginPathButMissing_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        var version = new Version(1, 0);
        SetupPlugin(id, version, "does-not-exist.png");

        var result = CreateController().GetPluginImage(id, version);

        Assert.IsType<NotFoundResult>(result);
    }

    [Theory]
    [InlineData("../../../../etc/passwd")]
    [InlineData("subdir/../../../../etc/passwd")]
    public void GetPluginImage_TraversalOutsidePluginPath_ReturnsNotFound(string imagePath)
    {
        var id = Guid.NewGuid();
        var version = new Version(1, 0);
        SetupPlugin(id, version, imagePath);

        var result = CreateController().GetPluginImage(id, version);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public void GetPluginImage_SiblingPrefixDirectory_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        var version = new Version(1, 0);
        // Resolves to "<pluginPath>-evil/logo.png", which shares the plugin path as a string prefix.
        // The file is created so the check fails on the boundary, not on File.Exists.
        var siblingDir = _pluginPath + "-evil";
        Directory.CreateDirectory(siblingDir);
        File.WriteAllBytes(Path.Combine(siblingDir, "logo.png"), Array.Empty<byte>());
        SetupPlugin(id, version, "../jellyfin-plugin-image-tests-evil/logo.png");

        var result = CreateController().GetPluginImage(id, version);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public void GetPluginImage_AbsoluteImagePath_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        var version = new Version(1, 0);
        SetupPlugin(id, version, OperatingSystem.IsWindows() ? "C:\\Windows\\win.ini" : "/etc/passwd");

        var result = CreateController().GetPluginImage(id, version);

        Assert.IsType<NotFoundResult>(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetPluginImage_NoImagePathOrResource_ReturnsNotFound(string? imagePath)
    {
        var id = Guid.NewGuid();
        var version = new Version(1, 0);
        SetupPlugin(id, version, imagePath);

        var result = CreateController().GetPluginImage(id, version);

        Assert.IsType<NotFoundResult>(result);
    }
}
