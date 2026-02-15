using System.Collections.ObjectModel;

namespace Jellyfin.Server.Migrations.Stages;

/// <summary>
/// Defines a Stage that can be Invoked and Handled at different times from the code.
/// </summary>
internal class MigrationStage : Collection<CodeMigration>
{
    public MigrationStage(JellyfinMigrationStageTypes stage)
    {
        Stage = stage;
    }

    public JellyfinMigrationStageTypes Stage { get; }
}
