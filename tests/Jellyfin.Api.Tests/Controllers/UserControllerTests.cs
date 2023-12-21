using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using Jellyfin.Api.Controllers;
using Jellyfin.Data.Entities;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Devices;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Controller.QuickConnect;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Users;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Nikse.SubtitleEdit.Core.Common;
using Xunit;

namespace Jellyfin.Api.Tests.Controllers;

public class UserControllerTests
{
    private readonly UserController _subject;
    private readonly Mock<IUserManager> _mockUserManager;
    private readonly Mock<ISessionManager> _mockSessionManager;
    private readonly Mock<INetworkManager> _mockNetworkManager;
    private readonly Mock<IDeviceManager> _mockDeviceManager;
    private readonly Mock<IAuthorizationContext> _mockAuthorizationContext;
    private readonly Mock<IServerConfigurationManager> _mockServerConfigurationManager;
    private readonly Mock<ILogger<UserController>> _mockLogger;
    private readonly Mock<IQuickConnect> _mockQuickConnect;
    private readonly Mock<IPlaylistManager> _mockPlaylistManager;

    public UserControllerTests()
    {
        _mockUserManager = new Mock<IUserManager>();
        _mockSessionManager = new Mock<ISessionManager>();
        _mockNetworkManager = new Mock<INetworkManager>();
        _mockDeviceManager = new Mock<IDeviceManager>();
        _mockAuthorizationContext = new Mock<IAuthorizationContext>();
        _mockServerConfigurationManager = new Mock<IServerConfigurationManager>();
        _mockLogger = new Mock<ILogger<UserController>>();
        _mockQuickConnect = new Mock<IQuickConnect>();
        _mockPlaylistManager = new Mock<IPlaylistManager>();

        _subject = new UserController(
            _mockUserManager.Object,
            _mockSessionManager.Object,
            _mockNetworkManager.Object,
            _mockDeviceManager.Object,
            _mockAuthorizationContext.Object,
            _mockServerConfigurationManager.Object,
            _mockLogger.Object,
            _mockQuickConnect.Object,
            _mockPlaylistManager.Object);
    }

    [Theory]
    [AutoData]
    public async Task UpdateUserPolicy_WhenUserNotFound_ReturnsNotFound(Guid userId, UserPolicy userPolicy)
    {
        User? nullUser = null;
        _mockUserManager.
            Setup(m => m.GetUserById(userId))
            .Returns(nullUser);

        Assert.IsType<NotFoundResult>(await _subject.UpdateUserPolicy(userId, userPolicy));
    }

    [Theory]
    [InlineAutoData(null)]
    [InlineAutoData("")]
    [InlineAutoData("   ")]
    public void UpdateUserPolicy_WhenPasswordResetProviderIdNotSupplied_ReturnsBadRequest(string? passwordResetProviderId)
    {
        var userPolicy = new UserPolicy
        {
            PasswordResetProviderId = passwordResetProviderId,
            AuthenticationProviderId = "AuthenticationProviderId"
        };

        Assert.Contains(
            Validate(userPolicy), v =>
                v.MemberNames.Contains("PasswordResetProviderId") &&
                v.ErrorMessage != null &&
                v.ErrorMessage.Contains("required", StringComparison.CurrentCultureIgnoreCase));
    }

    [Theory]
    [InlineAutoData(null)]
    [InlineAutoData("")]
    [InlineAutoData("   ")]
    public void UpdateUserPolicy_WhenAuthenticationProviderIdNotSupplied_ReturnsBadRequest(string? authenticationProviderId)
    {
        var userPolicy = new UserPolicy
        {
            AuthenticationProviderId = authenticationProviderId,
            PasswordResetProviderId = "PasswordResetProviderId"
        };

        Assert.Contains(Validate(userPolicy), v =>
            v.MemberNames.Contains("AuthenticationProviderId") &&
            v.ErrorMessage != null &&
            v.ErrorMessage.Contains("required", StringComparison.CurrentCultureIgnoreCase));
    }

    private List<ValidationResult> Validate(object model)
    {
        var result = new List<ValidationResult>();
        var context = new ValidationContext(model, null, null);
        Validator.TryValidateObject(model, context, result, true);

        return result;
    }
}
