using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Database.Providers.Sqlite;
using Jellyfin.Server.Implementations.Users;
using MediaBrowser.Common;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Authentication;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Cryptography;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Users
{
    public sealed class UserManagerProfileImageTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<JellyfinDbContext> _dbOptions;
        private readonly UserManager _userManager;

        public UserManagerProfileImageTests()
        {
            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();

            _dbOptions = new DbContextOptionsBuilder<JellyfinDbContext>()
                .UseSqlite(_connection)
                .Options;

            // Create the schema
            using var ctx = CreateDbContext();
            ctx.Database.EnsureCreated();

            var factory = new Mock<IDbContextFactory<JellyfinDbContext>>();
            factory.Setup(f => f.CreateDbContext()).Returns(CreateDbContext);
            factory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreateDbContext);

            var cryptoProvider = new Mock<ICryptoProvider>();
            var configManager = new Mock<IServerConfigurationManager>();
            var appPaths = new Mock<IServerApplicationPaths>();
            appPaths.Setup(x => x.ProgramDataPath).Returns(Path.GetTempPath());
            configManager.Setup(x => x.ApplicationPaths).Returns(appPaths.Object);

            var appHost = new Mock<IApplicationHost>();

            var defaultAuthProvider = new DefaultAuthenticationProvider(
                NullLogger<DefaultAuthenticationProvider>.Instance,
                cryptoProvider.Object);
            var invalidAuthProvider = new InvalidAuthProvider();
            var defaultPasswordResetProvider = new DefaultPasswordResetProvider(
                configManager.Object,
                appHost.Object);

            _userManager = new UserManager(
                factory.Object,
                new NoopEventManager(),
                new Mock<INetworkManager>().Object,
                appHost.Object,
                new Mock<IImageProcessor>().Object,
                NullLogger<UserManager>.Instance,
                configManager.Object,
                new IPasswordResetProvider[] { defaultPasswordResetProvider },
                new IAuthenticationProvider[] { defaultAuthProvider, invalidAuthProvider });
        }

        public void Dispose()
        {
            _userManager.Dispose();
            _connection.Dispose();
        }

        private JellyfinDbContext CreateDbContext()
        {
            return new JellyfinDbContext(
                _dbOptions,
                NullLogger<JellyfinDbContext>.Instance,
                new SqliteDatabaseProvider(null!, NullLogger<SqliteDatabaseProvider>.Instance),
                new NoLockBehavior(NullLogger<NoLockBehavior>.Instance));
        }

        [Fact]
        public async Task ClearProfileImageAsync_WhenInMemoryImageHasTemporaryKey_RemovesPersistedImage()
        {
            var user = await _userManager.CreateUserAsync("profileimageuser");

            // Assign a profile image the same way the image endpoint does and persist it.
            // UpdateUserAsync creates the persisted ImageInfo on a separately loaded db entity,
            // so the in-memory instance below is never assigned the database generated key.
            user.ProfileImage = new ImageInfo(Path.Combine(Path.GetTempPath(), "profile.png"));
            await _userManager.UpdateUserAsync(user);

            // Precondition reproducing the bug: the in-memory image still carries the default,
            // never-persisted (temporary) key, while a real image row exists in the database.
            Assert.Equal(0, user.ProfileImage.Id);
            Assert.NotNull(_userManager.GetUserById(user.Id)!.ProfileImage);

            // This used to throw InvalidOperationException:
            // "The property 'ImageInfo.Id' has a temporary value while attempting to change the entity's state to 'Deleted'."
            var exception = await Record.ExceptionAsync(() => _userManager.ClearProfileImageAsync(user));

            Assert.Null(exception);
            Assert.Null(user.ProfileImage);
            Assert.Null(_userManager.GetUserById(user.Id)!.ProfileImage);
        }

        [Fact]
        public async Task ClearProfileImageAsync_WhenNoProfileImage_DoesNothing()
        {
            var user = await _userManager.CreateUserAsync("noprofileimageuser");

            var exception = await Record.ExceptionAsync(() => _userManager.ClearProfileImageAsync(user));

            Assert.Null(exception);
            Assert.Null(user.ProfileImage);
        }

        private sealed class NoopEventManager : IEventManager
        {
            public void Publish<T>(T eventArgs)
                where T : EventArgs
            {
            }

            public Task PublishAsync<T>(T eventArgs)
                where T : EventArgs
                => Task.CompletedTask;
        }
    }
}
