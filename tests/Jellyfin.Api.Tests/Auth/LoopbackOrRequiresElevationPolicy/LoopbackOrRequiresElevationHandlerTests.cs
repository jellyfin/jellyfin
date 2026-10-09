using System.Collections.Generic;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoMoq;
using Jellyfin.Api.Auth.LoopbackOrRequiresElevationPolicy;
using Jellyfin.Api.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Jellyfin.Api.Tests.Auth.LoopbackOrRequiresElevationPolicy
{
    public class LoopbackOrRequiresElevationHandlerTests
    {
        private const string PolicyName = "LoopbackOrRequiresElevation";

        private readonly Mock<IHttpContextAccessor> _httpContextAccessor;
        private readonly IAuthorizationService _authorizationService;

        public LoopbackOrRequiresElevationHandlerTests()
        {
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            _httpContextAccessor = fixture.Freeze<Mock<IHttpContextAccessor>>();

            var handler = fixture.Create<LoopbackOrRequiresElevationHandler>();

            var services = new ServiceCollection();
            services.AddAuthorizationCore();
            services.AddLogging();
            services.AddOptions();
            services.AddSingleton<IAuthorizationHandler>(handler);
            services.AddAuthorization(options =>
            {
                options.AddPolicy(PolicyName, policy => policy.Requirements.Add(new LoopbackOrRequiresElevationRequirement()));
            });
            _authorizationService = services.BuildServiceProvider().GetRequiredService<IAuthorizationService>();
        }

        [Theory]
        [InlineData("127.0.0.1")]
        [InlineData("::1")]
        public async Task ShouldSucceedForLoopbackRegardlessOfRole(string ipAddress)
        {
            SetRemoteIp(IPAddress.Parse(ipAddress));
            var claims = CreateClaimsPrincipal(UserRoles.User);

            var result = await _authorizationService.AuthorizeAsync(claims, PolicyName);

            Assert.True(result.Succeeded);
        }

        [Fact]
        public async Task ShouldSucceedWhenHttpContextIsMissing()
        {
            _httpContextAccessor.Setup(h => h.HttpContext).Returns((HttpContext?)null);
            var claims = CreateClaimsPrincipal(UserRoles.User);

            var result = await _authorizationService.AuthorizeAsync(claims, PolicyName);

            Assert.True(result.Succeeded);
        }

        [Theory]
        [InlineData(UserRoles.Administrator, true)]
        [InlineData(UserRoles.User, false)]
        [InlineData(UserRoles.Guest, false)]
        public async Task ShouldRequireAdministratorForRemoteAccess(string userRole, bool shouldSucceed)
        {
            SetRemoteIp(IPAddress.Parse("203.0.113.10"));
            var claims = CreateClaimsPrincipal(userRole);

            var result = await _authorizationService.AuthorizeAsync(claims, PolicyName);

            Assert.Equal(shouldSucceed, result.Succeeded);
        }

        private void SetRemoteIp(IPAddress ipAddress)
        {
            _httpContextAccessor
                .Setup(h => h.HttpContext!.Connection.RemoteIpAddress)
                .Returns(ipAddress);
        }

        private static ClaimsPrincipal CreateClaimsPrincipal(string role)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.Role, role),
                new Claim(ClaimTypes.Name, "jellyfin")
            };

            return new ClaimsPrincipal(new ClaimsIdentity(claims));
        }
    }
}
