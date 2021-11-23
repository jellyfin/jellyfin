using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MediaBrowser.Common.Plugins;

namespace Emby.Server.Implementations.Plugins;

/// <inheritdoc />
public class BlazorPagesManager : IBlazorPagesManager
{
    private Assembly[]? _assemblies;

    /// <inheritdoc />
    public void SetAssemblies(IEnumerable<Assembly> assemblies)
        => _assemblies = assemblies.ToArray();

    /// <inheritdoc />
    public IReadOnlyCollection<Assembly> GetAssemblies()
        => _assemblies ?? Array.Empty<Assembly>();
}
