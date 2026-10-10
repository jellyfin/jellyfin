using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Jellyfin.Api.Auth.LoopbackAccessPolicy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Jellyfin.Api.Tests.Auth.LoopbackAccessPolicy
{
    public class LoopbackAccessHandlerTests
    {
        private const string PolicyName = "LoopbackAccess";

        private readonly Mock<IHttpContextAccessor> _httpContextAccessor = new();
        private readonly IAuthorizationService _authorizationService;

        public LoopbackAccessHandlerTests()
        {
            var services = new ServiceCollection();
            services.AddAuthorizationCore();
            services.AddLogging();
            services.AddOptions();
            services.AddSingleton<IAuthorizationHandler>(new LoopbackAccessHandler(_httpContextAccessor.Object));
            services.AddAuthorization(options =>
            {
                options.AddPolicy(PolicyName, policy => policy.Requirements.Add(new LoopbackAccessRequirement()));
            });
            _authorizationService = services.BuildServiceProvider().GetRequiredService<IAuthorizationService>();
        }

        [Theory]
        [InlineData("127.0.0.1")]
        [InlineData("::1")]
        [InlineData("::ffff:127.0.0.1")]
        public async Task ShouldSucceedForLoopback(string ipAddress)
        {
            SetHttpContext(ipAddress);

            var result = await _authorizationService.AuthorizeAsync(new ClaimsPrincipal(new ClaimsIdentity()), PolicyName);

            Assert.True(result.Succeeded);
        }

        [Theory]
        [InlineData("192.168.1.20")]
        [InlineData("10.0.0.5")]
        [InlineData("203.0.113.10")]
        [InlineData("2001:db8::1")]
        public async Task ShouldFailForOtherAddresses(string ipAddress)
        {
            SetHttpContext(ipAddress);

            var result = await _authorizationService.AuthorizeAsync(new ClaimsPrincipal(new ClaimsIdentity()), PolicyName);

            Assert.False(result.Succeeded);
        }

        [Theory]
        [InlineData("X-Forwarded-For")]
        [InlineData("Forwarded")]
        [InlineData("X-Real-IP")]
        public async Task ShouldFailForLoopbackRelayedByProxy(string header)
        {
            SetHttpContext("127.0.0.1", header);

            var result = await _authorizationService.AuthorizeAsync(new ClaimsPrincipal(new ClaimsIdentity()), PolicyName);

            Assert.False(result.Succeeded);
        }

        [Fact]
        public async Task ShouldFailWhenHttpContextIsMissing()
        {
            _httpContextAccessor.Setup(h => h.HttpContext).Returns((HttpContext?)null);

            var result = await _authorizationService.AuthorizeAsync(new ClaimsPrincipal(new ClaimsIdentity()), PolicyName);

            Assert.False(result.Succeeded);
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
    }
}
