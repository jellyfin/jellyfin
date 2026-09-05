using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Server.ServerSetupApp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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

    public async Task Perform(IServiceProvider? serviceProvider, IStartupLogger logger, CancellationToken cancellationToken)
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
                var migrationServices = new MigrationServiceProvider(serviceProvider, logger);
                await using (migrationServices.ConfigureAwait(false))
                {
                    ((IMigrationRoutine)ActivatorUtilities.CreateInstance(migrationServices, MigrationType)).Perform();
                }
            }
#pragma warning restore CS0618 // Type or member is obsolete
        }
        else if (typeof(IAsyncMigrationRoutine).IsAssignableFrom(MigrationType))
        {
            if (serviceProvider is null)
            {
                await ((IAsyncMigrationRoutine)Activator.CreateInstance(MigrationType)!).PerformAsync(cancellationToken).ConfigureAwait(false);
            }
            else
            {
                var migrationServices = new MigrationServiceProvider(serviceProvider, logger);
                await using (migrationServices.ConfigureAwait(false))
                {
                    await ((IAsyncMigrationRoutine)ActivatorUtilities.CreateInstance(migrationServices, MigrationType)).PerformAsync(cancellationToken).ConfigureAwait(false);
                }
            }
        }
        else
        {
            throw new InvalidOperationException($"The type {MigrationType} does not implement either IMigrationRoutine or IAsyncMigrationRoutine and is not a valid migration type");
        }
    }

    /// <summary>
    /// Provides the services a migration routine is constructed with.
    /// </summary>
    /// <remarks>
    /// This overlays the migration scoped logging services onto a scope of the application container. Copying the
    /// application service descriptors into a child container instead would make that child container the owner of
    /// every singleton it forwards, so disposing it after the migration would also dispose the applications own
    /// instance of services like the <c>ProviderManager</c> and leave the server broken until the next restart.
    /// </remarks>
    private sealed class MigrationServiceProvider : IServiceProvider, IServiceProviderIsService, IAsyncDisposable
    {
        private readonly AsyncServiceScope _scope;
        private readonly IStartupLogger _logger;
        private readonly IServiceProviderIsService? _isService;

        public MigrationServiceProvider(IServiceProvider serviceProvider, IStartupLogger logger)
        {
            _scope = serviceProvider.CreateAsyncScope();
            _logger = logger;
            _isService = _scope.ServiceProvider.GetService<IServiceProviderIsService>();
        }

        public object? GetService(Type serviceType)
        {
            if (serviceType == typeof(IServiceProvider))
            {
                return this;
            }

            if (serviceType == typeof(IServiceProviderIsService))
            {
                return _isService is null ? null : this;
            }

            if (serviceType == typeof(IStartupLogger))
            {
                return _logger;
            }

            if (serviceType == typeof(StartupLogTopic))
            {
                return _logger.Topic;
            }

            if (IsCategoryLogger(serviceType))
            {
                var category = serviceType.GenericTypeArguments[0];
                var baseLogger = _scope.ServiceProvider.GetRequiredService(typeof(ILogger<>).MakeGenericType(category));
                return Activator.CreateInstance(typeof(NestedStartupLogger<>).MakeGenericType(category), baseLogger, _logger.Topic);
            }

            return _scope.ServiceProvider.GetService(serviceType);
        }

        public bool IsService(Type serviceType)
        {
            if (serviceType == typeof(IServiceProvider)
                || serviceType == typeof(IServiceProviderIsService)
                || serviceType == typeof(IStartupLogger)
                || serviceType == typeof(StartupLogTopic)
                || IsCategoryLogger(serviceType))
            {
                return true;
            }

            return _isService?.IsService(serviceType) ?? false;
        }

        public ValueTask DisposeAsync()
        {
            return _scope.DisposeAsync();
        }

        private static bool IsCategoryLogger(Type serviceType)
        {
            return serviceType.IsConstructedGenericType && serviceType.GetGenericTypeDefinition() == typeof(IStartupLogger<>);
        }
    }

    private class NestedStartupLogger<TCategory> : StartupLogger<TCategory>
    {
        public NestedStartupLogger(ILogger logger, StartupLogTopic? topic) : base(logger, topic)
        {
        }
    }
}
