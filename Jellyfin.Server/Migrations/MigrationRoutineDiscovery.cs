using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Jellyfin.Server.Migrations;

/// <summary>
/// Finds the code migration routines that are part of this assembly.
/// </summary>
internal static class MigrationRoutineDiscovery
{
    /// <summary>
    /// Finds every routine that is annotated with a <see cref="JellyfinMigrationAttribute"/>.
    /// </summary>
    /// <returns>The routine types with their metadata.</returns>
    public static IEnumerable<(Type Type, JellyfinMigrationAttribute Metadata)> Find()
    {
#pragma warning disable CS0618 // Type or member is obsolete
        return typeof(IAsyncMigrationRoutine).Assembly.GetTypes()
            .Where(e => typeof(IMigrationRoutine).IsAssignableFrom(e) || typeof(IAsyncMigrationRoutine).IsAssignableFrom(e))
            .Select(e => (Type: e, Metadata: e.GetCustomAttribute<JellyfinMigrationAttribute>()))
            .Where(e => e.Metadata is not null)
            .Select(e => (e.Type, e.Metadata!));
#pragma warning restore CS0618 // Type or member is obsolete
    }
}
