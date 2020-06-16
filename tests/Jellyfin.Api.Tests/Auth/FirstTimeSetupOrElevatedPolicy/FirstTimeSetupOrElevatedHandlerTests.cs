using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoMoq;
using Jellyfin.Api.Auth.FirstTimeSetupOrElevatedPolicy;
using Jellyfin.Api.Constants;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;
using Jellyfin.Server.Implementations.Users;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace Jellyfin.Api.Tests.Auth.FirstTimeSetupOrElevatedPolicy
{
    public class FirstTimeSetupOrElevatedHandlerTests
    {
        /// <summary>
        /// 127.0.0.1.
        /// </summary>
        private const long InternalIp = 16777343;

        /// <summary>
        /// 1.1.1.1.
        /// </summary>
        /// private const long ExternalIp = 16843009;
        private readonly Mock<IConfigurationManager> _configurationManagerMock;
        private readonly List<IAuthorizationRequirement> _requirements;
        private readonly FirstTimeSetupOrElevatedHandler _sut;
        private readonly Mock<IUserManager> _userManagerMock;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessor;

        public FirstTimeSetupOrElevatedHandlerTests()
        {
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            _configurationManagerMock = fixture.Freeze<Mock<IConfigurationManager>>();
            _requirements = new List<IAuthorizationRequirement> { new FirstTimeSetupOrElevatedRequirement() };
            _userManagerMock = fixture.Freeze<Mock<IUserManager>>();
            _httpContextAccessor = fixture.Freeze<Mock<IHttpContextAccessor>>();

            _sut = fixture.Create<FirstTimeSetupOrElevatedHandler>();
        }

        [Theory]
        [InlineData(UserRoles.Administrator)]
        [InlineData(UserRoles.Guest)]
        [InlineData(UserRoles.User)]
        public async Task ShouldSucceedIfStartupWizardIncomplete(string userRole)
        {
            SetupConfigurationManager(false);
            var (user, claims) = SetupUser(userRole);

            _userManagerMock.Setup(u => u.GetUserById(It.IsAny<Guid>()))
                .Returns(user);

            _httpContextAccessor.Setup(h => h.HttpContext.Connection.RemoteIpAddress)
                .Returns(new IPAddress(InternalIp));

            var context = new AuthorizationHandlerContext(_requirements, claims, null);

            await _sut.HandleAsync(context);
            Assert.True(context.HasSucceeded);
        }

        [Theory]
        [InlineData(UserRoles.Administrator, true)]
        [InlineData(UserRoles.Guest, false)]
        [InlineData(UserRoles.User, false)]
        public async Task ShouldRequireAdministratorIfStartupWizardComplete(string userRole, bool shouldSucceed)
        {
            SetupConfigurationManager(true);
            var (user, claims) = SetupUser(userRole);

            _userManagerMock.Setup(u => u.GetUserById(It.IsAny<Guid>()))
                .Returns(user);

            _httpContextAccessor.Setup(h => h.HttpContext.Connection.RemoteIpAddress)
                .Returns(new IPAddress(InternalIp));

            var context = new AuthorizationHandlerContext(_requirements, claims, null);

            await _sut.HandleAsync(context);
            Assert.Equal(shouldSucceed, context.HasSucceeded);
        }

        private static (User, ClaimsPrincipal) SetupUser(string role)
        {
            var user = new User(
                "jellyfin",
                typeof(DefaultAuthenticationProvider).FullName,
                typeof(DefaultPasswordResetProvider).FullName);

            user.SetPermission(PermissionKind.IsAdministrator, role.Equals(UserRoles.Administrator, StringComparison.OrdinalIgnoreCase));
            var claims = new[]
            {
                new Claim(ClaimTypes.Role, role),
                new Claim(ClaimTypes.Name, "jellyfin"),
                new Claim(InternalClaimTypes.UserId, Guid.Empty.ToString("N", CultureInfo.InvariantCulture)),
                new Claim(InternalClaimTypes.DeviceId, Guid.Empty.ToString("N", CultureInfo.InvariantCulture)),
                new Claim(InternalClaimTypes.Device, "test"),
                new Claim(InternalClaimTypes.Client, "test"),
                new Claim(InternalClaimTypes.Version, "test"),
                new Claim(InternalClaimTypes.Token, "test"),
            };

            var identity = new ClaimsIdentity(claims);
            return (user, new ClaimsPrincipal(identity));
        }

        private void SetupConfigurationManager(bool startupWizardCompleted)
        {
            var commonConfiguration = new BaseApplicationConfiguration
            {
                IsStartupWizardCompleted = startupWizardCompleted
            };

            _configurationManagerMock.Setup(c => c.CommonConfiguration)
                .Returns(commonConfiguration);
        }
    }
}
