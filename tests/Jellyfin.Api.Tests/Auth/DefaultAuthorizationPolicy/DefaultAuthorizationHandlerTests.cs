using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoMoq;
using Jellyfin.Api.Auth.DefaultAuthorizationPolicy;
using Jellyfin.Api.Constants;
using Jellyfin.Data.Entities;
using Jellyfin.Server.Implementations.Security;
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

        [Fact]
        public async Task ShouldSucceedOnApiKey()
        {
            TestHelpers.SetupConfigurationManager(_configurationManagerMock, true);

            _httpContextAccessor
                .Setup(h => h.HttpContext!.Connection.RemoteIpAddress)
                .Returns(new IPAddress(0));

            _userManagerMock
                .Setup(u => u.GetUserById(It.IsAny<Guid>()))
                .Returns<User?>(null);

            var claims = new[]
            {
                new Claim(InternalClaimTypes.IsApiKey, bool.TrueString)
            };

            var identity = new ClaimsIdentity(claims, string.Empty);
            var principal = new ClaimsPrincipal(identity);
            var context = new AuthorizationHandlerContext(_requirements, principal, null);

            await _sut.HandleAsync(context);
            Assert.True(context.HasSucceeded);
        }

        [Theory]
        [MemberData(nameof(GetParts_ValidAuthHeader_Success_Data))]
        public void GetParts_ValidAuthHeader_Success(string input, Dictionary<string, string> parts)
        {
            var dict = AuthorizationContext.GetParts(input);
            foreach (var (key, value) in parts)
            {
                Assert.Equal(dict[key], value);
            }
        }

        public static TheoryData<string, Dictionary<string, string>> GetParts_ValidAuthHeader_Success_Data()
        {
            var data = new TheoryData<string, Dictionary<string, string>>();

            data.Add(
                "x=\"123,123\",y=\"123\"",
                new Dictionary<string, string>
                {
                    { "x", "123,123" },
                    { "y", "123" }
                });

            data.Add(
                "x=\"123,123\",         y=\"123\",z=\"'hi'\"",
                new Dictionary<string, string>
                {
                    { "x", "123,123" },
                    { "y", "123" },
                    { "z", "'hi'" }
                });

            data.Add(
                "x=\"ab\"",
                new Dictionary<string, string>
                {
                    { "x", "ab" }
                });

            data.Add(
                "param=Hörbücher",
                new Dictionary<string, string>
                {
                    { "param", "Hörbücher" }
                });

            data.Add(
                "param=%22%Hörbücher",
                new Dictionary<string, string>
                {
                    { "param", "\"%Hörbücher" }
                });

            return data;
        }
    }
}
