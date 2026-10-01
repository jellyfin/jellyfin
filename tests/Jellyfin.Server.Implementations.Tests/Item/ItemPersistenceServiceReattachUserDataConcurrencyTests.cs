using System;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.Library;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Database.Providers.Sqlite;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Item;

public sealed class ItemPersistenceServiceReattachUserDataConcurrencyTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReattachUserDataAsync_OverlappingPlaybackSave_PreservesPlaybackData(bool reattachmentFirst)
    {
        var directory = Directory.CreateTempSubdirectory("jellyfin-reattachment-");
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = Path.Combine(directory.FullName, "jellyfin.db"),
            Pooling = false,
            DefaultTimeout = 5
        }.ToString();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(15));
        var cancellationToken = timeout.Token;
        var firstStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondStarting = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var first = Task.CompletedTask;
        var second = Task.CompletedTask;

        try
        {
            var item = new Folder { Id = Guid.NewGuid() };
            var user = new User("user", "auth-provider", "reset-provider");
            var key = item.GetUserDataKeys()[0];
            var retentionDate = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
            await using (var context = CreateContext(connectionString))
            {
                await context.Database.EnsureCreatedAsync(cancellationToken);
                context.Users.Add(user);
                context.BaseItems.Add(new BaseItemEntity { Id = item.Id, Type = typeof(Folder).FullName!, IsFolder = true });
                context.UserData.Add(new UserData
                {
                    ItemId = BaseItemRepository.PlaceholderId,
                    Item = null,
                    UserId = user.Id,
                    User = null,
                    CustomDataKey = key,
                    RetentionDate = retentionDate,
                    PlaybackPositionTicks = 1234,
                    Rating = 2
                });
                await context.SaveChangesAsync(cancellationToken);
            }

            var reattachmentFactory = CreateFactory(
                connectionString,
                new TransactionOrderInterceptor(reattachmentFirst, firstStarted, secondStarting, cancellationToken));
            var playbackFactory = CreateFactory(
                connectionString,
                new TransactionOrderInterceptor(!reattachmentFirst, firstStarted, secondStarting, cancellationToken));
            var service = new ItemPersistenceService(
                reattachmentFactory,
                Mock.Of<IServerApplicationHost>(),
                NullLogger<ItemPersistenceService>.Instance);
            var configuration = new Mock<IServerConfigurationManager>();
            configuration.SetupGet(e => e.Configuration).Returns(new ServerConfiguration());
            var manager = new UserDataManager(configuration.Object, playbackFactory);
            var playbackData = new UserItemData
            {
                Key = key,
                PlaybackPositionTicks = 9876,
                IsFavorite = true,
                Rating = 9,
                PlayCount = 3
            };
            Func<Task> reattach = () => service.ReattachUserDataAsync(item, cancellationToken);
            Func<Task> savePlayback = () =>
            {
                manager.SaveUserData(user, item, playbackData, UserDataSaveReason.PlaybackProgress, cancellationToken);
                return Task.CompletedTask;
            };

            // Hold the first transaction until the other connection starts its transaction.
            // SQLite must serialize these writers without either operation losing user data.
            first = Task.Run(reattachmentFirst ? reattach : savePlayback, cancellationToken);
            await firstStarted.Task.WaitAsync(cancellationToken);
            second = Task.Run(reattachmentFirst ? savePlayback : reattach, cancellationToken);
            await Task.WhenAll(first, second).WaitAsync(cancellationToken);

            await using var after = CreateContext(connectionString);
            var current = await after.UserData.SingleAsync(e => e.ItemId.Equals(item.Id), cancellationToken);
            Assert.Equal(playbackData.PlaybackPositionTicks, current.PlaybackPositionTicks);
            Assert.Equal(playbackData.IsFavorite, current.IsFavorite);
            Assert.Equal(playbackData.Rating, current.Rating);
            Assert.Equal(playbackData.PlayCount, current.PlayCount);
            Assert.Null(current.RetentionDate);
            var retained = await after.UserData.Where(e => e.ItemId.Equals(BaseItemRepository.PlaceholderId))
                .ToArrayAsync(cancellationToken);
            if (reattachmentFirst)
            {
                Assert.Empty(retained);
            }
            else
            {
                Assert.Equal(retentionDate, Assert.Single(retained).RetentionDate);
                Assert.Equal(1234, retained[0].PlaybackPositionTicks);
                Assert.Equal(2, retained[0].Rating);
            }
        }
        finally
        {
            secondStarting.TrySetResult();
            try
            {
                await Task.WhenAll(first, second);
            }
            finally
            {
                directory.Delete(true);
            }
        }
    }

    private static JellyfinDbContext CreateContext(string connectionString, params IInterceptor[] interceptors) => new(
        new DbContextOptionsBuilder<JellyfinDbContext>().UseSqlite(connectionString).AddInterceptors(interceptors).Options,
        NullLogger<JellyfinDbContext>.Instance,
        new SqliteDatabaseProvider(Mock.Of<IApplicationPaths>(), NullLogger<SqliteDatabaseProvider>.Instance),
        new NoLockBehavior(NullLogger<NoLockBehavior>.Instance));

    private static IDbContextFactory<JellyfinDbContext> CreateFactory(string connectionString, IInterceptor interceptor)
    {
        var factory = new Mock<IDbContextFactory<JellyfinDbContext>>();
        factory.Setup(e => e.CreateDbContext()).Returns(() => CreateContext(connectionString, interceptor));
        factory.Setup(e => e.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => CreateContext(connectionString, interceptor));
        return factory.Object;
    }

    private sealed class TransactionOrderInterceptor(
        bool first,
        TaskCompletionSource firstStarted,
        TaskCompletionSource secondStarting,
        CancellationToken cancellationToken) : DbTransactionInterceptor
    {
        public override InterceptionResult<DbTransaction> TransactionStarting(
            DbConnection connection, TransactionStartingEventData eventData, InterceptionResult<DbTransaction> result)
        {
            if (!first)
            {
                secondStarting.TrySetResult();
            }

            return result;
        }

        public override ValueTask<InterceptionResult<DbTransaction>> TransactionStartingAsync(
            DbConnection connection,
            TransactionStartingEventData eventData,
            InterceptionResult<DbTransaction> result,
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult(TransactionStarting(connection, eventData, result));

        public override DbTransaction TransactionStarted(
            DbConnection connection, TransactionEndEventData eventData, DbTransaction result)
        {
            WaitForOtherTransaction().GetAwaiter().GetResult();
            return result;
        }

        public override async ValueTask<DbTransaction> TransactionStartedAsync(
            DbConnection connection,
            TransactionEndEventData eventData,
            DbTransaction result,
            CancellationToken cancellationToken = default)
        {
            await WaitForOtherTransaction();
            return result;
        }

        private Task WaitForOtherTransaction()
        {
            if (!first)
            {
                return Task.CompletedTask;
            }

            firstStarted.TrySetResult();
            return secondStarting.Task.WaitAsync(cancellationToken);
        }
    }
}
