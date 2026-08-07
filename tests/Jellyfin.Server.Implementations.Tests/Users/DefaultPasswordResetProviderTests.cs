using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Server.Implementations.Users;
using MediaBrowser.Common;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Users
{
    public sealed class DefaultPasswordResetProviderTests : IDisposable
    {
        private readonly string _passwordResetFileBaseDir;
        private readonly RecordingLogger<DefaultPasswordResetProvider> _logger;
        private readonly DefaultPasswordResetProvider _provider;
        private readonly Mock<IUserManager> _userManager;

        public DefaultPasswordResetProviderTests()
        {
            _passwordResetFileBaseDir = Directory.CreateTempSubdirectory(nameof(DefaultPasswordResetProviderTests)).FullName;

            var appPaths = new Mock<IServerApplicationPaths>();
            appPaths.Setup(x => x.ProgramDataPath).Returns(_passwordResetFileBaseDir);

            var configManager = new Mock<IServerConfigurationManager>();
            configManager.Setup(x => x.ApplicationPaths).Returns(appPaths.Object);

            _userManager = new Mock<IUserManager>();

            var appHost = new Mock<IApplicationHost>();
            appHost.Setup(x => x.Resolve<IUserManager>()).Returns(_userManager.Object);

            _logger = new RecordingLogger<DefaultPasswordResetProvider>();

            _provider = new DefaultPasswordResetProvider(
                configManager.Object,
                appHost.Object,
                _logger);
        }

        public void Dispose()
        {
            Directory.Delete(_passwordResetFileBaseDir, true);
        }

        [Fact]
        public async Task RedeemPasswordResetPin_CorruptResetFilePresent_DoesNotThrowJsonException()
        {
            await File.WriteAllTextAsync(
                Path.Combine(_passwordResetFileBaseDir, "passwordreset-corrupt.json"),
                "{ this is not valid json",
                TestContext.Current.CancellationToken);

            var ex = await Record.ExceptionAsync(() => _provider.RedeemPasswordResetPin("1234"));

            Assert.IsType<ResourceNotFoundException>(ex);
        }

        [Fact]
        public async Task RedeemPasswordResetPin_CorruptResetFilePresent_DeletesCorruptFileAndStillRedeemsValidOne()
        {
            var corruptFile = Path.Combine(_passwordResetFileBaseDir, "passwordreset-corrupt.json");
            await File.WriteAllTextAsync(corruptFile, "{ this is not valid json", TestContext.Current.CancellationToken);

            var user = new User("testuser", "DefaultAuthenticationProvider", "DefaultPasswordResetProvider");
            _userManager.Setup(x => x.GetUserByName("testuser")).Returns(user);
            _userManager.Setup(x => x.ChangePassword(user.Id, "1234")).Returns(Task.CompletedTask);

            var validFile = Path.Combine(_passwordResetFileBaseDir, "passwordreset-valid.json");
            var validReset = new
            {
                ExpirationDate = DateTime.UtcNow.AddMinutes(30),
                Pin = "1234",
                PinFile = validFile,
                UserName = "testuser"
            };
            await File.WriteAllTextAsync(validFile, JsonSerializer.Serialize(validReset), TestContext.Current.CancellationToken);

            var result = await _provider.RedeemPasswordResetPin("1234");

            Assert.True(result.Success);
            Assert.Contains("testuser", result.UsersReset);
            Assert.False(File.Exists(corruptFile));
        }

        [Fact]
        public async Task RedeemPasswordResetPin_NullDeserializedResetFilePresent_DoesNotDeleteFile()
        {
            var nullFile = Path.Combine(_passwordResetFileBaseDir, "passwordreset-null.json");
            await File.WriteAllTextAsync(nullFile, "null", TestContext.Current.CancellationToken);

            var ex = await Record.ExceptionAsync(() => _provider.RedeemPasswordResetPin("1234"));

            Assert.IsType<ResourceNotFoundException>(ex);
            Assert.True(File.Exists(nullFile));
        }

        [Fact]
        public async Task RedeemPasswordResetPin_CorruptResetFilePresent_DoesNotLogFilePathOrPin()
        {
            var corruptFile = Path.Combine(_passwordResetFileBaseDir, "passwordreset-corrupt.json");
            await File.WriteAllTextAsync(corruptFile, "{ this is not valid json", TestContext.Current.CancellationToken);

            await Record.ExceptionAsync(() => _provider.RedeemPasswordResetPin("1234"));

            Assert.NotEmpty(_logger.Messages);
            Assert.All(_logger.Messages, message => Assert.DoesNotContain(corruptFile, message, StringComparison.Ordinal));
            Assert.All(_logger.Messages, message => Assert.DoesNotContain(_passwordResetFileBaseDir, message, StringComparison.Ordinal));
        }

        private sealed class RecordingLogger<T> : ILogger<T>
        {
            public List<string> Messages { get; } = new();

            public IDisposable? BeginScope<TState>(TState state)
                where TState : notnull
                => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                Messages.Add(formatter(state, exception));
            }
        }
    }
}
