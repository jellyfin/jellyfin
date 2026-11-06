using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Database.Providers.Sqlite;
using Jellyfin.Server.Migrations.Routines;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Server.Tests.Migrations;

/// <summary>
/// Round-trips the hand written ItemValues access against a real SQLite database, so the Guids it
/// writes match the ones the provider wrote to BaseItems.
/// </summary>
public sealed class LegacyItemValuesTests : IDisposable
{
    private static readonly Guid _movieId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid _genreId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid _tagId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<JellyfinDbContext> _dbOptions;

    public LegacyItemValuesTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _dbOptions = new DbContextOptionsBuilder<JellyfinDbContext>().UseSqlite(_connection).Options;

        using var ctx = CreateDbContext();
        ctx.Database.EnsureCreated();
        ctx.Database.ExecuteSqlRaw("CREATE TABLE ItemValues (ItemValueId TEXT PRIMARY KEY, Type INTEGER NOT NULL, Value TEXT NOT NULL, CleanValue TEXT NOT NULL)");
        ctx.Database.ExecuteSqlRaw("CREATE TABLE ItemValuesMap (ItemValueId TEXT NOT NULL, ItemId TEXT NOT NULL, PRIMARY KEY (ItemValueId, ItemId))");
        ctx.BaseItems.Add(new BaseItemEntity { Id = _movieId, Type = "MediaBrowser.Controller.Entities.Movies.Movie", Name = "Movie" });
        ctx.SaveChanges();
    }

    public void Dispose() => _connection.Dispose();

    [Fact]
    public async Task Write_ThenRead_LinksTheValueToItsItem()
    {
        using (var ctx = CreateDbContext())
        {
            LegacyItemValues.Write(ctx, [(_genreId, LegacyItemValues.Genre, "Drama", "drama", [_movieId])]);
        }

        using (var ctx = CreateDbContext())
        {
            var values = await LegacyItemValues.ReadValuesAsync(ctx, [LegacyItemValues.Genre], CancellationToken.None);
            var value = Assert.Single(values);
            Assert.Equal(_genreId, value.ItemValueId);
            Assert.Equal("Drama", value.Value);

            var link = Assert.Single(await LegacyItemValues.ReadLinksAsync(ctx, values, CancellationToken.None));
            Assert.Equal(_movieId, link.ItemId);
            Assert.Equal("MediaBrowser.Controller.Entities.Movies.Movie", link.ItemType);
        }
    }

    [Fact]
    public async Task DeleteAsync_RemovesOnlyTheGivenTypes()
    {
        using (var ctx = CreateDbContext())
        {
            LegacyItemValues.Write(
                ctx,
                [
                    (_genreId, LegacyItemValues.Genre, "Drama", "drama", [_movieId]),
                    (_tagId, LegacyItemValues.Tag, "Favourite", "favourite", [_movieId])
                ]);
        }

        using (var ctx = CreateDbContext())
        {
            Assert.Equal(1, await LegacyItemValues.DeleteAsync(ctx, [LegacyItemValues.Genre], CancellationToken.None));

            Assert.Empty(await LegacyItemValues.ReadValuesAsync(ctx, [LegacyItemValues.Genre], CancellationToken.None));
            var tags = await LegacyItemValues.ReadValuesAsync(ctx, [LegacyItemValues.Tag], CancellationToken.None);
            Assert.Single(await LegacyItemValues.ReadLinksAsync(ctx, tags, CancellationToken.None));
        }
    }

    private JellyfinDbContext CreateDbContext()
    {
        return new JellyfinDbContext(
            _dbOptions,
            NullLogger<JellyfinDbContext>.Instance,
            new SqliteDatabaseProvider(null!, NullLogger<SqliteDatabaseProvider>.Instance),
            new NoLockBehavior(NullLogger<NoLockBehavior>.Instance));
    }
}
