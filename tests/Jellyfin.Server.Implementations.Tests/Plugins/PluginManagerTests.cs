using System;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using AutoFixture;
using Emby.Server.Implementations.Library;
using Emby.Server.Implementations.Plugins;
using Jellyfin.Extensions.Json;
using Jellyfin.Extensions.Json.Converters;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Updates;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Plugins
{
    public class PluginManagerTests
    {
        private static readonly string _testPathRoot = Path.Combine(Path.GetTempPath(), "jellyfin-test-data");

        private string _tempPath = string.Empty;

        private string _pluginPath = string.Empty;

        private JsonSerializerOptions _options;

        public PluginManagerTests()
        {
            (_tempPath, _pluginPath) = GetTestPaths("plugin-" + Path.GetRandomFileName());

            Directory.CreateDirectory(_pluginPath);

            _options = GetTestSerializerOptions();
        }

        [Fact]
        public void SaveManifest_RoundTrip_Success()
        {
            var pluginManager = new PluginManager(new NullLogger<PluginManager>(), null!, null!, null!, new Version(1, 0));
            var manifest = new PluginManifest()
            {
                Version = "1.0"
            };

            Assert.True(pluginManager.SaveManifest(manifest, _pluginPath));

            var res = pluginManager.LoadManifest(_pluginPath);

            Assert.Equal(manifest.Category, res.Manifest.Category);
            Assert.Equal(manifest.Changelog, res.Manifest.Changelog);
            Assert.Equal(manifest.Description, res.Manifest.Description);
            Assert.Equal(manifest.Id, res.Manifest.Id);
            Assert.Equal(manifest.Name, res.Manifest.Name);
            Assert.Equal(manifest.Overview, res.Manifest.Overview);
            Assert.Equal(manifest.Owner, res.Manifest.Owner);
            Assert.Equal(manifest.TargetAbi, res.Manifest.TargetAbi);
            Assert.Equal(manifest.Timestamp, res.Manifest.Timestamp);
            Assert.Equal(manifest.Version, res.Manifest.Version);
            Assert.Equal(manifest.Status, res.Manifest.Status);
            Assert.Equal(manifest.AutoUpdate, res.Manifest.AutoUpdate);
            Assert.Equal(manifest.ImagePath, res.Manifest.ImagePath);
            Assert.Equal(manifest.Assemblies, res.Manifest.Assemblies);
        }

        /// <summary>
        ///  Tests safe traversal within the plugin directory.
        /// </summary>
        /// <param name="dllFile">The safe path to evaluate.</param>
        [Theory]
        [InlineData("./some.dll")]
        [InlineData("some.dll")]
        [InlineData("sub/path/some.dll")]
        public void Constructor_DiscoversSafePluginAssembly_Status_Active(string dllFile)
        {
            var manifest = new PluginManifest
            {
                Id = Guid.NewGuid(),
                Name = "Safe Assembly",
                Assemblies = new string[] { dllFile }
            };

            var filename = Path.GetFileName(dllFile)!;
            var dllPath = Path.GetDirectoryName(Path.Combine(_pluginPath, dllFile))!;

            Directory.CreateDirectory(dllPath);
            File.Create(Path.Combine(dllPath, filename));
            var metafilePath = Path.Combine(_pluginPath, "meta.json");

            File.WriteAllText(metafilePath, JsonSerializer.Serialize(manifest, _options));

            var pluginManager = new PluginManager(new NullLogger<PluginManager>(), null!, null!, _tempPath, new Version(1, 0));

            var res = JsonSerializer.Deserialize<PluginManifest>(File.ReadAllText(metafilePath), _options);

            var expectedFullPath = Path.Combine(_pluginPath, dllFile).Canonicalize();

            Assert.NotNull(res);
            Assert.NotEmpty(pluginManager.Plugins);
            Assert.Equal(PluginStatus.Active, res!.Status);
            Assert.Equal(expectedFullPath, pluginManager.Plugins[0].DllFiles[0]);
            Assert.StartsWith(_pluginPath, expectedFullPath, StringComparison.InvariantCulture);
        }

        /// <summary>
        ///  Tests unsafe attempts to traverse to higher directories.
        /// </summary>
        /// <remarks>
        ///  Attempts to load directories outside of the plugin should be
        ///  constrained. Path traversal, shell expansion, and double encoding
        ///  can be used to load unintended files.
        ///  See <see href="https://owasp.org/www-community/attacks/Path_Traversal"/> for more.
        /// </remarks>
        /// <param name="unsafePath">The unsafe path to evaluate.</param>
        [Theory]
        [InlineData("/some.dll")] // Root path.
        [InlineData("../some.dll")] // Simple traversal.
        [InlineData("C:\\some.dll")] // Windows root path.
        [InlineData("test.txt")] // Not a DLL
        [InlineData(".././.././../some.dll")] // Traversal with current and parent
        [InlineData(@"..\.\..\.\..\some.dll")] // Windows traversal with current and parent
        [InlineData(@"\\network\resource.dll")] // UNC Path
        [InlineData("https://jellyfin.org/some.dll")] // URL
        [InlineData("~/some.dll")] // Tilde poses a shell expansion risk, but is a valid path character.
        public void Constructor_DiscoversUnsafePluginAssembly_Status_Malfunctioned(string unsafePath)
        {
            var manifest = new PluginManifest
            {
                Id = Guid.NewGuid(),
                Name = "Unsafe Assembly",
                Assemblies = new string[] { unsafePath }
            };

            // Only create very specific files. Otherwise the test will be exploiting path traversal.
            var files = new string[]
            {
                "../other.dll",
                "some.dll"
            };

            foreach (var file in files)
            {
                File.Create(Path.Combine(_pluginPath, file));
            }

            var metafilePath = Path.Combine(_pluginPath, "meta.json");

            File.WriteAllText(metafilePath, JsonSerializer.Serialize(manifest, _options));

            var pluginManager = new PluginManager(new NullLogger<PluginManager>(), null!, null!, _tempPath, new Version(1, 0));

            var res = JsonSerializer.Deserialize<PluginManifest>(File.ReadAllText(metafilePath), _options);

            Assert.NotNull(res);
            Assert.Empty(pluginManager.Plugins);
            Assert.Equal(PluginStatus.Malfunctioned, res!.Status);
        }

        [Fact]
        public async Task PopulateManifest_ExistingMetafilePlugin_PopulatesMissingFields()
        {
            var packageInfo = GenerateTestPackage();

            // Partial plugin without a name, but matching version and package ID
            var partial = new PluginManifest
            {
                Id = packageInfo.Id,
                AutoUpdate = false, // Turn off AutoUpdate
                Status = PluginStatus.Restart,
                Version = new Version(1, 0, 0).ToString(),
                Assemblies = new[] { "Jellyfin.Test.dll" }
            };

            var expectedManifest = new PluginManifest
            {
                Id = partial.Id,
                Name = packageInfo.Name,
                AutoUpdate = partial.AutoUpdate,
                Status = PluginStatus.Active,
                Owner = packageInfo.Owner,
                Assemblies = partial.Assemblies,
                Category = packageInfo.Category,
                Description = packageInfo.Description,
                Overview = packageInfo.Overview,
                TargetAbi = packageInfo.Versions[0].TargetAbi!,
                Timestamp = DateTime.Parse(packageInfo.Versions[0].Timestamp!, CultureInfo.InvariantCulture),
                Changelog = packageInfo.Versions[0].Changelog!,
                Version = new Version(1, 0).ToString(),
                ImagePath = string.Empty
            };

            var metafilePath = Path.Combine(_pluginPath, "meta.json");
            await File.WriteAllTextAsync(metafilePath, JsonSerializer.Serialize(partial, _options));

            var pluginManager = new PluginManager(new NullLogger<PluginManager>(), null!, null!, _tempPath, new Version(1, 0));

            await pluginManager.PopulateManifest(packageInfo, new Version(1, 0), _pluginPath, PluginStatus.Active);

            var resultBytes = await File.ReadAllBytesAsync(metafilePath);
            var result = JsonSerializer.Deserialize<PluginManifest>(resultBytes, _options);

            Assert.NotNull(result);
            Assert.Equivalent(expectedManifest, result);
        }

        [Fact]
        public async Task PopulateManifest_NoMetafile_PreservesManifest()
        {
            var packageInfo = GenerateTestPackage();
            var expectedManifest = new PluginManifest
            {
                Id = packageInfo.Id,
                Name = packageInfo.Name,
                AutoUpdate = true,
                Status = PluginStatus.Active,
                Owner = packageInfo.Owner,
                Assemblies = Array.Empty<string>(),
                Category = packageInfo.Category,
                Description = packageInfo.Description,
                Overview = packageInfo.Overview,
                TargetAbi = packageInfo.Versions[0].TargetAbi!,
                Timestamp = DateTime.Parse(packageInfo.Versions[0].Timestamp!, CultureInfo.InvariantCulture),
                Changelog = packageInfo.Versions[0].Changelog!,
                Version = packageInfo.Versions[0].Version,
                ImagePath = string.Empty
            };

            var pluginManager = new PluginManager(new NullLogger<PluginManager>(), null!, null!, null!, new Version(1, 0));

            await pluginManager.PopulateManifest(packageInfo, new Version(1, 0), _pluginPath, PluginStatus.Active);

            var metafilePath = Path.Combine(_pluginPath, "meta.json");
            var resultBytes = await File.ReadAllBytesAsync(metafilePath);
            var result = JsonSerializer.Deserialize<PluginManifest>(resultBytes, _options);

            Assert.NotNull(result);
            Assert.Equivalent(expectedManifest, result);
        }

        [Fact]
        public async Task PopulateManifest_ExistingMetafileMismatchedIds_Status_Malfunctioned()
        {
            var packageInfo = GenerateTestPackage();

            // Partial plugin without a name, but matching version and package ID
            var partial = new PluginManifest
            {
                Id = Guid.NewGuid(),
                Version = new Version(1, 0, 0).ToString()
            };

            var metafilePath = Path.Combine(_pluginPath, "meta.json");
            await File.WriteAllTextAsync(metafilePath, JsonSerializer.Serialize(partial, _options));

            var pluginManager = new PluginManager(new NullLogger<PluginManager>(), null!, null!, _tempPath, new Version(1, 0));

            await pluginManager.PopulateManifest(packageInfo, new Version(1, 0), _pluginPath, PluginStatus.Active);

            var resultBytes = await File.ReadAllBytesAsync(metafilePath);
            var result = JsonSerializer.Deserialize<PluginManifest>(resultBytes, _options);

            Assert.NotNull(result);
            Assert.Equal(packageInfo.Name, result.Name);
            Assert.Equal(PluginStatus.Malfunctioned, result.Status);
        }

        [Fact]
        public async Task PopulateManifest_ExistingMetafileMismatchedVersions_Updates_Version()
        {
            var packageInfo = GenerateTestPackage();

            var partial = new PluginManifest
            {
                Id = packageInfo.Id,
                Version = new Version(2, 0, 0).ToString()
            };

            var metafilePath = Path.Combine(_pluginPath, "meta.json");
            await File.WriteAllTextAsync(metafilePath, JsonSerializer.Serialize(partial, _options));

            var pluginManager = new PluginManager(new NullLogger<PluginManager>(), null!, null!, _tempPath, new Version(1, 0));

            await pluginManager.PopulateManifest(packageInfo, new Version(1, 0), _pluginPath, PluginStatus.Active);

            var resultBytes = await File.ReadAllBytesAsync(metafilePath);
            var result = JsonSerializer.Deserialize<PluginManifest>(resultBytes, _options);

            Assert.NotNull(result);
            Assert.Equal(packageInfo.Name, result.Name);
            Assert.Equal(PluginStatus.Active, result.Status);
            Assert.Equal(packageInfo.Versions[0].Version, result.Version);
        }

        private PackageInfo GenerateTestPackage()
        {
            var fixture = new Fixture();
            fixture.Customize<PackageInfo>(c => c.Without(x => x.Versions).Without(x => x.ImageUrl));
            fixture.Customize<VersionInfo>(c => c.Without(x => x.Version).Without(x => x.Timestamp));

            var versionInfo = fixture.Create<VersionInfo>();
            versionInfo.Version = new Version(1, 0).ToString();
            versionInfo.Timestamp = DateTime.UtcNow.ToString(CultureInfo.InvariantCulture);

            var packageInfo = fixture.Create<PackageInfo>();
            packageInfo.Versions = new[] { versionInfo };

            return packageInfo;
        }

        private JsonSerializerOptions GetTestSerializerOptions()
        {
            var options = new JsonSerializerOptions(JsonDefaults.Options)
            {
                WriteIndented = true
            };

            for (var i = 0; i < options.Converters.Count; i++)
            {
                // Remove the Guid converter for parity with plugin manager.
                if (options.Converters[i] is JsonGuidConverter converter)
                {
                    options.Converters.Remove(converter);
                }
            }

            return options;
        }

        private (string TempPath, string PluginPath) GetTestPaths(string pluginFolderName)
        {
            var tempPath = Path.Combine(_testPathRoot, "plugin-manager" + Path.GetRandomFileName());
            var pluginPath = Path.Combine(tempPath, pluginFolderName);

            return (tempPath, pluginPath);
        }
    }
}
