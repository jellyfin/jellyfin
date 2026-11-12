using System;
using System.Globalization;
using System.Security.Claims;
using System.Threading.Tasks;
using Jellyfin.Api.Auth.ShareLinkPolicy;
using Jellyfin.Api.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace Jellyfin.Api.Tests.Auth
{
    public class ShareLinkHandlerTests
    {
        [Fact]
        public async Task NoShareClaim_Authenticated_Succeeds()
        {
            var context = await AuthorizeAsync(CreatePrincipal(), null);

            Assert.True(context.HasSucceeded);
        }

        [Fact]
        public async Task ShareClaim_MatchingRouteItem_Succeeds()
        {
            var itemId = Guid.NewGuid();
            var context = await AuthorizeAsync(CreateSharePrincipal(itemId), itemId);

            Assert.True(context.HasSucceeded);
        }

        [Fact]
        public async Task ShareClaim_MismatchedRouteItem_Fails()
        {
            var context = await AuthorizeAsync(CreateSharePrincipal(Guid.NewGuid()), Guid.NewGuid());

            Assert.False(context.HasSucceeded);
        }

        [Fact]
        public async Task NoShareClaim_Unauthenticated_Fails()
        {
            var context = await AuthorizeAsync(new ClaimsPrincipal(new ClaimsIdentity()), null);

            Assert.False(context.HasSucceeded);
        }

        private static async Task<AuthorizationHandlerContext> AuthorizeAsync(ClaimsPrincipal user, Guid? routeItemId)
        {
            var httpContext = new DefaultHttpContext();
            if (routeItemId.HasValue)
            {
                httpContext.Request.RouteValues["itemId"] = routeItemId.Value.ToString("N", CultureInfo.InvariantCulture);
            }

            var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            httpContextAccessorMock.Setup(a => a.HttpContext).Returns(httpContext);

            var handler = new ShareLinkHandler(httpContextAccessorMock.Object);
            var context = new AuthorizationHandlerContext(
                new[] { new ShareLinkRequirement() },
                user,
                httpContext);

            await handler.HandleAsync(context);

            return context;
        }

        private static ClaimsPrincipal CreatePrincipal()
            => new(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "user") }, "Test"));

        private static ClaimsPrincipal CreateSharePrincipal(Guid itemId)
            => new(new ClaimsIdentity(
                new[]
                {
                    new Claim(ClaimTypes.Name, "user"),
                    new Claim(InternalClaimTypes.IsShareLink, bool.TrueString),
                    new Claim(InternalClaimTypes.ShareItemId, itemId.ToString("N", CultureInfo.InvariantCulture))
                },
                "ShareLink"));
    }
}
