using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

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

    public virtual async Task ExecuteMigrationsAsync(IEnumerable<string> appliedMigrations, IServiceProvider? serviceProvider)
    {

    }
}

internal class CoreMigrationStage() : MigrationStage(JellyfinMigrationStageTypes.CoreInitialisaition)
{
    public override Task ExecuteMigrationsAsync(IEnumerable<string> appliedMigrations)
    {

    }
}

internal record CodeMigration(string Order, Type MigrationType);
