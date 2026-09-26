using System;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Emby.Server.Implementations;
using Jellyfin.Server.Migrations.PreStartupRoutines;
using Jellyfin.Server.Migrations.Routines;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Tests.Migrations;

public sealed class FixNullEncoderPresetTests : IDisposable
{
    private readonly string _configurationDirectory;
    private readonly string _encodingConfigurationPath;
    private readonly FixNullEncoderPreset _migration;

    public FixNullEncoderPresetTests()
    {
        _configurationDirectory = Directory.CreateTempSubdirectory("jellyfin-migration-test-").FullName;
        _encodingConfigurationPath = Path.Combine(_configurationDirectory, "encoding.xml");

        var applicationPaths = new Mock<IApplicationPaths>();
        applicationPaths.SetupGet(paths => paths.ConfigurationDirectoryPath).Returns(_configurationDirectory);
        _migration = new FixNullEncoderPreset(applicationPaths.Object, NullLogger<FixNullEncoderPreset>.Instance);
    }

    [Fact]
    public async Task PerformAsync_ReplacesNullEncoderPresetWithAuto()
    {
        const string Configuration = """
            <?xml version="1.0" encoding="utf-8"?>
            <EncodingOptions xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
              <HardwareAccelerationType>qsv</HardwareAccelerationType>
              <EncoderPreset xsi:nil="true" />
              <EnableHardwareEncoding>true</EnableHardwareEncoding>
            </EncodingOptions>
            """;
        await File.WriteAllTextAsync(_encodingConfigurationPath, Configuration, TestContext.Current.CancellationToken);

        await _migration.PerformAsync(TestContext.Current.CancellationToken);

        var serializer = new XmlSerializer(typeof(EncodingOptions));
        using var reader = File.OpenRead(_encodingConfigurationPath);
        var encodingOptions = Assert.IsType<EncodingOptions>(serializer.Deserialize(reader));
        Assert.Equal(EncoderPreset.auto, encodingOptions.EncoderPreset);
        Assert.Equal(HardwareAccelerationType.qsv, encodingOptions.HardwareAccelerationType);
        Assert.True(encodingOptions.EnableHardwareEncoding);
    }

    [Fact]
    public async Task PerformAsync_LeavesValidEncoderPresetUnchanged()
    {
        const string Configuration = """
            <?xml version="1.0" encoding="utf-8"?>
            <EncodingOptions xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
              <EncoderPreset>fast</EncoderPreset>
            </EncodingOptions>
            """;
        await File.WriteAllTextAsync(_encodingConfigurationPath, Configuration, TestContext.Current.CancellationToken);

        await _migration.PerformAsync(TestContext.Current.CancellationToken);

        Assert.Equal(Configuration, await File.ReadAllTextAsync(_encodingConfigurationPath, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task PerformAsync_LeavesMalformedConfigurationUnchanged()
    {
        const string Configuration = "<EncodingOptions><EncoderPreset xsi:nil=\"true\" />";
        await File.WriteAllTextAsync(_encodingConfigurationPath, Configuration, TestContext.Current.CancellationToken);

        await _migration.PerformAsync(TestContext.Current.CancellationToken);

        Assert.Equal(Configuration, await File.ReadAllTextAsync(_encodingConfigurationPath, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task MigrateEncodingOptions_UsesAutoForNullEncoderPreset()
    {
        const string Configuration = """
            <?xml version="1.0" encoding="utf-8"?>
            <EncodingOptions xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
              <HardwareAccelerationType>qsv</HardwareAccelerationType>
              <TonemappingRange>tv</TonemappingRange>
              <EncoderPreset xsi:nil="true" />
              <EnableHardwareEncoding>true</EnableHardwareEncoding>
            </EncodingOptions>
            """;
        await File.WriteAllTextAsync(_encodingConfigurationPath, Configuration, TestContext.Current.CancellationToken);
        var applicationPaths = new ServerApplicationPaths(
            _configurationDirectory,
            _configurationDirectory,
            _configurationDirectory,
            _configurationDirectory,
            _configurationDirectory);

        var migration = new MigrateEncodingOptions(applicationPaths, NullLoggerFactory.Instance);
        migration.Perform();

        var serializer = new XmlSerializer(typeof(EncodingOptions));
        using var reader = File.OpenRead(_encodingConfigurationPath);
        var encodingOptions = Assert.IsType<EncodingOptions>(serializer.Deserialize(reader));
        Assert.Equal(EncoderPreset.auto, encodingOptions.EncoderPreset);
        Assert.Equal(HardwareAccelerationType.qsv, encodingOptions.HardwareAccelerationType);
        Assert.True(encodingOptions.EnableHardwareEncoding);
    }

    public void Dispose()
    {
        Directory.Delete(_configurationDirectory, true);
    }
}
