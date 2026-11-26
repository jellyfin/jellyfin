#pragma warning disable CS0618

using System.Threading.Tasks;
using Jellyfin.Api.Controllers;
using Jellyfin.Api.Models.StartupDtos;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Server.Implementations.Users;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Jellyfin.Api.Tests.Controllers;

public class StartupControllerTests
{
    private readonly StartupController _subject;
    private readonly Mock<IUserManager> _mockUserManager;
    private readonly Mock<IServerConfigurationManager> _mockConfig;

    public StartupControllerTests()
    {
        _mockUserManager = new Mock<IUserManager>();
        _mockConfig = new Mock<IServerConfigurationManager>();
        _subject = new StartupController(_mockConfig.Object, _mockUserManager.Object);
    }

    private static User CreateUser()
        => new User(
            "jellyfin",
            typeof(DefaultAuthenticationProvider).FullName!,
            typeof(DefaultPasswordResetProvider).FullName!);

    [Fact]
    public async Task UpdateStartupUser_WhenPasswordAlreadyConfigured_ReturnsForbidden()
    {
        var user = CreateUser();
        user.Username = "this-user-exists";
        user.Password = "already-set-hash";
        _mockUserManager.Setup(m => m.GetUserByName(user.Username)).Returns(user);

        var result = await _subject.UpdateStartupUser(new StartupUserDto { Name = "this-user-exists", Password = "new-pw" });

        // The startup wizard must never overwrite the password of an already-provisioned
        // account, even if IsStartupWizardCompleted has been cleared.
        Assert.IsType<ForbidResult>(result);
        _mockUserManager.Verify(m => m.ChangePassword(It.IsAny<System.Guid>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UpdateStartupUser_WhenNoPasswordYet_SetsPassword()
    {
        var user = CreateUser();
        user.Username = "user-does-not-exist";
        user.Password = string.Empty;

        _mockUserManager.Setup(m => m.CreateUserAsync(user.Username)).ReturnsAsync(user);

        var result = await _subject.UpdateStartupUser(new StartupUserDto { Name = user.Username, Password = "first-pw" });

        Assert.IsType<NoContentResult>(result);
        _mockUserManager.Verify(m => m.ChangePassword(user.Id, "first-pw"), Times.Once);
        _mockUserManager.Verify(m => m.CreateUserAsync(user.Username), Times.Once);
    }
}
