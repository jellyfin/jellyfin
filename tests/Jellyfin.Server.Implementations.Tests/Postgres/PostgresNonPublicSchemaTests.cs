using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.DbConfiguration;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Database.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Postgres;

/// <summary>
/// The baseline creates its tables without naming a schema, so they go to the first schema of the search path. These
/// tests migrate a database whose search path starts with a schema other than <c>public</c>, and expect everything the
/// baseline creates, including the objects that are written by hand, to follow the tables.
/// </summary>
public sealed class PostgresNonPublicSchemaTests : IDisposable
{
    private const string Schema = "jf";

    private readonly TestDatabase _database = PostgresTestBackend.Instance.CreateEmpty();

    public void Dispose()
    {
        _database.Dispose();
    }

    [ProviderFact(TestDbProvider.Postgres)]
    public async Task Migrate_SearchPathStartsWithAnotherSchema_PutsEverythingInThatSchema()
    {
        await using var context = CreateMigratedContext();

        Assert.Equal([Schema], await QueryAsync(context, """SELECT table_schema AS "Value" FROM information_schema.tables WHERE table_name IN ('BaseItems', '__EFMigrationsHistory') GROUP BY table_schema"""));
        Assert.Equal([Schema], await QueryAsync(context, """SELECT pronamespace::regnamespace::text AS "Value" FROM pg_proc WHERE proname = 'jf_uuid_smaller'"""));
        Assert.Equal([Schema], await QueryAsync(context, """SELECT pronamespace::regnamespace::text AS "Value" FROM pg_proc WHERE proname = 'min' AND proargtypes::text = 'uuid'::regtype::oid::text"""));
        Assert.Equal([Schema], await QueryAsync(context, """SELECT schemaname AS "Value" FROM pg_indexes WHERE indexname = 'IX_Peoples_NameLower'"""));
        Assert.Empty(await QueryAsync(context, """SELECT c.relname AS "Value" FROM pg_class c WHERE c.relnamespace = 'public'::regnamespace AND c.relkind = 'r'"""));
        Assert.Empty(await QueryAsync(context, """SELECT proname AS "Value" FROM pg_proc WHERE pronamespace = 'public'::regnamespace AND proname IN ('jf_uuid_smaller', 'min')"""));
    }

    [ProviderFact(TestDbProvider.Postgres)]
    public async Task Migrate_SearchPathStartsWithAnotherSchema_GroupingByLowestIdWorks()
    {
        await using var context = CreateMigratedContext();
        var ids = new[]
        {
            ("a", "0b000000-0000-0000-0000-000000000003"),
            ("a", "0a000000-0000-0000-0000-000000000002"),
            ("b", "f0000000-0000-0000-0000-000000000004"),
            ("b", "e0000000-0000-0000-0000-000000000005")
        };
        context.BaseItems.AddRange(ids.Select(e => new BaseItemEntity { Id = Guid.Parse(e.Item2), Type = "Movie", PresentationUniqueKey = e.Item1 }));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var lowest = await context.BaseItems
            .Where(e => e.PresentationUniqueKey != null)
            .GroupBy(e => e.PresentationUniqueKey!)
            .Select(g => new { g.Key, Id = g.Min(e => e.Id) })
            .ToDictionaryAsync(e => e.Key, e => e.Id, TestContext.Current.CancellationToken);

        Assert.Equal(Guid.Parse("0a000000-0000-0000-0000-000000000002"), lowest["a"]);
        Assert.Equal(Guid.Parse("e0000000-0000-0000-0000-000000000005"), lowest["b"]);
    }

    [ProviderFact(TestDbProvider.Postgres)]
    public async Task MigrateDown_SearchPathStartsWithAnotherSchema_RemovesTheObjectsOfThatSchema()
    {
        await using var context = CreateMigratedContext();

        await context.GetService<IMigrator>().MigrateAsync("0", TestContext.Current.CancellationToken);

        Assert.Empty(await QueryAsync(context, """SELECT proname AS "Value" FROM pg_proc WHERE pronamespace = 'jf'::regnamespace"""));
        Assert.Empty(await QueryAsync(context, """SELECT relname AS "Value" FROM pg_class WHERE relnamespace = 'jf'::regnamespace AND relkind = 'r' AND relname <> '__EFMigrationsHistory'"""));
    }

    private static async Task<List<string>> QueryAsync(DbContext context, string sql)
    {
        return await context.Database.SqlQueryRaw<string>(sql).ToListAsync(TestContext.Current.CancellationToken);
    }

    private JellyfinDbContext CreateMigratedContext()
    {
        var baseConnectionString = RelationalOptionsExtension.Extract(_database.Options).ConnectionString!;
        using (var connection = new NpgsqlConnection(new NpgsqlConnectionStringBuilder(baseConnectionString) { Pooling = false }.ConnectionString))
        {
            connection.Open();
            using var command = new NpgsqlCommand($"CREATE SCHEMA {Schema}", connection);
            command.ExecuteNonQuery();
        }

        var builder = new DbContextOptionsBuilder<JellyfinDbContext>();
        var provider = _database.CreateProvider();
        provider.Initialise(
            builder,
            new DatabaseConfigurationOptions
            {
                DatabaseType = "Jellyfin-PostgreSQL",
                CustomProviderOptions = new CustomDatabaseOptions
                {
                    PluginName = string.Empty,
                    PluginAssembly = string.Empty,
                    ConnectionString = new NpgsqlConnectionStringBuilder(baseConnectionString) { SearchPath = Schema }.ConnectionString
                }
            });

        var context = new JellyfinDbContext(builder.Options, NullLogger<JellyfinDbContext>.Instance, provider, new NoLockBehavior(NullLogger<NoLockBehavior>.Instance));
        context.Database.Migrate();
        return context;
    }
}
