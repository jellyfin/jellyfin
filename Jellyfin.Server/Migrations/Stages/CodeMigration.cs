using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Server.Migrations.Stages;

internal class CodeMigration(Type migrationType, JellyfinMigrationAttribute metadata)
{
    public Type MigrationType { get; } = migrationType;

    public JellyfinMigrationAttribute Metadata { get; } = metadata;

    public string BuildCodeMigrationId()
    {
        return Metadata.Order.ToString("yyyyMMddHHmmsss", CultureInfo.InvariantCulture) + "_" + MigrationType.Name!;
    }

    public async Task Perform(IServiceProvider? serviceProvider, CancellationToken cancellationToken)
    {
#pragma warning disable CS0618 // Type or member is obsolete
        if (typeof(IMigrationRoutine).IsAssignableFrom(MigrationType))
        {
            if (serviceProvider is null)
            {
                ((IMigrationRoutine)Activator.CreateInstance(MigrationType)!).Perform();
            }
            else
            {
                ((IMigrationRoutine)ActivatorUtilities.CreateInstance(serviceProvider, MigrationType)).Perform();
#pragma warning restore CS0618 // Type or member is obsolete
            }
        }
        else if (typeof(IAsyncMigrationRoutine).IsAssignableFrom(MigrationType))
        {
            if (serviceProvider is null)
            {
                await ((IAsyncMigrationRoutine)Activator.CreateInstance(MigrationType)!).PerformAsync(cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await ((IAsyncMigrationRoutine)ActivatorUtilities.CreateInstance(serviceProvider, MigrationType)).PerformAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        else
        {
            throw new InvalidOperationException($"The type {MigrationType} does not implement either IMigrationRoutine or IAsyncMigrationRoutine and is not a valid migration type");
        }
    }
}
