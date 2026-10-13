using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Server.Implementations.FullSystemBackup;
using Jellyfin.Server.Migrations.Stages;

namespace Jellyfin.Server.Migrations;

/// <summary>
/// Names the code migration routines that are part of this assembly.
/// </summary>
internal sealed class MigrationRoutineCatalog : IMigrationRoutineCatalog
{
    /// <inheritdoc/>
    public IReadOnlySet<string> RoutineIds { get; } = MigrationRoutineDiscovery.Find()
        .Select(routine => new CodeMigration(routine.Type, routine.Metadata, null).BuildCodeMigrationId())
        .ToHashSet(StringComparer.Ordinal);
}
