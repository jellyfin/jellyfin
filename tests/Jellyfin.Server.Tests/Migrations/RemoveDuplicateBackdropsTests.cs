using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.IO;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Database.Providers.Sqlite;
using Jellyfin.Server.Migrations.Routines;
using Jellyfin.Server.ServerSetupApp;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities.Movies;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Tests.Migrations;

public sealed class RemoveDuplicateBackdropsTests : IDisposable
{
    private static readonly byte[] _image = [1, 2, 3, 4];
    private static readonly byte[] _sameLengthImage = [4, 3, 2, 1];
    private static readonly byte[] _longerImage = [1, 2, 3, 4, 5];

    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<JellyfinDbContext> _dbOptions;
    private readonly string _dir;

    public RemoveDuplicateBackdropsTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _dbOptions = new DbContextOptionsBuilder<JellyfinDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = CreateDbContext();
        context.Database.EnsureCreated();

        _dir = Directory.CreateTempSubdirectory("jellyfin-backdrops").FullName;
        Directory.CreateDirectory(Path.Combine(_dir, "metadata", "movie"));
        Directory.CreateDirectory(Path.Combine(_dir, "metadata", "other"));
        Directory.CreateDirectory(Path.Combine(_dir, "media", "movie"));
    }

    public void Dispose()
    {
        _connection.Dispose();
        Directory.Delete(_dir, true);
    }

    [Fact]
    public async Task Perform_IdenticalBackdrops_KeepsOneCopyPerItem()
    {
        var movie = new BaseItemEntity { Id = Guid.NewGuid(), Type = typeof(Movie).FullName! };
        var other = new BaseItemEntity { Id = Guid.NewGuid(), Type = typeof(Movie).FullName! };

        using (var ctx = CreateDbContext())
        {
            ctx.BaseItems.AddRange(movie, other);
            ctx.BaseItemImageInfos.AddRange(
                Image(movie, WriteImage("metadata/movie/backdrop.jpg", _image)),
                Image(movie, WriteImage("metadata/movie/backdrop1.jpg", _image)),
                Image(movie, WriteImage("metadata/movie/backdrop2.jpg", _image)),
                Image(movie, WriteImage("metadata/movie/backdrop3.jpg", _sameLengthImage)),
                Image(movie, WriteImage("metadata/movie/backdrop4.jpg", _longerImage)),
                Image(movie, Path.Combine(_dir, "metadata/movie/missing.jpg")),
                Image(movie, "https://example.com/backdrop.jpg"),
                Image(movie, WriteImage("metadata/movie/poster.jpg", _image), ImageInfoImageType.Primary),
                Image(other, WriteImage("metadata/other/backdrop.jpg", _image)));

            await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await CreateMigration().PerformAsync(TestContext.Current.CancellationToken);

        Assert.False(File.Exists(Path.Combine(_dir, "metadata/movie/backdrop1.jpg")));
        Assert.False(File.Exists(Path.Combine(_dir, "metadata/movie/backdrop2.jpg")));
        Assert.True(File.Exists(Path.Combine(_dir, "metadata/movie/backdrop.jpg")));
        Assert.True(File.Exists(Path.Combine(_dir, "metadata/movie/backdrop3.jpg")));
        Assert.True(File.Exists(Path.Combine(_dir, "metadata/movie/backdrop4.jpg")));
        Assert.True(File.Exists(Path.Combine(_dir, "metadata/movie/poster.jpg")));
        Assert.True(File.Exists(Path.Combine(_dir, "metadata/other/backdrop.jpg")));

        using (var ctx = CreateDbContext())
        {
            var remaining = ctx.BaseItemImageInfos
                .Select(e => e.Path)
                .AsEnumerable()
                .Select(p => p.Replace(_dir, string.Empty, StringComparison.Ordinal).Replace('\\', '/'))
                .Order(StringComparer.Ordinal)
                .ToArray();

            Assert.Equal(
                [
                    "/metadata/movie/backdrop.jpg",
                    "/metadata/movie/backdrop3.jpg",
                    "/metadata/movie/backdrop4.jpg",
                    "/metadata/movie/missing.jpg",
                    "/metadata/movie/poster.jpg",
                    "/metadata/other/backdrop.jpg",
                    "https://example.com/backdrop.jpg"
                ],
                remaining);
        }
    }

    [Fact]
    public async Task Perform_VirtualMetadataPath_RemovesCopies()
    {
        var movie = new BaseItemEntity { Id = Guid.NewGuid(), Type = typeof(Movie).FullName! };
        WriteImage("metadata/movie/backdrop.jpg", _image);
        WriteImage("metadata/movie/backdrop1.jpg", _image);

        using (var ctx = CreateDbContext())
        {
            ctx.BaseItems.Add(movie);
            ctx.BaseItemImageInfos.AddRange(
                Image(movie, "%MetadataPath%/movie/backdrop.jpg"),
                Image(movie, "%MetadataPath%/movie/backdrop1.jpg"));

            await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await CreateMigration().PerformAsync(TestContext.Current.CancellationToken);

        Assert.True(File.Exists(Path.Combine(_dir, "metadata/movie/backdrop.jpg")));
        Assert.False(File.Exists(Path.Combine(_dir, "metadata/movie/backdrop1.jpg")));

        using (var ctx = CreateDbContext())
        {
            Assert.Equal("%MetadataPath%/movie/backdrop.jpg", Assert.Single(ctx.BaseItemImageInfos).Path);
        }
    }

    [Fact]
    public async Task Perform_IdenticalBackdropsOutsideMetadataFolder_KeepsFiles()
    {
        var movie = new BaseItemEntity { Id = Guid.NewGuid(), Type = typeof(Movie).FullName! };

        using (var ctx = CreateDbContext())
        {
            ctx.BaseItems.Add(movie);
            ctx.BaseItemImageInfos.AddRange(
                Image(movie, WriteImage("media/movie/fanart.jpg", _image)),
                Image(movie, WriteImage("media/movie/backdrop1.jpg", _image)));

            await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await CreateMigration().PerformAsync(TestContext.Current.CancellationToken);

        Assert.True(File.Exists(Path.Combine(_dir, "media/movie/fanart.jpg")));
        Assert.True(File.Exists(Path.Combine(_dir, "media/movie/backdrop1.jpg")));

        using (var ctx = CreateDbContext())
        {
            Assert.Equal(2, ctx.BaseItemImageInfos.Count());
        }
    }

    private string WriteImage(string relativePath, byte[] content)
    {
        var path = Path.Combine(_dir, relativePath);
        File.WriteAllBytes(path, content);
        return path;
    }

    private static BaseItemImageInfo Image(BaseItemEntity item, string path, ImageInfoImageType type = ImageInfoImageType.Backdrop)
        => new()
        {
            Id = Guid.NewGuid(),
            ItemId = item.Id,
            Item = item,
            Path = path,
            ImageType = type
        };

    private JellyfinDbContext CreateDbContext() => new(
        _dbOptions,
        NullLogger<JellyfinDbContext>.Instance,
        new SqliteDatabaseProvider(new Mock<IApplicationPaths>().Object, NullLogger<SqliteDatabaseProvider>.Instance),
        new NoLockBehavior(NullLogger<NoLockBehavior>.Instance));

    private RemoveDuplicateBackdrops CreateMigration()
    {
        var factory = new Mock<IDbContextFactory<JellyfinDbContext>>();
        factory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(CreateDbContext);

        var metadataPath = Path.Combine(_dir, "metadata");
        var appHost = new Mock<IServerApplicationHost>();
        appHost.Setup(h => h.ExpandVirtualPath(It.IsAny<string>()))
            .Returns((string path) => path.Replace("%MetadataPath%", metadataPath, StringComparison.Ordinal));
        var appPaths = new Mock<IServerApplicationPaths>();
        appPaths.Setup(p => p.InternalMetadataPath).Returns(metadataPath);
        var fileSystem = new ManagedFileSystem(NullLogger<ManagedFileSystem>.Instance, Mock.Of<IApplicationPaths>(), []);

        return new RemoveDuplicateBackdrops(
            new StartupLogger<RemoveDuplicateBackdrops>(NullLogger<RemoveDuplicateBackdrops>.Instance),
            factory.Object,
            appHost.Object,
            appPaths.Object,
            fileSystem);
    }
}
