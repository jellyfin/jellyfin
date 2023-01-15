using System.Reflection;
using System.Runtime.Loader;

namespace Emby.Server.Implementations.Plugins;

/// <summary>
/// A custom <see cref="AssemblyLoadContext"/> for loading Jellyfin plugins.
/// </summary>
public class PluginLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginLoadContext"/> class.
    /// </summary>
    /// <param name="path">The path of the plugin assembly.</param>
    public PluginLoadContext(string path) : base(true)
    {
        _resolver = new AssemblyDependencyResolver(path);
    }

    /// <inheritdoc />
    protected override Assembly? Load(AssemblyName assemblyName)
    {
        var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        if (assemblyPath is not null)
        {
            return LoadFromAssemblyPath(assemblyPath);
        }

        return null;
    }
}
