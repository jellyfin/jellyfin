using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using MediaBrowser.Common.Plugins;

namespace Emby.Server.Implementations.Migrations;

/// <summary>
/// Provides assemblies that should be scanned for migration routines.
/// Includes Jellyfin.Server and all loaded plugin assemblies.
/// </summary>
internal class MigrationAssemblyProvider : IMigrationAssemblyProvider
{
    private readonly IPluginManager? _pluginManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="MigrationAssemblyProvider"/> class.
    /// </summary>
    /// <param name="pluginManager">Optional plugin manager for discovering plugin assemblies.</param>
    public MigrationAssemblyProvider(IPluginManager? pluginManager = null)
    {
        _pluginManager = pluginManager;
    }

    /// <inheritdoc/>
    public IEnumerable<Assembly> GetMigrationAssemblies()
    {
        var assemblies = new HashSet<Assembly>(AssemblyEqualityComparer.Instance)
        {
            typeof(IAsyncMigrationRoutine).Assembly, // Emby.Server.Implementations
            typeof(MigrationAssemblyProvider).Assembly // Jellyfin.Server
        };

        if (_pluginManager is not null)
        {
            foreach (var plugin in _pluginManager.Plugins)
            {
                if (!plugin.IsEnabledAndSupported)
                {
                    continue;
                }

                foreach (var dllFile in plugin.DllFiles)
                {
                    var dllPath = Path.IsPathRooted(dllFile)
                        ? dllFile
                        : Path.Combine(Path.GetDirectoryName(plugin.Path)!, dllFile);

                    if (!File.Exists(dllPath))
                    {
                        continue;
                    }

                    try
                    {
                        var assembly = Assembly.LoadFrom(dllPath);
                        assemblies.Add(assembly);
                    }
                    catch (ReflectionTypeLoadException)
                    {
                        // Skip assemblies that fail to load completely
                    }
                    catch (FileLoadException)
                    {
                        // Already loaded or incompatible
                    }
                    catch (IOException)
                    {
                        // Skip assemblies that can't be read
                    }
                }
            }
        }

        return assemblies;
    }

    private sealed class AssemblyEqualityComparer : IEqualityComparer<Assembly>
    {
        public static readonly AssemblyEqualityComparer Instance = new();

        public bool Equals(Assembly? x, Assembly? y)
        {
            if (x is null || y is null)
            {
                return x is null && y is null;
            }

            return x.GetName().FullName == y.GetName().FullName;
        }

        public int GetHashCode(Assembly obj)
        {
            return obj?.GetName().FullName?.GetHashCode() ?? 0;
        }
    }
}
