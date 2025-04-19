using System;
using System.Globalization;
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

    public IMigrationRoutine Construct(IServiceProvider? serviceProvider)
    {
        if (serviceProvider is null)
        {
            return (IMigrationRoutine)Activator.CreateInstance(MigrationType)!;
        }
        else
        {
            return (IMigrationRoutine)ActivatorUtilities.CreateInstance(serviceProvider, MigrationType);
        }
    }
}
