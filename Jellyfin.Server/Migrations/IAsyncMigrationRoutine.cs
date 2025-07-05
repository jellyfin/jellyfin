using System;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Server.Migrations;

/// <summary>
/// Interface that describes a migration routine.
/// </summary>
internal interface IAsyncMigrationRoutine
{
    /// <summary>
    /// Execute the migration routine.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token triggered if the migration should be aborted.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public Task PerformAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Interface that describes a migration routine.
/// </summary>
[Obsolete("Use IAsyncMigrationRoutine instead")]
internal interface IMigrationRoutine
{
    /// <summary>
    /// Execute the migration routine.
    /// </summary>
    [Obsolete("Use IAsyncMigrationRoutine.PerformAsync instead")]
    public void Perform();
}
