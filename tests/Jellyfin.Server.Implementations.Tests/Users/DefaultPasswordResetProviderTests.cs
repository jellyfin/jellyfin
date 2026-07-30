using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Server.Implementations.Users;
using MediaBrowser.Common;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Configuration;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Users;

public sealed class DefaultPasswordResetProviderTests
{
    [Fact]
    public async Task StartForgotPasswordProcess_PinSatisfiesConfiguredMinimumPasswordLength()
    {
        var testDirectory = Path.Combine(
            Path.GetTempPath(),
            "jellyfin-password-reset-tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(testDirectory);

        try
        {
            var paths = new Mock<IServerApplicationPaths>();
            paths.SetupGet(value => value.ProgramDataPath).Returns(testDirectory);
            var configurationManager = new Mock<IServerConfigurationManager>();
            configurationManager.SetupGet(value => value.ApplicationPaths).Returns(paths.Object);
            configurationManager
                .SetupGet(value => value.Configuration)
                .Returns(new ServerConfiguration
                {
                    PublicUserRegistrationMinimumPasswordLength = 16
                });
            var provider = new DefaultPasswordResetProvider(
                configurationManager.Object,
                new Mock<IApplicationHost>().Object);
            var user = new User("public-user", "auth", "reset");

            var result = await provider.StartForgotPasswordProcess(
                user,
                user.Username,
                true);

            await using var stream = File.OpenRead(result.PinFile);
            using var document = await JsonDocument.ParseAsync(
                stream,
                cancellationToken: TestContext.Current.CancellationToken);
            var pin = document.RootElement.GetProperty("Pin").GetString();
            Assert.NotNull(pin);
            Assert.True(pin.Length >= 16);
        }
        finally
        {
            Directory.Delete(testDirectory, true);
        }
    }
}
