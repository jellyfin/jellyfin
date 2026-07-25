using System;
using System.IO;
using Emby.Server.Implementations;
using Emby.Server.Implementations.Configuration;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Configuration;

public sealed class ServerConfigurationManagerTests : IDisposable
{
    private readonly string _testRoot;
    private readonly ServerApplicationPaths _applicationPaths;

    public ServerConfigurationManagerTests()
    {
        _testRoot = Path.Combine(Path.GetTempPath(), nameof(ServerConfigurationManagerTests), Guid.NewGuid().ToString("N"));
        _applicationPaths = new ServerApplicationPaths(
            Path.Combine(_testRoot, "program-data"),
            Path.Combine(_testRoot, "log"),
            Path.Combine(_testRoot, "config"),
            Path.Combine(_testRoot, "cache"),
            Path.Combine(_testRoot, "web"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, true);
        }
    }

    [Fact]
    public void MakeSanityCheckOrThrow_MetadataPathNestedInsideCache_Throws()
    {
        _applicationPaths.InternalMetadataPath = Path.Combine(_applicationPaths.CachePath, "metadata");

        Assert.Throws<InvalidOperationException>(_applicationPaths.MakeSanityCheckOrThrow);
    }

    [Fact]
    public void MakeSanityCheckOrThrow_DefaultMetadataPath_CreatesMarker()
    {
        _applicationPaths.MakeSanityCheckOrThrow();

        Assert.True(File.Exists(Path.Combine(_applicationPaths.InternalMetadataPath, ".jellyfin-metadata")));
    }

    [Theory]
    [InlineData("cache")]
    [InlineData("data")]
    [InlineData("config")]
    public void ReplaceConfiguration_MetadataPathNestedInsideJellyfinDirectory_ThrowsArgumentException(string directory)
    {
        var metadataPath = Path.Combine(GetApplicationPath(directory), "metadata");
        Directory.CreateDirectory(metadataPath);
        var configurationManager = CreateConfigurationManager();
        var newConfiguration = new ServerConfiguration
        {
            CachePath = _applicationPaths.CachePath,
            MetadataPath = metadataPath
        };

        Assert.Throws<ArgumentException>(() => configurationManager.ReplaceConfiguration(newConfiguration));
        Assert.Empty(configurationManager.Configuration.MetadataPath);
    }

    [Fact]
    public void ReplaceConfiguration_MetadataPathIsSibling_AcceptsPathAndCreatesMarker()
    {
        var metadataPath = Path.Combine(_testRoot, "metadata");
        Directory.CreateDirectory(metadataPath);
        var configurationManager = CreateConfigurationManager();
        var newConfiguration = new ServerConfiguration
        {
            CachePath = _applicationPaths.CachePath,
            MetadataPath = metadataPath
        };

        configurationManager.ReplaceConfiguration(newConfiguration);

        Assert.Equal(metadataPath, configurationManager.Configuration.MetadataPath);
        Assert.True(File.Exists(Path.Combine(metadataPath, ".jellyfin-metadata")));
    }

    [Fact]
    public void ValidateMetadataPathOrThrow_NestedPathWithTrailingSeparator_Throws()
    {
        var metadataPath = Path.Combine(_applicationPaths.CachePath, "metadata")
            + Path.DirectorySeparatorChar;

        Assert.Throws<InvalidOperationException>(
            () => _applicationPaths.ValidateMetadataPathOrThrow(metadataPath));
    }

    [Fact]
    public void ValidateMetadataPathOrThrow_CaseDifference_UsesPlatformPathComparison()
    {
        var cachePath = _applicationPaths.CachePath.ToUpperInvariant();
        var metadataPath = Path.Combine(cachePath, "metadata");

        if (OperatingSystem.IsWindows())
        {
            Assert.Throws<InvalidOperationException>(
                () => _applicationPaths.ValidateMetadataPathOrThrow(metadataPath));
        }
        else
        {
            _applicationPaths.ValidateMetadataPathOrThrow(metadataPath);
        }
    }

    private ServerConfigurationManager CreateConfigurationManager()
    {
        return new ServerConfigurationManager(
            _applicationPaths,
            NullLoggerFactory.Instance,
            Mock.Of<IXmlSerializer>());
    }

    private string GetApplicationPath(string directory)
    {
        return directory switch
        {
            "cache" => _applicationPaths.CachePath,
            "data" => _applicationPaths.DataPath,
            "config" => _applicationPaths.ConfigurationDirectoryPath,
            _ => throw new ArgumentException($"Unknown application directory: {directory}", nameof(directory))
        };
    }
}
