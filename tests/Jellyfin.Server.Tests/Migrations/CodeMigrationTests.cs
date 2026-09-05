using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Server.Migrations;
using Jellyfin.Server.Migrations.Stages;
using Jellyfin.Server.ServerSetupApp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Server.Tests.Migrations;

public class CodeMigrationTests
{
    [Fact]
    public async Task Perform_LeavesApplicationSingletonsAlive()
    {
        var services = new ServiceCollection()
            .AddLogging()
            .RegisterStartupLogger()
            .AddSingleton<ApplicationSingleton>()
            .AddTransient<MigrationTransient>();
        services.AddSingleton(services);

        await using var serviceProvider = services.BuildServiceProvider();
        var applicationSingleton = serviceProvider.GetRequiredService<ApplicationSingleton>();
        var logger = new StartupLogger(NullLogger.Instance).BeginGroup($"Test migration");

        var migration = new CodeMigration(
            typeof(TestMigration),
            new JellyfinMigrationAttribute("2026-09-05T10:00:00", nameof(TestMigration)),
            null);
        await migration.Perform(serviceProvider, logger, CancellationToken.None);

        var performed = TestMigration.Performed;
        Assert.NotNull(performed);
        // The migration has to run against the applications own services, and they have to outlive it.
        Assert.Same(applicationSingleton, performed.Singleton);
        Assert.False(applicationSingleton.IsDisposed);
        Assert.Same(applicationSingleton, serviceProvider.GetRequiredService<ApplicationSingleton>());
        // Services created for the migration itself are still owned by the migration.
        Assert.True(performed.Transient.IsDisposed);
        // The startup logger has to stay attached to the topic of the running migration.
        Assert.Same(logger.Topic, performed.Logger.Topic);
    }

    private sealed class ApplicationSingleton : IDisposable
    {
        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            IsDisposed = true;
        }
    }

    private sealed class MigrationTransient : IDisposable
    {
        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            IsDisposed = true;
        }
    }

    private sealed class TestMigration : IAsyncMigrationRoutine
    {
        public TestMigration(ApplicationSingleton singleton, MigrationTransient transient, IStartupLogger<TestMigration> logger)
        {
            Singleton = singleton;
            Transient = transient;
            Logger = logger;
        }

        public static TestMigration? Performed { get; private set; }

        public ApplicationSingleton Singleton { get; }

        public MigrationTransient Transient { get; }

        public IStartupLogger<TestMigration> Logger { get; }

        public Task PerformAsync(CancellationToken cancellationToken)
        {
            Performed = this;
            return Task.CompletedTask;
        }
    }
}
