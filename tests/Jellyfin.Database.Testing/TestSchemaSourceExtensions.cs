using System;
using System.Linq;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Locking;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Database.Testing;

internal static class TestSchemaSourceExtensions
{
    public static void Apply(this TestSchemaSource source, DbContextOptions<JellyfinDbContext> options, IJellyfinDatabaseProvider provider)
    {
        using var context = new JellyfinDbContext(
            options,
            NullLogger<JellyfinDbContext>.Instance,
            provider,
            new NoLockBehavior(NullLogger<NoLockBehavior>.Instance));

        switch (source)
        {
            case TestSchemaSource.Model:
                context.Database.EnsureCreated();
                break;
            case TestSchemaSource.Migrations:
                if (!context.Database.GetMigrations().Any())
                {
                    // An engine whose provider has no migrations yet says so in its traits; for any other, it is a fault.
                    var withoutMigrations = EngineTraits.All.FirstOrDefault(traits => traits.ProviderType == provider.GetType() && !traits.HasSchemaMigrations);
                    Assert.SkipWhen(withoutMigrations is not null, $"The provider of {withoutMigrations?.Provider} has no migrations until it is registered with its baseline, so no schema can be built from them yet.");
                    throw new NotSupportedException($"The provider {context.Database.ProviderName} has no migrations to build a schema from.");
                }

                context.Database.Migrate();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(source), source, null);
        }
    }
}
