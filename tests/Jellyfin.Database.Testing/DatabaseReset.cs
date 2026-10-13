using System;
using System.Linq;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Locking;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jellyfin.Database.Testing;

/// <summary>
/// Returns a database to the state that building its schema leaves it in.
/// </summary>
internal static class DatabaseReset
{
    public static void Run(DbContextOptions<JellyfinDbContext> options, IJellyfinDatabaseProvider provider)
    {
        using var context = new JellyfinDbContext(
            options,
            NullLogger<JellyfinDbContext>.Instance,
            provider,
            new NoLockBehavior(NullLogger<NoLockBehavior>.Instance));

        // The migration history is not part of the model, so purging the tables of the model keeps it. The provider
        // takes table names without a schema, so the tables are assumed to be in the default schema.
        var tables = context.Model.GetEntityTypes()
            .Select(entityType => entityType.GetTableName())
            .OfType<string>()
            .Distinct()
            .ToList();
        provider.PurgeDatabase(context, tables).GetAwaiter().GetResult();

        InsertSeedData(context);
    }

    private static void InsertSeedData(JellyfinDbContext context)
    {
        // The model that is compiled for the runtime leaves the seed data out.
        foreach (var entityType in context.GetService<IDesignTimeModel>().Model.GetEntityTypes())
        {
            foreach (var values in entityType.GetSeedData())
            {
                var entry = context.Entry(Activator.CreateInstance(entityType.ClrType, nonPublic: true)!);
                foreach (var (name, value) in values)
                {
                    // Seeding an entity also records its navigations, which hold no value to insert.
                    if (entityType.FindProperty(name) is not null)
                    {
                        entry.Property(name).CurrentValue = value;
                    }
                }

                entry.State = EntityState.Added;
            }
        }

        context.SaveChanges();
    }
}
