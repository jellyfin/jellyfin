using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoFixture.Xunit3;
using Jellyfin.Api.Constants;
using Jellyfin.Api.Controllers;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Devices;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Controller.QuickConnect;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Users;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
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
    public void GetUserById_NonAdminRequestsOtherUser_ReturnsForbidden(Guid requesterId, Guid otherUserId)
    {
        SetCurrentUser(requesterId, UserRoles.User);

        var result = _subject.GetUserById(otherUserId);

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
        Assert.Equal("User is not allowed to view other users.", objectResult.Value);
        _mockUserManager.Verify(m => m.GetUserById(It.IsAny<Guid>()), Times.Never);
    }

    [Theory]
    [AutoData]
    public void GetUserById_NonAdminRequestsSelf_ReturnsUser(Guid userId)
    {
        SetCurrentUser(userId, UserRoles.User);
        var expected = SetupUserLookup(userId);

        var result = _subject.GetUserById(userId);

        Assert.Same(expected, result.Value);
    }

    [Theory]
    [AutoData]
    public void GetUserById_AdminRequestsOtherUser_ReturnsUser(Guid adminId, Guid otherUserId)
    {
        SetCurrentUser(adminId, UserRoles.Administrator);
        var expected = SetupUserLookup(otherUserId);

        var result = _subject.GetUserById(otherUserId);

        Assert.Same(expected, result.Value);
    }

    [Theory]
    [AutoData]
    public void GetUserById_ApiKeyRequestsOtherUser_ReturnsUser(Guid otherUserId)
    {
        // API keys have Administrator role and no specific user ID (Guid.Empty)
        SetCurrentUser(Guid.Empty, UserRoles.Administrator);
        var expected = SetupUserLookup(otherUserId);

        var result = _subject.GetUserById(otherUserId);

        Assert.Same(expected, result.Value);
    }

    [Theory]
    [AutoData]
    public void GetUserById_AdminRequestsUnknownUser_ReturnsNotFound(Guid adminId, Guid unknownUserId)
    {
        SetCurrentUser(adminId, UserRoles.Administrator);
        User? nullUser = null;
        _mockUserManager
            .Setup(m => m.GetUserById(unknownUserId))
            .Returns(nullUser);

        var result = _subject.GetUserById(unknownUserId);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal("User not found", notFoundResult.Value);
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
                v.ErrorMessage is not null &&
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
            v.ErrorMessage is not null &&
            v.ErrorMessage.Contains("required", StringComparison.CurrentCultureIgnoreCase));
    }

    private List<ValidationResult> Validate(object model)
    {
        var result = new List<ValidationResult>();
        var context = new ValidationContext(model, null, null);
        Validator.TryValidateObject(model, context, result, true);

        return result;
    }

    private void SetCurrentUser(Guid userId, string role)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Role, role),
            new Claim(InternalClaimTypes.UserId, userId.ToString("N", CultureInfo.InvariantCulture))
        };

        _subject.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims))
            }
        };
    }

    private UserDto SetupUserLookup(Guid userId)
    {
        var user = new User("jellyfin", "AuthenticationProviderId", "PasswordResetProviderId");
        var dto = new UserDto();

        _mockUserManager
            .Setup(m => m.GetUserById(userId))
            .Returns(user);
        _mockUserManager
            .Setup(m => m.GetUserDto(user, It.IsAny<string?>()))
            .Returns(dto);

        return dto;
    }
}
