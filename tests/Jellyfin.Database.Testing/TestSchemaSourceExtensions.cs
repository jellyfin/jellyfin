using System;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Locking;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

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
                context.Database.Migrate();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(source), source, null);
        }
    }
}
