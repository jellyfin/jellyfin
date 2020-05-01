using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoMoq;
using Jellyfin.Api.Auth.FirstTimeSetupOrElevatedPolicy;
using Jellyfin.Api.Constants;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Configuration;
using Microsoft.AspNetCore.Authorization;
using Moq;
using Xunit;

namespace Jellyfin.Api.Tests.Auth.FirstTimeSetupOrElevatedPolicy
{
    public class FirstTimeSetupOrElevatedHandlerTests
    {
        private readonly Mock<IConfigurationManager> _configurationManagerMock;
        private readonly List<IAuthorizationRequirement> _requirements;
        private readonly FirstTimeSetupOrElevatedHandler _sut;

        public FirstTimeSetupOrElevatedHandlerTests()
        {
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            _configurationManagerMock = fixture.Freeze<Mock<IConfigurationManager>>();
            _requirements = new List<IAuthorizationRequirement> { new FirstTimeSetupOrElevatedRequirement() };

            _sut = fixture.Create<FirstTimeSetupOrElevatedHandler>();
        }

        [Theory]
        [InlineData(UserRoles.Administrator)]
        [InlineData(UserRoles.Guest)]
        [InlineData(UserRoles.User)]
        public async Task ShouldSucceedIfStartupWizardIncomplete(string userRole)
        {
            SetupConfigurationManager(false);
            var user = SetupUser(userRole);
            var context = new AuthorizationHandlerContext(_requirements, user, null);

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
            var user = SetupUser(userRole);
            var context = new AuthorizationHandlerContext(_requirements, user, null);

            await _sut.HandleAsync(context);
            Assert.Equal(shouldSucceed, context.HasSucceeded);
        }

        private static ClaimsPrincipal SetupUser(string role)
        {
            var claims = new[] { new Claim(ClaimTypes.Role, role) };
            var identity = new ClaimsIdentity(claims);
            return new ClaimsPrincipal(identity);
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
