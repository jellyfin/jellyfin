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
        if (!typeof(IAsyncMigrationRoutine).IsAssignableFrom(MigrationType))
        {
            throw new InvalidOperationException($"The type {MigrationType} does not implement IAsyncMigrationRoutine and is not a valid migration type");
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
                await ((IAsyncMigrationRoutine)ActivatorUtilities.CreateInstance(scope.ServiceProvider, MigrationType)).PerformAsync(cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
