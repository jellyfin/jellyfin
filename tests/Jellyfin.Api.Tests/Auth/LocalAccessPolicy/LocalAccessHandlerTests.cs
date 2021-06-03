using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoMoq;
using Jellyfin.Api.Auth.LocalAccessPolicy;
using Jellyfin.Api.Constants;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace Jellyfin.Api.Tests.Auth.LocalAccessPolicy
{
    public class LocalAccessHandlerTests
    {
        private readonly Mock<IConfigurationManager> _configurationManagerMock;
        private readonly List<IAuthorizationRequirement> _requirements;
        private readonly LocalAccessHandler _sut;
        private readonly Mock<IUserManager> _userManagerMock;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessor;
        private readonly Mock<INetworkManager> _networkManagerMock;

        public LocalAccessHandlerTests()
        {
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            _configurationManagerMock = fixture.Freeze<Mock<IConfigurationManager>>();
            _requirements = new List<IAuthorizationRequirement> { new LocalAccessRequirement() };
            _userManagerMock = fixture.Freeze<Mock<IUserManager>>();
            _httpContextAccessor = fixture.Freeze<Mock<IHttpContextAccessor>>();
            _networkManagerMock = fixture.Freeze<Mock<INetworkManager>>();

            _sut = fixture.Create<LocalAccessHandler>();
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(false, false)]
        public async Task LocalAccessOnly(bool isInLocalNetwork, bool shouldSucceed)
        {
            _networkManagerMock
                .Setup(n => n.IsInLocalNetwork(It.IsAny<IPAddress>()))
                .Returns(isInLocalNetwork);

            TestHelpers.SetupConfigurationManager(_configurationManagerMock, true);
            var claims = TestHelpers.SetupUser(
                _userManagerMock,
                _httpContextAccessor,
                UserRoles.User);

            var context = new AuthorizationHandlerContext(_requirements, claims, null);
            await _sut.HandleAsync(context);
            Assert.Equal(shouldSucceed, context.HasSucceeded);
        }
    }
}
