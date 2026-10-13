using System.Collections.Generic;

namespace Jellyfin.Server.Implementations.FullSystemBackup;

/// <summary>
/// Names the code migration routines of the running server.
/// </summary>
public interface IMigrationRoutineCatalog
{
    /// <summary>
    /// Gets the ids under which the routines are recorded in the migration history table.
    /// </summary>
    IReadOnlySet<string> RoutineIds { get; }
}
