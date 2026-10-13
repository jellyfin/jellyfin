using System.Threading.Tasks;
using Jellyfin.Api.Controllers;
using Jellyfin.Api.Models.StartupDtos;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Server.Implementations.Users;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Configuration;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Jellyfin.Api.Tests.Controllers;

public class StartupControllerTests
{
    private readonly StartupController _subject;
    private readonly Mock<IUserManager> _mockUserManager;
    private readonly Mock<IServerConfigurationManager> _mockConfig;
    private readonly Mock<IMediaEncoder> _mockMediaEncoder;
    private readonly ServerConfiguration _configuration;

    public StartupControllerTests()
    {
        _mockUserManager = new Mock<IUserManager>();
        _mockConfig = new Mock<IServerConfigurationManager>();
        _mockMediaEncoder = new Mock<IMediaEncoder>();
        _configuration = new ServerConfiguration();
        _mockConfig.Setup(m => m.Configuration).Returns(_configuration);
        _subject = new StartupController(_mockConfig.Object, _mockUserManager.Object, _mockMediaEncoder.Object);
    }

    private static User CreateUser()
        => new User(
            "jellyfin",
            typeof(DefaultAuthenticationProvider).FullName!,
            typeof(DefaultPasswordResetProvider).FullName!);

    [Fact]
    public async Task UpdateStartupUser_WhenNoUserExists_ReturnsNotFound()
    {
        _mockUserManager.Setup(m => m.GetFirstUser()).Returns((User?)null);

        var result = await _subject.UpdateStartupUser(new StartupUserDto { Name = "admin", Password = "pw" });

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task UpdateStartupUser_WhenPasswordAlreadyConfigured_ReturnsForbidden()
    {
        var user = CreateUser();
        user.Password = "already-set-hash";
        _mockUserManager.Setup(m => m.GetFirstUser()).Returns(user);

        var result = await _subject.UpdateStartupUser(new StartupUserDto { Name = "attacker", Password = "new-pw" });

        // The startup wizard must never overwrite the password of an already-provisioned
        // account, even if IsStartupWizardCompleted has been cleared.
        Assert.IsType<ForbidResult>(result);
        _mockUserManager.Verify(m => m.ChangePassword(It.IsAny<System.Guid>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UpdateStartupUser_WhenNoPasswordYet_SetsPassword()
    {
        var user = CreateUser();
        Assert.True(string.IsNullOrEmpty(user.Password));
        _mockUserManager.Setup(m => m.GetFirstUser()).Returns(user);

        var result = await _subject.UpdateStartupUser(new StartupUserDto { Name = "jellyfin", Password = "first-pw" });

        Assert.IsType<NoContentResult>(result);
        _mockUserManager.Verify(m => m.ChangePassword(user.Id, "first-pw"), Times.Once);
    }

    [Fact]
    public void CompleteWizard_FirstTime_EnablesRecommendedHardwareAcceleration()
    {
        var result = _subject.CompleteWizard();

        Assert.IsType<NoContentResult>(result);
        Assert.True(_configuration.IsStartupWizardCompleted);
        _mockMediaEncoder.Verify(m => m.EnableRecommendedHardwareAcceleration(), Times.Once);
    }

    [Fact]
    public void CompleteWizard_AlreadyCompleted_LeavesHardwareAccelerationAlone()
    {
        _configuration.IsStartupWizardCompleted = true;

        var result = _subject.CompleteWizard();

        Assert.IsType<NoContentResult>(result);
        _mockMediaEncoder.Verify(m => m.EnableRecommendedHardwareAcceleration(), Times.Never);
    }
}
