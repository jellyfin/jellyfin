using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Jellyfin.Database.Implementations.DesignTime;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.EfMigrations;

public sealed class PairedMigrationsDesignTimeServicesTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("design-time-services").FullName;

    public static TheoryData<string> MirroringProviderNames() => new(MigrationProvider.All.Where(e => !e.IsReference).Select(e => e.Name));

    [Theory]
    [MemberData(nameof(MirroringProviderNames))]
    [Trait("Category", "Migrations")]
    public void DesignTimeServices_OfAMirroringProvider_UseTheSharedGenerator(string providerName)
    {
        var generator = CreateIdGenerator(MigrationProvider.Get(providerName));

        Assert.IsType<PairedMigrationsIdGenerator>(generator);
    }

    [Theory]
    [MemberData(nameof(MirroringProviderNames))]
    [Trait("Category", "Migrations")]
    public void DesignTimeServices_OfAMirroringProvider_ReuseTheIdOfTheSqliteMigrationWithTheName(string providerName)
    {
        var generator = CreateIdGenerator(MigrationProvider.Get(providerName));
        var sqliteIds = MigrationProvider.Reference.MigrationIds();

        Assert.All(sqliteIds.TakeLast(3), id => Assert.Equal(id, generator.GenerateId(generator.GetName(id))));
    }

    [Theory]
    [MemberData(nameof(MirroringProviderNames))]
    [Trait("Category", "Migrations")]
    public void DesignTimeServices_OfAMirroringProvider_GiveANameWithoutASqliteMigrationAFreshId(string providerName)
    {
        var generator = CreateIdGenerator(MigrationProvider.Get(providerName));
        var sqliteIds = MigrationProvider.Reference.MigrationIds();

        var id = generator.GenerateId("NameThatNoSqliteMigrationHas");

        Assert.True(generator.IsValidId(id), id);
        Assert.Equal("NameThatNoSqliteMigrationHas", generator.GetName(id));
        Assert.DoesNotContain(id, sqliteIds);
        Assert.True(string.CompareOrdinal(id, sqliteIds[^1]) > 0, id + " sorts before " + sqliteIds[^1]);
    }

    [Theory]
    [InlineData("Jellyfin.Database.Providers.Postgres")]
    [InlineData("Jellyfin.Database.Providers.MySql")]
    [InlineData("Jellyfin.Database.Providers.SqlServer")]
    public void FindSqliteMigrations_FromTheOutputDirectoryOfAProvider_FindsTheDirectory(string providerProject)
    {
        var expected = CreateSqliteMigrations();
        var output = Directory.CreateDirectory(Path.Combine(_root, "src", "Jellyfin.Database", providerProject, "bin", "Debug", "net10.0"));

        Assert.Equal(expected, PairedMigrationsDesignTimeServices.FindSqliteMigrations(output.FullName));
    }

    [Fact]
    public void FindSqliteMigrations_FromTheOutputDirectoryOfAnotherProject_FindsTheDirectory()
    {
        var expected = CreateSqliteMigrations();
        var output = Directory.CreateDirectory(Path.Combine(_root, "Jellyfin.Server", "bin", "Debug", "net10.0"));

        Assert.Equal(expected, PairedMigrationsDesignTimeServices.FindSqliteMigrations(output.FullName));
    }

    [Fact]
    public void FindSqliteMigrations_WithoutTheSqliteProject_ReturnsNull()
    {
        var output = Directory.CreateDirectory(Path.Combine(_root, "elsewhere", "bin"));

        Assert.Null(PairedMigrationsDesignTimeServices.FindSqliteMigrations(output.FullName));
    }

    [Fact]
    public void FindSqliteMigrations_FromTheTestOutput_FindsTheMigrationsOfTheRepository()
    {
        var directory = PairedMigrationsDesignTimeServices.FindSqliteMigrations(AppContext.BaseDirectory);

        Assert.NotNull(directory);
        Assert.True(File.Exists(Path.Combine(directory, "JellyfinDbModelSnapshot.cs")), directory);
    }

    public void Dispose()
    {
        Directory.Delete(_root, recursive: true);
    }

    /// <summary>
    /// Builds the id generator the way the design-time tools do: through the <see cref="DesignTimeServicesReferenceAttribute"/>
    /// of the migrations assembly of the provider, with scoped services validated.
    /// </summary>
    private static IMigrationsIdGenerator CreateIdGenerator(MigrationProvider provider)
    {
        Assembly assembly;
        using (var context = provider.CreateContext())
        {
            assembly = context.GetService<IMigrationsAssembly>().Assembly;
        }

        var reference = Assert.Single(assembly.GetCustomAttributes<DesignTimeServicesReferenceAttribute>());
        var designTimeServices = Assert.IsAssignableFrom<IDesignTimeServices>(Activator.CreateInstance(Type.GetType(reference.TypeName, throwOnError: true)!));

        // EF Core registers its logger factory as a scoped service.
        var services = new ServiceCollection();
        services.AddScoped<ILoggerFactory>(_ => NullLoggerFactory.Instance);
        designTimeServices.ConfigureDesignTimeServices(services);

        // The generator keeps nothing of the scope, so it outlives it.
        using var serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        using var scope = serviceProvider.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IMigrationsIdGenerator>();
    }

    private string CreateSqliteMigrations()
    {
        return Directory.CreateDirectory(Path.Combine(_root, "src", "Jellyfin.Database", "Jellyfin.Database.Providers.Sqlite", "Migrations")).FullName;
    }
}
