using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Server.Implementations.Users;
using MediaBrowser.Common;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Users;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Users
{
    public sealed class DefaultPasswordResetProviderTests : IDisposable
    {
        private readonly string _dataPath;
        private readonly DefaultPasswordResetProvider _provider;

        public DefaultPasswordResetProviderTests()
        {
            _dataPath = Path.Combine(Path.GetTempPath(), "jf-pwreset-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dataPath);

            _provider = CreateProvider(_dataPath, NullLogger<DefaultPasswordResetProvider>.Instance);
        }

        public void Dispose()
        {
            Directory.Delete(_dataPath, true);
        }

        [Fact]
        public async Task StartForgotPasswordProcess_ExistingAndUnknownUser_ReturnSameShape()
        {
            var user = new User("someone", "default", "default");

            var existing = await _provider.StartForgotPasswordProcess(user, "someone", true);
            var unknown = await _provider.StartForgotPasswordProcess(null, "nobody", true);

            Assert.Equal(ForgotPasswordAction.PinCode, existing.Action);
            Assert.Equal(ForgotPasswordAction.PinCode, unknown.Action);
            Assert.NotNull(existing.PinExpirationDate);
            Assert.NotNull(unknown.PinExpirationDate);
            Assert.StartsWith(Path.Combine(_dataPath, "passwordreset"), existing.PinFile, StringComparison.Ordinal);
            Assert.StartsWith(Path.Combine(_dataPath, "passwordreset"), unknown.PinFile, StringComparison.Ordinal);

            // Let the background write for the existing user finish before cleanup deletes the directory.
            Assert.True(await WaitForFileAsync(existing.PinFile!), "PIN file was not written in the background");
        }

        [Fact]
        public async Task StartForgotPasswordProcess_ExistingInNetworkUser_EventuallyWritesPinFile()
        {
            var user = new User("someone", "default", "default");

            var result = await _provider.StartForgotPasswordProcess(user, "someone", true);

            Assert.True(await WaitForFileAsync(result.PinFile!), "PIN file was not written in the background");
            // The file is written elsewhere and moved into place, so only the finished PIN file is left behind.
            Assert.Equal(result.PinFile, Assert.Single(Directory.EnumerateFiles(_dataPath)));
        }

        [Fact]
        public async Task StartForgotPasswordProcess_UnknownUser_WritesNoPinFile()
        {
            var result = await _provider.StartForgotPasswordProcess(null, "nobody", true);

            Assert.False(await WaitForFileAsync(result.PinFile!, TimeSpan.FromMilliseconds(500)));
        }

        [Fact]
        public async Task StartForgotPasswordProcess_OutOfNetworkUser_WritesNoPinFile()
        {
            var user = new User("someone", "default", "default");

            var result = await _provider.StartForgotPasswordProcess(user, "someone", false);

            Assert.False(await WaitForFileAsync(result.PinFile!, TimeSpan.FromMilliseconds(500)));
        }

        [Fact]
        public async Task StartForgotPasswordProcess_PinFileWriteFails_ReturnsResultAndLogsError()
        {
            var logger = new Mock<ILogger<DefaultPasswordResetProvider>>();
            var provider = CreateProvider(Path.Combine(_dataPath, "missing"), logger.Object);
            var user = new User("someone", "default", "default");

            var result = await provider.StartForgotPasswordProcess(user, "someone", true);

            Assert.Equal(ForgotPasswordAction.PinCode, result.Action);
            Assert.True(
                await WaitForAsync(() => logger.Invocations.Any(i => i.Method.Name == nameof(ILogger.Log) && (LogLevel)i.Arguments[0] == LogLevel.Error)),
                "The failed PIN file write was not logged");
        }

        private static DefaultPasswordResetProvider CreateProvider(string dataPath, ILogger<DefaultPasswordResetProvider> logger)
        {
            var appPaths = new Mock<IServerApplicationPaths>();
            appPaths.Setup(x => x.ProgramDataPath).Returns(dataPath);
            var configManager = new Mock<IServerConfigurationManager>();
            configManager.Setup(x => x.ApplicationPaths).Returns(appPaths.Object);

            return new DefaultPasswordResetProvider(
                configManager.Object,
                new Mock<IApplicationHost>().Object,
                logger);
        }

        private static Task<bool> WaitForFileAsync(string path, TimeSpan? timeout = null)
            => WaitForAsync(() => File.Exists(path), timeout);

        private static async Task<bool> WaitForAsync(Func<bool> condition, TimeSpan? timeout = null)
        {
            var stopwatch = Stopwatch.StartNew();
            while (stopwatch.Elapsed < (timeout ?? TimeSpan.FromSeconds(10))) // default timeout: 10 seconds
            {
                if (condition())
                {
                    return true;
                }

                await Task.Delay(25);
            }

            return false;
        }
    }
}
