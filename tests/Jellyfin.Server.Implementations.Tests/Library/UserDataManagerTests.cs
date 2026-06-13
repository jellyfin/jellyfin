using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Emby.Server.Implementations.Library;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Database.Providers.Sqlite;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Library
{
    public sealed class UserDataManagerTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<JellyfinDbContext> _dbOptions;
        private readonly UserDataManager _userDataManager;

        public UserDataManagerTests()
        {
            Video.RecordingsManager = Mock.Of<IRecordingsManager>();

            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();

            _dbOptions = new DbContextOptionsBuilder<JellyfinDbContext>()
                .UseSqlite(_connection)
                .Options;

            using var ctx = CreateDbContext();
            ctx.Database.EnsureCreated();

            var factory = new Mock<IDbContextFactory<JellyfinDbContext>>();
            factory.Setup(f => f.CreateDbContext()).Returns(CreateDbContext);

            var configManager = new Mock<IServerConfigurationManager>();
            configManager.Setup(x => x.Configuration).Returns(new ServerConfiguration());

            _userDataManager = new UserDataManager(configManager.Object, factory.Object);
        }

        public void Dispose()
        {
            _connection.Dispose();
        }

        // Regression test for #15140: favoriting a video whose IMDb and TMDB provider ids are
        // identical (e.g. a malformed NFO) makes Video.GetUserDataKeys() return the same key
        // twice. SaveUserData used to add one UserData entity per key without de-duplicating,
        // so EF Core threw "The instance of entity type 'UserData' cannot be tracked because
        // another instance with the same key value ... is already being tracked".
        [Fact]
        public void SaveUserData_WithDuplicateUserDataKeys_PersistsWithoutThrowing()
        {
            var itemId = Guid.NewGuid();
            var user = new User("testuser", "Test", "Test");

            using (var seed = CreateDbContext())
            {
                seed.BaseItems.Add(new BaseItemEntity { Id = itemId, Type = "Movie" });
                seed.Users.Add(user);
                seed.SaveChanges();
            }

            var item = new Movie
            {
                Id = itemId,
                ProviderIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Imdb"] = "tt1234567",
                    ["Tmdb"] = "tt1234567",
                },
            };

            // Sanity check: the keys really do contain a duplicate.
            var keys = item.GetUserDataKeys();
            Assert.NotEqual(keys.Count, keys.Distinct().Count());

            var userData = new UserItemData { Key = string.Empty, IsFavorite = true };

            var exception = Record.Exception(
                () => _userDataManager.SaveUserData(user, item, userData, UserDataSaveReason.UpdateUserData, CancellationToken.None));

            Assert.Null(exception);

            using var verify = CreateDbContext();
            var rows = verify.UserData.AsNoTracking().Where(u => u.ItemId.Equals(itemId)).ToList();
            Assert.NotEmpty(rows);
            Assert.All(rows, r => Assert.True(r.IsFavorite));
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
}
