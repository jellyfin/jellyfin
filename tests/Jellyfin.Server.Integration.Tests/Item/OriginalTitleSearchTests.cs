using System;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoMoq;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Configuration;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Integration.Tests.Item
{
    public sealed class OriginalTitleSearchTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<JellyfinDbContext> _dbContextOptions;
        private readonly Mock<IJellyfinDatabaseProvider> _dbProvider;
        private readonly Mock<IEntityFrameworkCoreLockingBehavior> _locking;
        private readonly BaseItemRepository _repo;

        public OriginalTitleSearchTests()
        {
            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();
            _connection.CreateFunction("CleanValue", (string? s) => s?.ToLowerInvariant(), isDeterministic: true);

            _dbProvider = CreateDbProviderMock();
            _locking = CreateLockingMock();

            _dbContextOptions = new DbContextOptionsBuilder<JellyfinDbContext>()
                .UseSqlite(_connection)
                .Options;

            using var context = CreateDbContext();
            context.Database.EnsureCreated();

            _repo = BuildRepository();
        }

        private JellyfinDbContext CreateDbContext()
            => new JellyfinDbContext(_dbContextOptions, NullLogger<JellyfinDbContext>.Instance, _dbProvider.Object, _locking.Object);

        private BaseItemRepository BuildRepository()
        {
            var appHost = new Mock<MediaBrowser.Controller.IServerApplicationHost>();
            appHost.Setup(x => x.ExpandVirtualPath(It.IsAny<string>())).Returns((string x) => x);
            appHost.Setup(x => x.ReverseVirtualPath(It.IsAny<string>())).Returns((string x) => x);

            var configManager = new Mock<IServerConfigurationManager>();
            configManager.SetupGet(x => x.Configuration).Returns(new ServerConfiguration());

            var fixture = new Fixture().Customize(new AutoMoqCustomization { ConfigureMembers = true });
            fixture.Inject<IDbContextFactory<JellyfinDbContext>>(new TestDbContextFactory(_dbContextOptions, _dbProvider.Object, _locking.Object));
            fixture.Inject(appHost.Object);
            fixture.Inject(configManager.Object);

            return fixture.Create<BaseItemRepository>();
        }

        private static Mock<IJellyfinDatabaseProvider> CreateDbProviderMock()
        {
            var mock = new Mock<IJellyfinDatabaseProvider>();
            mock.Setup(x => x.OnModelCreating(It.IsAny<ModelBuilder>()));
            mock.Setup(x => x.ConfigureConventions(It.IsAny<ModelConfigurationBuilder>()));
            return mock;
        }

        private static Mock<IEntityFrameworkCoreLockingBehavior> CreateLockingMock()
        {
            var mock = new Mock<IEntityFrameworkCoreLockingBehavior>();
            mock.Setup(x => x.OnSaveChanges(It.IsAny<JellyfinDbContext>(), It.IsAny<Action>()))
                .Callback<JellyfinDbContext, Action>((_, save) => save());
            mock.Setup(x => x.OnSaveChangesAsync(It.IsAny<JellyfinDbContext>(), It.IsAny<Func<Task>>()))
                .Returns<JellyfinDbContext, Func<Task>>((_, save) => save());
            return mock;
        }

        [Fact]
        public void GetItemList_SearchByOriginalTitle_NonAscii_ReturnsMatch()
        {
            using var context = CreateDbContext();
            var spiritedAway = new Movie { Id = Guid.NewGuid(), Name = "Spirited Away", OriginalTitle = "千と千尋の神隠し" };
            spiritedAway.SortName = spiritedAway.Name;
            var totoro = new Movie { Id = Guid.NewGuid(), Name = "My Neighbor Totoro", OriginalTitle = "となりのトトロ" };
            totoro.SortName = totoro.Name;
            context.BaseItems.AddRange(_repo.Map(spiritedAway), _repo.Map(totoro));
            context.SaveChanges();

            var results = _repo.GetItemList(new InternalItemsQuery { SearchTerm = "千と千尋" });

            Assert.Single(results);
            Assert.Equal(spiritedAway.Id, results[0].Id);
        }

        [Fact]
        public void GetItemList_SearchByOriginalTitle_AsciiCaseInsensitive_ReturnsMatch()
        {
            using var context = CreateDbContext();
            var movie = new Movie { Id = Guid.NewGuid(), Name = "My Movie", OriginalTitle = "Spirited Away" };
            movie.SortName = movie.Name;
            context.BaseItems.Add(_repo.Map(movie));
            context.SaveChanges();

            var results = _repo.GetItemList(new InternalItemsQuery { SearchTerm = "spirited away" });

            Assert.Single(results);
            Assert.Equal(movie.Id, results[0].Id);
        }

        [Fact]
        public void GetItemList_SearchByName_OriginalTitleNotMatching_ReturnsNameMatch()
        {
            using var context = CreateDbContext();
            var movie = new Movie { Id = Guid.NewGuid(), Name = "Spirited Away", OriginalTitle = "千と千尋の神隠し" };
            movie.SortName = movie.Name;
            context.BaseItems.Add(_repo.Map(movie));
            context.SaveChanges();

            var results = _repo.GetItemList(new InternalItemsQuery { SearchTerm = "Spirited Away" });

            Assert.Single(results);
            Assert.Equal(movie.Id, results[0].Id);
        }

        [Fact]
        public void GetItemList_SearchByOriginalTitle_CyrillicCaseInsensitive_ReturnsMatch()
        {
            using var context = CreateDbContext();
            var movie = new Movie { Id = Guid.NewGuid(), Name = "The Expendables", OriginalTitle = "Неудержимые" };
            movie.SortName = movie.Name;
            context.BaseItems.Add(_repo.Map(movie));
            context.SaveChanges();

            var results = _repo.GetItemList(new InternalItemsQuery { SearchTerm = "неудержимые" });

            Assert.Single(results);
            Assert.Equal(movie.Id, results[0].Id);
        }

        [Fact]
        public void GetItemList_SearchTerm_NoMatch_ReturnsEmpty()
        {
            using var context = CreateDbContext();
            var movie = new Movie { Id = Guid.NewGuid(), Name = "Spirited Away", OriginalTitle = "千と千尋の神隠し" };
            movie.SortName = movie.Name;
            context.BaseItems.Add(_repo.Map(movie));
            context.SaveChanges();

            var results = _repo.GetItemList(new InternalItemsQuery { SearchTerm = "Totoro" });

            Assert.Empty(results);
        }

        public void Dispose() => _connection.Dispose();

        private sealed class TestDbContextFactory : IDbContextFactory<JellyfinDbContext>
        {
            private readonly DbContextOptions<JellyfinDbContext> _options;
            private readonly IJellyfinDatabaseProvider _provider;
            private readonly IEntityFrameworkCoreLockingBehavior _locking;

            public TestDbContextFactory(DbContextOptions<JellyfinDbContext> options, IJellyfinDatabaseProvider provider, IEntityFrameworkCoreLockingBehavior locking)
            {
                _options = options;
                _provider = provider;
                _locking = locking;
            }

            public JellyfinDbContext CreateDbContext()
                => new JellyfinDbContext(_options, NullLogger<JellyfinDbContext>.Instance, _provider, _locking);
        }
    }
}
