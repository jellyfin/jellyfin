using System.Collections.Generic;
using System.Reflection;

namespace Emby.Server.Implementations.Migrations;

/// <summary>
/// Provides assemblies that should be scanned for migration routines.
/// </summary>
internal interface IMigrationAssemblyProvider
{
    /// <summary>
    /// Gets the list of assemblies to scan for migrations.
    /// </summary>
    /// <returns>An enumerable of assemblies that may contain migration types.</returns>
    IEnumerable<Assembly> GetMigrationAssemblies();
}
