using System;
using System.IO;
using System.Text.Json;
using Emby.Server.Implementations.Library;
using Emby.Server.Implementations.Plugins;
using Jellyfin.Extensions.Json;
using Jellyfin.Extensions.Json.Converters;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Plugins
{
    public class PluginManagerTests
    {
        private static readonly string _testPathRoot = Path.Combine(Path.GetTempPath(), "jellyfin-test-data");

        [Fact]
        public void SaveManifest_RoundTrip_Success()
        {
            var pluginManager = new PluginManager(new NullLogger<PluginManager>(), null!, null!, null!, new Version(1, 0));
            var manifest = new PluginManifest()
            {
                Version = "1.0"
            };

            var tempPath = Path.Combine(_testPathRoot, "manifest-" + Path.GetRandomFileName());
            Directory.CreateDirectory(tempPath);

            Assert.True(pluginManager.SaveManifest(manifest, tempPath));

            var res = pluginManager.LoadManifest(tempPath);

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
            var (tempPath, pluginPath) = GetTestPaths("safe");

            Directory.CreateDirectory(Path.Combine(pluginPath, dllFile.Replace(filename, string.Empty, StringComparison.OrdinalIgnoreCase)));
            File.Create(Path.Combine(pluginPath, dllFile));

            var options = GetTestSerializerOptions();
            var data = JsonSerializer.Serialize(manifest, options);
            var metafilePath = Path.Combine(tempPath, "safe", "meta.json");

            File.WriteAllText(metafilePath, data);

            var pluginManager = new PluginManager(new NullLogger<PluginManager>(), null!, null!, tempPath, new Version(1, 0));

            var res = JsonSerializer.Deserialize<PluginManifest>(File.ReadAllText(metafilePath), options);

            var expectedFullPath = Path.Combine(pluginPath, dllFile).Canonicalize();

            Assert.NotNull(res);
            Assert.NotEmpty(pluginManager.Plugins);
            Assert.Equal(PluginStatus.Active, res!.Status);
            Assert.Equal(expectedFullPath, pluginManager.Plugins[0].DllFiles[0]);
            Assert.StartsWith(Path.Combine(tempPath, "safe"), expectedFullPath, StringComparison.InvariantCulture);
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
        [InlineData("..\\.\\..\\.\\..\\some.dll")] // Windows traversal with current and parent
        [InlineData("\\\\network\\resource.dll")] // UNC Path
        [InlineData("https://jellyfin.org/some.dll")] // URL
        [InlineData("....//....//some.dll")] // Path replacement risk if a single "../" replacement occurs.
        [InlineData("~/some.dll")] // Tilde poses a shell expansion risk, but is a valid path character.
        public void Constructor_DiscoversUnsafePluginAssembly_Status_Malfunctioned(string unsafePath)
        {
            var manifest = new PluginManifest
            {
                Id = Guid.NewGuid(),
                Name = "Unsafe Assembly",
                Assemblies = new string[] { unsafePath }
            };

            var (tempPath, pluginPath) = GetTestPaths("unsafe");

            Directory.CreateDirectory(pluginPath);

            var files = new string[]
            {
                "../other.dll",
                "some.dll"
            };

            foreach (var file in files)
            {
                File.Create(Path.Combine(pluginPath, file));
            }

            var options = GetTestSerializerOptions();
            var data = JsonSerializer.Serialize(manifest, options);
            var metafilePath = Path.Combine(tempPath, "unsafe", "meta.json");

            File.WriteAllText(metafilePath, data);

            var pluginManager = new PluginManager(new NullLogger<PluginManager>(), null!, null!, tempPath, new Version(1, 0));

            var res = JsonSerializer.Deserialize<PluginManifest>(File.ReadAllText(metafilePath), options);

            Assert.NotNull(res);
            Assert.Empty(pluginManager.Plugins);
            Assert.Equal(PluginStatus.Malfunctioned, res!.Status);
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
            var tempPath = Path.Combine(_testPathRoot, "plugins-" + Path.GetRandomFileName());
            var pluginPath = Path.Combine(tempPath, pluginFolderName);

            return (tempPath, pluginPath);
        }
    }
}
