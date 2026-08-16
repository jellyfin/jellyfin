using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.Migrations;
using Emby.Server.Implementations.Migrations.Stages;
using Jellyfin.Server.ServerSetupApp;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Server.Migrations;

/// <summary>
/// Represents a single code migration routine discovered from attributes.
/// </summary>
internal sealed class CodeMigration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CodeMigration"/> class.
    /// </summary>
    /// <param name="migrationType">The migration class type.</param>
    /// <param name="metadata">The migration attribute metadata.</param>
    /// <param name="backupMetadata">Optional backup attribute metadata.</param>
    public CodeMigration(Type migrationType, JellyfinMigrationAttribute metadata, JellyfinMigrationBackupAttribute? backupMetadata)
    {
        MigrationType = migrationType;
        Metadata = metadata;
        BackupRequirements = backupMetadata;
    }

    /// <summary>
    /// Gets the migration class type.
    /// </summary>
    public Type MigrationType { get; }

    /// <summary>
    /// Gets the migration attribute metadata.
    /// </summary>
    public JellyfinMigrationAttribute Metadata { get; }

    /// <summary>
    /// Gets the optional backup requirements for this migration.
    /// </summary>
    public JellyfinMigrationBackupAttribute? BackupRequirements { get; }

    /// <summary>
    /// Builds a unique migration ID combining the stage and migration order.
    /// </summary>
    /// <returns>The unique migration ID string.</returns>
    public string BuildCodeMigrationId()
    {
        return $"Code_{Metadata.Order:yyyyMMddHHmmss}_{Metadata.Name}";
    }

    /// <summary>
    /// Performs the migration with the given service provider, logger, and cancellation token.
    /// </summary>
    /// <param name="serviceProvider">Service provider for resolving dependencies.</param>
    /// <param name="logger">Logger for recording migration activity.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task Perform(IServiceProvider? serviceProvider, IStartupLogger logger, CancellationToken cancellationToken)
    {
        if (typeof(IAsyncMigrationRoutine).IsAssignableFrom(MigrationType))
        {
            var instance = ActivatorUtilities.CreateInstance(serviceProvider!, MigrationType);
            if (instance is IAsyncMigrationRoutine asyncRoutine)
            {
                await asyncRoutine.PerformAsync(cancellationToken).ConfigureAwait(false);
                return;
            }
        }

#pragma warning disable CS0618
        if (typeof(IMigrationRoutine).IsAssignableFrom(MigrationType))
        {
            var instance = ActivatorUtilities.CreateInstance(serviceProvider!, MigrationType);
            if (instance is IMigrationRoutine syncRoutine)
            {
#pragma warning disable CS0618 // Type or member is obsolete
                syncRoutine.Perform();
#pragma warning restore CS0618
#pragma warning restore CS0618 // Type or member is obsolete
                return;
            }
        }

        throw new InvalidOperationException($"Migration type {MigrationType.FullName} does not implement IAsyncMigrationRoutine or IMigrationRoutine.");
    }
}

/// <summary>
/// Groups code migrations by their migration stage.
/// Implements <see cref="IEnumerable{CodeMigration}"/> and <see cref="ICollection{CodeMigration}"/>.
/// </summary>
internal sealed class MigrationStage : ICollection<CodeMigration>
{
    private readonly List<CodeMigration> _migrations = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="MigrationStage"/> class.
    /// </summary>
    /// <param name="stage">The migration stage type.</param>
    public MigrationStage(JellyfinMigrationStageTypes stage)
    {
        Stage = stage;
    }

    /// <summary>
    /// Gets the migration stage type.
    /// </summary>
    public JellyfinMigrationStageTypes Stage { get; }

    /// <inheritdoc />
    public IEnumerator<CodeMigration> GetEnumerator() => _migrations.GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => _migrations.GetEnumerator();

    /// <inheritdoc />
    public void Add(CodeMigration migration) => _migrations.Add(migration);

    /// <inheritdoc />
    public void Clear() => _migrations.Clear();

    /// <inheritdoc />
    public bool Contains(CodeMigration item) => _migrations.Contains(item);

    /// <inheritdoc />
    public void CopyTo(CodeMigration[] array, int arrayIndex) => _migrations.CopyTo(array, arrayIndex);

    /// <inheritdoc />
    public bool Remove(CodeMigration item) => _migrations.Remove(item);

    /// <inheritdoc />
    public int Count => _migrations.Count;

    /// <inheritdoc />
    public bool IsReadOnly => false;
}
