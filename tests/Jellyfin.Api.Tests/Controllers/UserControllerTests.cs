using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoFixture.Xunit3;
using Jellyfin.Api.Constants;
using Jellyfin.Api.Controllers;
using Jellyfin.Api.Models.UserDtos;
using Jellyfin.Data;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Authentication;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Devices;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Controller.QuickConnect;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Users;
using Microsoft.AspNetCore.Http;
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
    private readonly Mock<ILibraryManager> _mockLibraryManager;

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
        _mockLibraryManager = new Mock<ILibraryManager>();
        _mockLibraryManager.Setup(manager => manager.GetVirtualFolders()).Returns([]);
        _mockServerConfigurationManager
            .SetupGet(manager => manager.Configuration)
            .Returns(new ServerConfiguration());
        _mockUserManager
            .Setup(manager => manager.GetUsers())
            .Returns(Array.Empty<User>());
        _mockNetworkManager
            .Setup(manager => manager.IsInLocalNetwork(It.IsAny<IPAddress>()))
            .Returns(true);
        _mockAuthorizationContext
            .Setup(context => context.GetAuthorizationInfo(It.IsAny<HttpRequest>()))
            .ReturnsAsync(new AuthorizationInfo
            {
                Client = "test",
                Version = "1",
                DeviceId = "device-id",
                Device = "device"
            });

        _subject = new UserController(
            _mockUserManager.Object,
            _mockSessionManager.Object,
            _mockNetworkManager.Object,
            _mockDeviceManager.Object,
            _mockAuthorizationContext.Object,
            _mockServerConfigurationManager.Object,
            _mockLogger.Object,
            _mockQuickConnect.Object,
            _mockPlaylistManager.Object,
            _mockLibraryManager.Object);
        _subject.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        _subject.HttpContext.Connection.RemoteIpAddress = IPAddress.Loopback;
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

    [Theory]
    [InlineData(null, false)]
    [InlineData("short", false)]
    [InlineData("password", true)]
    public async Task UpdateUserPassword_PublicSelfServiceWeakPassword_ReturnsBadRequest(
        string? newPassword,
        bool resetPassword)
    {
        var user = new User("public-user", "auth", "reset");
        user.SetPermission(PermissionKind.IsPubliclyRegistered, true);
        SetAuthenticatedUser(user, UserRoles.User);
        _mockUserManager
            .Setup(manager => manager.GetUserById(user.Id))
            .Returns(user);
        _mockServerConfigurationManager
            .SetupGet(manager => manager.Configuration)
            .Returns(new ServerConfiguration
            {
                PublicUserRegistrationMinimumPasswordLength = 8
            });

        var result = await _subject.UpdateUserPassword(user.Id, new UpdateUserPassword
        {
            CurrentPw = "current-password",
            NewPw = newPassword,
            ResetPassword = resetPassword
        });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Password must be at least 8 characters long.", badRequest.Value);
        _mockUserManager.Verify(
            manager => manager.AuthenticateUser(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>()),
            Times.Never);
        _mockUserManager.Verify(
            manager => manager.ChangePassword(It.IsAny<Guid>(), It.IsAny<string>()),
            Times.Never);
        _mockUserManager.Verify(
            manager => manager.ResetPassword(It.IsAny<Guid>()),
            Times.Never);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("short", false)]
    [InlineData("password", true)]
    public async Task UpdateUserPassword_PublicAccountChangedByAdministratorWeakPassword_ReturnsBadRequest(
        string? newPassword,
        bool resetPassword)
    {
        var user = new User("public-user", "auth", "reset");
        user.SetPermission(PermissionKind.IsPubliclyRegistered, true);
        var administrator = new User("administrator", "auth", "reset");
        SetAuthenticatedUser(administrator, UserRoles.Administrator);
        _mockUserManager
            .Setup(manager => manager.GetUserById(user.Id))
            .Returns(user);
        _mockServerConfigurationManager
            .SetupGet(manager => manager.Configuration)
            .Returns(new ServerConfiguration
            {
                PublicUserRegistrationMinimumPasswordLength = 8
            });

        var result = await _subject.UpdateUserPassword(user.Id, new UpdateUserPassword
        {
            NewPw = newPassword,
            ResetPassword = resetPassword
        });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Password must be at least 8 characters long.", badRequest.Value);
        _mockUserManager.Verify(
            manager => manager.ChangePassword(It.IsAny<Guid>(), It.IsAny<string>()),
            Times.Never);
        _mockUserManager.Verify(
            manager => manager.ResetPassword(It.IsAny<Guid>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateUserPassword_PublicSelfServiceMinimumLength_Succeeds()
    {
        var user = new User("public-user", "auth", "reset");
        user.SetPermission(PermissionKind.IsPubliclyRegistered, true);
        SetAuthenticatedUser(user, UserRoles.User);
        _mockUserManager
            .Setup(manager => manager.GetUserById(user.Id))
            .Returns(user);
        _mockUserManager
            .Setup(manager => manager.AuthenticateUser(
                user.Username,
                "current-password",
                It.IsAny<string>(),
                false))
            .ReturnsAsync(user);
        _mockUserManager
            .Setup(manager => manager.ChangePassword(user.Id, "password"))
            .Returns(Task.CompletedTask);
        _mockSessionManager
            .Setup(manager => manager.RevokeUserTokens(user.Id, "test-token"))
            .Returns(Task.CompletedTask);
        _mockServerConfigurationManager
            .SetupGet(manager => manager.Configuration)
            .Returns(new ServerConfiguration
            {
                PublicUserRegistrationMinimumPasswordLength = 8
            });

        var result = await _subject.UpdateUserPassword(user.Id, new UpdateUserPassword
        {
            CurrentPw = "current-password",
            NewPw = "password"
        });

        Assert.IsType<NoContentResult>(result);
        _mockUserManager.Verify(
            manager => manager.ChangePassword(user.Id, "password"),
            Times.Once);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UpdateUserPassword_NonPublicSelfServiceEmptyPassword_PreservesLegacyBehavior(
        bool resetPassword)
    {
        var user = new User("local-user", "auth", "reset");
        SetAuthenticatedUser(user, UserRoles.User);
        _mockUserManager
            .Setup(manager => manager.GetUserById(user.Id))
            .Returns(user);
        _mockUserManager
            .Setup(manager => manager.AuthenticateUser(
                user.Username,
                "current-password",
                It.IsAny<string>(),
                false))
            .ReturnsAsync(user);
        _mockUserManager
            .Setup(manager => manager.ChangePassword(user.Id, string.Empty))
            .Returns(Task.CompletedTask);
        _mockUserManager
            .Setup(manager => manager.ResetPassword(user.Id))
            .Returns(Task.CompletedTask);
        _mockSessionManager
            .Setup(manager => manager.RevokeUserTokens(user.Id, "test-token"))
            .Returns(Task.CompletedTask);

        var result = await _subject.UpdateUserPassword(user.Id, new UpdateUserPassword
        {
            CurrentPw = "current-password",
            ResetPassword = resetPassword
        });

        Assert.IsType<NoContentResult>(result);
        if (resetPassword)
        {
            _mockUserManager.Verify(
                manager => manager.ResetPassword(user.Id),
                Times.Once);
        }
        else
        {
            _mockUserManager.Verify(
                manager => manager.ChangePassword(user.Id, string.Empty),
                Times.Once);
        }
    }

    [Fact]
    public async Task RegisterUser_WhenSessionCreationFails_DeletesCreatedUser()
    {
        var user = new User("registration", "auth", "reset");
        _mockServerConfigurationManager
            .SetupGet(manager => manager.Configuration)
            .Returns(new ServerConfiguration
            {
                EnablePublicUserRegistration = true,
                PublicUserRegistrationMinimumPasswordLength = 8
            });
        _mockAuthorizationContext
            .Setup(context => context.GetAuthorizationInfo(It.IsAny<HttpRequest>()))
            .ReturnsAsync(new AuthorizationInfo
            {
                Client = "test",
                Version = "1",
                DeviceId = "device-id",
                Device = "device"
            });
        _mockUserManager
            .Setup(manager => manager.CreateUserAsync("registration"))
            .ReturnsAsync(user);
        _mockUserManager
            .Setup(manager => manager.UpdateUserAsync(user))
            .Returns(Task.CompletedTask);
        _mockUserManager
            .Setup(manager => manager.GetUserDto(user, It.IsAny<string>()))
            .Returns(CreateUserDto(user));
        _mockUserManager
            .Setup(manager => manager.UpdatePolicyAsync(user.Id, It.IsAny<UserPolicy>()))
            .Returns(Task.CompletedTask);
        _mockUserManager
            .Setup(manager => manager.ChangePassword(user.Id, "password"))
            .Returns(Task.CompletedTask);
        _mockUserManager
            .Setup(manager => manager.DeleteUserAsync(user.Id))
            .Returns(Task.CompletedTask);
        _mockSessionManager
            .Setup(manager => manager.AuthenticateNewSession(It.IsAny<AuthenticationRequest>()))
            .ThrowsAsync(new InvalidOperationException("Session failed."));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _subject.RegisterUser(new CreateUserByName
        {
            Name = "registration",
            Password = "password"
        }));

        _mockUserManager.Verify(manager => manager.DeleteUserAsync(user.Id), Times.Once);
    }

    [Fact]
    public async Task RegisterUser_AppliesSafePublicPolicy()
    {
        var user = new User("safe-registration", "auth", "reset");
        var administrator = new User("administrator", "auth", "reset");
        administrator.SetPermission(PermissionKind.IsAdministrator, true);
        var configuration = new ServerConfiguration
        {
            EnablePublicUserRegistration = true,
            PublicUserRegistrationMinimumPasswordLength = 8,
            PublicUserRegistrationMaxUsers = 1,
            PublicUserRegistrationMaxActiveSessions = 2,
            PublicUserRegistrationRemoteClientBitrateLimit = 6_000_000
        };
        UserPolicy? appliedPolicy = null;
        _mockServerConfigurationManager
            .SetupGet(manager => manager.Configuration)
            .Returns(configuration);
        _mockUserManager
            .Setup(manager => manager.GetUsers())
            .Returns(new[] { administrator });
        _mockUserManager
            .Setup(manager => manager.CreateUserAsync(user.Username))
            .ReturnsAsync(user);
        _mockUserManager
            .Setup(manager => manager.UpdateUserAsync(user))
            .Returns(Task.CompletedTask);
        _mockUserManager
            .Setup(manager => manager.GetUserDto(user, It.IsAny<string>()))
            .Returns(CreateUserDto(user));
        _mockUserManager
            .Setup(manager => manager.UpdatePolicyAsync(user.Id, It.IsAny<UserPolicy>()))
            .Callback<Guid, UserPolicy>((_, policy) => appliedPolicy = policy)
            .Returns(Task.CompletedTask);
        _mockUserManager
            .Setup(manager => manager.ChangePassword(user.Id, "password"))
            .Returns(Task.CompletedTask);
        _mockSessionManager
            .Setup(manager => manager.AuthenticateNewSession(It.IsAny<AuthenticationRequest>()))
            .ReturnsAsync(new AuthenticationResult());
        var movieLibraryId = Guid.NewGuid();
        _mockLibraryManager
            .Setup(manager => manager.GetVirtualFolders())
            .Returns(
            [
                new VirtualFolderInfo
                {
                    ItemId = movieLibraryId.ToString(),
                    CollectionType = CollectionTypeOptions.movies
                },
                new VirtualFolderInfo
                {
                    ItemId = Guid.NewGuid().ToString(),
                    CollectionType = CollectionTypeOptions.music
                }
            ]);

        await _subject.RegisterUser(new CreateUserByName
        {
            Name = user.Username,
            Password = "password"
        });

        Assert.NotNull(appliedPolicy);
        Assert.False(appliedPolicy.IsAdministrator);
        Assert.False(appliedPolicy.EnableContentDownloading);
        Assert.False(appliedPolicy.EnableMediaConversion);
        Assert.False(appliedPolicy.EnableSyncTranscoding);
        Assert.False(appliedPolicy.EnablePublicSharing);
        Assert.False(appliedPolicy.EnableSharedDeviceControl);
        Assert.False(appliedPolicy.EnableRemoteControlOfOtherUsers);
        Assert.False(appliedPolicy.EnableAllFolders);
        Assert.Equal([movieLibraryId], appliedPolicy.EnabledFolders);
        Assert.Equal(SyncPlayUserAccessType.None, appliedPolicy.SyncPlayAccess);
        Assert.Equal(2, appliedPolicy.MaxActiveSessions);
        Assert.Equal(6_000_000, appliedPolicy.RemoteClientBitrateLimit);
        Assert.True(user.HasPermission(PermissionKind.IsPubliclyRegistered));
        _mockUserManager.Verify(
            manager => manager.UpdateUserAsync(
                It.Is<User>(updated => updated.HasPermission(PermissionKind.IsPubliclyRegistered))),
            Times.Once);
    }

    [Fact]
    public async Task RegisterUser_WhenRegularUserCapacityReached_ReturnsConflict()
    {
        var existingUser = new User("existing", "auth", "reset");
        _mockServerConfigurationManager
            .SetupGet(manager => manager.Configuration)
            .Returns(new ServerConfiguration
            {
                EnablePublicUserRegistration = true,
                PublicUserRegistrationMaxUsers = 1
            });
        _mockUserManager
            .Setup(manager => manager.GetUsers())
            .Returns(new[] { existingUser });

        var result = await _subject.RegisterUser(new CreateUserByName
        {
            Name = "capacity-registration",
            Password = "password"
        });

        var conflict = Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Equal("Public user registration capacity has been reached.", conflict.Value);
        _mockUserManager.Verify(
            manager => manager.CreateUserAsync(It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task AuthenticateUserByName_AfterRemoteFailure_ReturnsTooManyRequests()
    {
        var username = "remote-" + Guid.NewGuid().ToString("N");
        _subject.HttpContext.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.42");
        _mockNetworkManager
            .Setup(manager => manager.IsInLocalNetwork(It.IsAny<IPAddress>()))
            .Returns(false);
        _mockServerConfigurationManager
            .SetupGet(manager => manager.Configuration)
            .Returns(new ServerConfiguration
            {
                PublicUserLoginMaxFailedAttempts = 1,
                PublicUserLoginFailureWindowSeconds = 900
            });
        _mockSessionManager
            .Setup(manager => manager.AuthenticateNewSession(It.IsAny<AuthenticationRequest>()))
            .ThrowsAsync(new SecurityException("Invalid credentials."));

        await Assert.ThrowsAsync<SecurityException>(
            () => _subject.AuthenticateUserByName(new AuthenticateUserByName
            {
                Username = username,
                Pw = "wrong"
            }));
        var result = await _subject.AuthenticateUserByName(new AuthenticateUserByName
        {
            Username = username,
            Pw = "wrong"
        });

        var rateLimited = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status429TooManyRequests, rateLimited.StatusCode);
        Assert.True(_subject.Response.Headers.ContainsKey("Retry-After"));
        _mockSessionManager.Verify(
            manager => manager.AuthenticateNewSession(It.IsAny<AuthenticationRequest>()),
            Times.Once);
    }

    [Fact]
    public async Task AuthenticateUserByName_LanRequestsAreNotRateLimited()
    {
        var username = "lan-" + Guid.NewGuid().ToString("N");
        _mockServerConfigurationManager
            .SetupGet(manager => manager.Configuration)
            .Returns(new ServerConfiguration
            {
                PublicUserLoginMaxFailedAttempts = 1,
                PublicUserLoginFailureWindowSeconds = 900
            });
        _mockSessionManager
            .Setup(manager => manager.AuthenticateNewSession(It.IsAny<AuthenticationRequest>()))
            .ThrowsAsync(new SecurityException("Invalid credentials."));

        for (var attempt = 0; attempt < 2; attempt++)
        {
            await Assert.ThrowsAsync<SecurityException>(
                () => _subject.AuthenticateUserByName(new AuthenticateUserByName
                {
                    Username = username,
                    Pw = "wrong"
                }));
        }

        _mockSessionManager.Verify(
            manager => manager.AuthenticateNewSession(It.IsAny<AuthenticationRequest>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task AuthenticateUserByName_RemoteAdministratorIsNotAccountBlocked()
    {
        var username = "administrator-" + Guid.NewGuid().ToString("N");
        var administrator = new User(username, "auth", "reset");
        administrator.SetPermission(PermissionKind.IsAdministrator, true);
        _mockNetworkManager
            .Setup(manager => manager.IsInLocalNetwork(It.IsAny<IPAddress>()))
            .Returns(false);
        _mockServerConfigurationManager
            .SetupGet(manager => manager.Configuration)
            .Returns(new ServerConfiguration
            {
                PublicUserLoginMaxFailedAttempts = 1,
                PublicUserLoginFailureWindowSeconds = 900
            });
        _mockUserManager
            .Setup(manager => manager.GetUserByName(username))
            .Returns(administrator);
        _mockSessionManager
            .Setup(manager => manager.AuthenticateNewSession(It.IsAny<AuthenticationRequest>()))
            .ThrowsAsync(new SecurityException("Invalid credentials."));

        _subject.HttpContext.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.43");
        await Assert.ThrowsAsync<SecurityException>(
            () => _subject.AuthenticateUserByName(new AuthenticateUserByName
            {
                Username = username,
                Pw = "wrong"
            }));
        _subject.HttpContext.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.44");
        await Assert.ThrowsAsync<SecurityException>(
            () => _subject.AuthenticateUserByName(new AuthenticateUserByName
            {
                Username = username,
                Pw = "wrong"
            }));

        _mockSessionManager.Verify(
            manager => manager.AuthenticateNewSession(It.IsAny<AuthenticationRequest>()),
            Times.Exactly(2));
    }

    private static UserDto CreateUserDto(User user)
        => new()
        {
            Name = user.Username,
            Id = user.Id,
            Policy = new UserPolicy
            {
                AuthenticationProviderId = user.AuthenticationProviderId,
                PasswordResetProviderId = user.PasswordResetProviderId
            }
        };

    private void SetAuthenticatedUser(User user, string role)
    {
        _subject.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Role, role),
                new Claim(
                    InternalClaimTypes.UserId,
                    user.Id.ToString("N", CultureInfo.InvariantCulture)),
                new Claim(InternalClaimTypes.Token, "test-token")
            ],
            "Test"));
    }

    private List<ValidationResult> Validate(object model)
    {
        var result = new List<ValidationResult>();
        var context = new ValidationContext(model, null, null);
        Validator.TryValidateObject(model, context, result, true);

        return result;
    }
}
