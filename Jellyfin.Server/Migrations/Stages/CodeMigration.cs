using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Server.ServerSetupApp;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Server.Migrations.Stages;

internal class CodeMigration(Type migrationType, JellyfinMigrationAttribute metadata, JellyfinMigrationBackupAttribute? migrationBackupAttribute)
{
    public Type MigrationType { get; } = migrationType;

    public JellyfinMigrationAttribute Metadata { get; } = metadata;

    public JellyfinMigrationBackupAttribute? BackupRequirements { get; set; } = migrationBackupAttribute;

    public string BuildCodeMigrationId()
    {
        return Metadata.Order.ToString("yyyyMMddHHmmsss", CultureInfo.InvariantCulture) + "_" + Metadata.Name!;
    }

    public async Task Perform(IServiceProvider serviceProvider, IStartupLogger logger, CancellationToken cancellationToken)
    {
        if (!IsMigrationRoutine(MigrationType))
        {
            throw new InvalidOperationException($"The type {MigrationType} does not implement either IMigrationRoutine or IAsyncMigrationRoutine and is not a valid migration type");
        }

        // The routine runs against a scope of the applications own container. Copying the application service
        // descriptors into a child container instead would make that child container the owner of every singleton it
        // forwards, so disposing it after the migration would also dispose the applications own instance of services
        // like the ProviderManager and leave the server broken until the next restart.
        var scope = serviceProvider.CreateAsyncScope();
        await using (scope.ConfigureAwait(false))
        {
            // Nests everything the routine logs through an injected IStartupLogger under the migrations own topic.
            using (StartupLogger.BeginAmbientTopic(logger.Topic))
            {
                await RunAsync(ActivatorUtilities.CreateInstance(scope.ServiceProvider, MigrationType), cancellationToken).ConfigureAwait(false);
            }
        }
    }

    // The obsolete IMigrationRoutine is still implemented by every routine that predates the async interface, so
    // the members that have to touch it are grouped here behind a single suppression.
#pragma warning disable CS0618 // Type or member is obsolete
    private static bool IsMigrationRoutine(Type migrationType)
    {
        return typeof(IMigrationRoutine).IsAssignableFrom(migrationType) || typeof(IAsyncMigrationRoutine).IsAssignableFrom(migrationType);
    }

    private static async Task RunAsync(object routine, CancellationToken cancellationToken)
    {
        if (routine is IMigrationRoutine migrationRoutine)
        {
            migrationRoutine.Perform();
            return;
        }

        await ((IAsyncMigrationRoutine)routine).PerformAsync(cancellationToken).ConfigureAwait(false);
    }
#pragma warning restore CS0618 // Type or member is obsolete
}
