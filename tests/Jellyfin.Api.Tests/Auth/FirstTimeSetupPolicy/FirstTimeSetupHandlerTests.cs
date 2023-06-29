using System.Collections.Generic;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoMoq;
using Jellyfin.Api.Auth.FirstTimeSetupPolicy;
using Jellyfin.Api.Constants;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace Jellyfin.Api.Tests.Auth.FirstTimeSetupPolicy
{
    public class FirstTimeSetupHandlerTests
    {
        private readonly Mock<IConfigurationManager> _configurationManagerMock;
        private readonly List<IAuthorizationRequirement> _requirements;
        private readonly FirstTimeSetupHandler _firstTimeSetupHandler;
        private readonly Mock<IUserManager> _userManagerMock;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessor;

        public FirstTimeSetupHandlerTests()
        {
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            _configurationManagerMock = fixture.Freeze<Mock<IConfigurationManager>>();
            _requirements = new List<IAuthorizationRequirement> { new FirstTimeSetupRequirement() };
            _userManagerMock = fixture.Freeze<Mock<IUserManager>>();
            _httpContextAccessor = fixture.Freeze<Mock<IHttpContextAccessor>>();

            _firstTimeSetupHandler = fixture.Create<FirstTimeSetupHandler>();
        }

        [Theory]
        [InlineData(UserRoles.Administrator)]
        [InlineData(UserRoles.Guest)]
        [InlineData(UserRoles.User)]
        public async Task ShouldSucceedIfStartupWizardIncomplete(string userRole)
        {
            TestHelpers.SetupConfigurationManager(_configurationManagerMock, false);
            var claims = TestHelpers.SetupUser(
                _userManagerMock,
                _httpContextAccessor,
                userRole);

            var context = new AuthorizationHandlerContext(_requirements, claims, null);

            await _firstTimeSetupHandler.HandleAsync(context);
            Assert.True(context.HasSucceeded);
        }

        [Theory]
        [InlineData(UserRoles.Administrator, true)]
        [InlineData(UserRoles.Guest, false)]
        [InlineData(UserRoles.User, false)]
        public async Task ShouldRequireAdministratorIfStartupWizardComplete(string userRole, bool shouldSucceed)
        {
            TestHelpers.SetupConfigurationManager(_configurationManagerMock, true);
            var claims = TestHelpers.SetupUser(
                _userManagerMock,
                _httpContextAccessor,
                userRole);

            var context = new AuthorizationHandlerContext(_requirements, claims, null);

            await _firstTimeSetupHandler.HandleAsync(context);
            Assert.Equal(shouldSucceed, context.HasSucceeded);
        }
    }
}
