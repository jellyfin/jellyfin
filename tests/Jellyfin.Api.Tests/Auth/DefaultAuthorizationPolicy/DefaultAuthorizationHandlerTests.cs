using System.Collections.Generic;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoMoq;
using Jellyfin.Api.Auth.DefaultAuthorizationPolicy;
using Jellyfin.Api.Constants;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace Jellyfin.Api.Tests.Auth.DefaultAuthorizationPolicy
{
    public class DefaultAuthorizationHandlerTests
    {
        private readonly Mock<IConfigurationManager> _configurationManagerMock;
        private readonly List<IAuthorizationRequirement> _requirements;
        private readonly DefaultAuthorizationHandler _sut;
        private readonly Mock<IUserManager> _userManagerMock;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessor;

        public DefaultAuthorizationHandlerTests()
        {
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            _configurationManagerMock = fixture.Freeze<Mock<IConfigurationManager>>();
            _requirements = new List<IAuthorizationRequirement> { new DefaultAuthorizationRequirement() };
            _userManagerMock = fixture.Freeze<Mock<IUserManager>>();
            _httpContextAccessor = fixture.Freeze<Mock<IHttpContextAccessor>>();

            _sut = fixture.Create<DefaultAuthorizationHandler>();
        }

        [Theory]
        [InlineData(UserRoles.Administrator)]
        [InlineData(UserRoles.Guest)]
        [InlineData(UserRoles.User)]
        public async Task ShouldSucceedOnUser(string userRole)
        {
            TestHelpers.SetupConfigurationManager(_configurationManagerMock, true);
            var claims = TestHelpers.SetupUser(
                _userManagerMock,
                _httpContextAccessor,
                userRole);

            var context = new AuthorizationHandlerContext(_requirements, claims, null);

            await _sut.HandleAsync(context);
            Assert.True(context.HasSucceeded);
        }
    }
}
