using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
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

        private readonly Mock<IHttpContextAccessor> _httpContextAccessor = new();
        private readonly IAuthorizationService _authorizationService;

        public LoopbackOrRequiresElevationHandlerTests()
        {
            var services = new ServiceCollection();
            services.AddAuthorizationCore();
            services.AddLogging();
            services.AddOptions();
            services.AddSingleton<IAuthorizationHandler>(new LoopbackOrRequiresElevationHandler(_httpContextAccessor.Object));
            services.AddAuthorization(options =>
            {
                options.AddPolicy(PolicyName, policy => policy.Requirements.Add(new LoopbackOrRequiresElevationRequirement()));
            });
            _authorizationService = services.BuildServiceProvider().GetRequiredService<IAuthorizationService>();
        }

        [Theory]
        [InlineData("127.0.0.1")]
        [InlineData("::1")]
        [InlineData("::ffff:127.0.0.1")]
        public async Task ShouldSucceedForLoopbackRegardlessOfRole(string ipAddress)
        {
            SetHttpContext(ipAddress);

            var result = await _authorizationService.AuthorizeAsync(CreateClaimsPrincipal(UserRoles.User), PolicyName);

            Assert.True(result.Succeeded);
        }

        [Theory]
        [InlineData("192.168.1.20", UserRoles.Administrator, true)]
        [InlineData("192.168.1.20", UserRoles.User, false)]
        [InlineData("203.0.113.10", UserRoles.Administrator, true)]
        [InlineData("203.0.113.10", UserRoles.Guest, false)]
        public async Task ShouldRequireAdministratorForOtherAddresses(string ipAddress, string userRole, bool shouldSucceed)
        {
            SetHttpContext(ipAddress);

            var result = await _authorizationService.AuthorizeAsync(CreateClaimsPrincipal(userRole), PolicyName);

            Assert.Equal(shouldSucceed, result.Succeeded);
        }

        [Theory]
        [InlineData("X-Forwarded-For", UserRoles.User, false)]
        [InlineData("Forwarded", UserRoles.Guest, false)]
        [InlineData("X-Real-IP", UserRoles.User, false)]
        [InlineData("X-Forwarded-For", UserRoles.Administrator, true)]
        public async Task ShouldRequireAdministratorForLoopbackRelayedByProxy(string header, string userRole, bool shouldSucceed)
        {
            SetHttpContext("127.0.0.1", header);

            var result = await _authorizationService.AuthorizeAsync(CreateClaimsPrincipal(userRole), PolicyName);

            Assert.Equal(shouldSucceed, result.Succeeded);
        }

        [Theory]
        [InlineData(UserRoles.Administrator, true)]
        [InlineData(UserRoles.User, false)]
        public async Task ShouldRequireAdministratorWhenHttpContextIsMissing(string userRole, bool shouldSucceed)
        {
            _httpContextAccessor.Setup(h => h.HttpContext).Returns((HttpContext?)null);

            var result = await _authorizationService.AuthorizeAsync(CreateClaimsPrincipal(userRole), PolicyName);

            Assert.Equal(shouldSucceed, result.Succeeded);
        }

        private void SetHttpContext(string ipAddress, string? header = null)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Connection.RemoteIpAddress = IPAddress.Parse(ipAddress);

            if (header is not null)
            {
                httpContext.Request.Headers[header] = "203.0.113.10";
            }

            _httpContextAccessor.Setup(h => h.HttpContext).Returns(httpContext);
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
