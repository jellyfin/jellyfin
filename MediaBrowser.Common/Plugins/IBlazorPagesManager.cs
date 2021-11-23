using System.Collections.Generic;
using System.Reflection;

namespace MediaBrowser.Common.Plugins;

/// <summary>
/// Blazor pages manager.
/// </summary>
public interface IBlazorPagesManager
{
    /// <summary>
    /// Sets the list of assemblies that have blazor pages.
    /// </summary>
    /// <param name="assemblies">The list of assemblies.</param>
    void SetAssemblies(IEnumerable<Assembly> assemblies);

    /// <summary>
    /// Gets the array of assemblies that have blazor pages.
    /// </summary>
    /// <returns>The array of assemblies.</returns>
    IReadOnlyCollection<Assembly> GetAssemblies();
}
