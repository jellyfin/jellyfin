using System.Linq;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Entities.Security;
using Jellyfin.Database.Providers.Sqlite.Migrations;
using Jellyfin.Server.Implementations.Migrations;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.EfMigrations;

public class EfMigrationTests
{
    [Fact]
    public void CheckForUnappliedMigrations_SqLite()
    {
        var dbDesignContext = new SqliteDesignTimeJellyfinDbFactory();
        using var context = dbDesignContext.CreateDbContext([]);
        Assert.False(context.Database.HasPendingModelChanges(), "There are unapplied changes to the EFCore model for SQLite. Please create a Migration.");
    }

    [Fact]
    public void OidcExternalIdentity_ModelHasExpectedUniqueIndexes()
    {
        var dbDesignContext = new SqliteDesignTimeJellyfinDbFactory();
        using var context = dbDesignContext.CreateDbContext([]);
        var entityType = context.Model.FindEntityType(typeof(OidcExternalIdentity));

        Assert.NotNull(entityType);
        Assert.Contains(entityType!.GetIndexes(), index =>
            index.IsUnique
            && index.Properties.Select(property => property.Name).SequenceEqual(["ProviderId", "Issuer", "Subject"]));
        Assert.Contains(entityType.GetIndexes(), index =>
            index.IsUnique
            && index.Properties.Select(property => property.Name).SequenceEqual(["UserId", "ProviderId"]));
        Assert.Contains(entityType.GetForeignKeys(), foreignKey =>
            foreignKey.PrincipalEntityType.ClrType == typeof(User)
            && foreignKey.Properties.Select(property => property.Name).SequenceEqual(["UserId"])
            && foreignKey.DeleteBehavior == DeleteBehavior.Cascade);
    }

    [Fact]
    public void OidcSession_ModelIsKeyedByAccessToken()
    {
        var dbDesignContext = new SqliteDesignTimeJellyfinDbFactory();
        using var context = dbDesignContext.CreateDbContext([]);
        var entityType = context.Model.FindEntityType(typeof(OidcSession));

        Assert.NotNull(entityType);
        Assert.Contains(entityType!.GetIndexes(), index =>
            index.IsUnique
            && index.Properties.Select(property => property.Name).SequenceEqual(["AccessToken"]));
        Assert.NotNull(entityType.FindProperty(nameof(OidcSession.Sid)));
        Assert.NotNull(entityType.FindProperty(nameof(OidcSession.ProtectedIdTokenHint)));
    }
}
