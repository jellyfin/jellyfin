using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Jellyfin.Api.Auth.RequiresElevationPolicy;
using Jellyfin.Api.Constants;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace Jellyfin.Api.Tests.Auth.RequiresElevationPolicy
{
    public class RequiresElevationHandlerTests
    {
        private readonly RequiresElevationHandler _sut;

        public RequiresElevationHandlerTests()
        {
            _sut = new RequiresElevationHandler();
        }

        [Theory]
        [InlineData(UserRoles.Administrator, true)]
        [InlineData(UserRoles.User, false)]
        [InlineData(UserRoles.Guest, false)]
        public async Task ShouldHandleRolesCorrectly(string role, bool shouldSucceed)
        {
            var requirements = new List<IAuthorizationRequirement> { new RequiresElevationRequirement() };

            var claims = new[] { new Claim(ClaimTypes.Role, role) };
            var identity = new ClaimsIdentity(claims);
            var user = new ClaimsPrincipal(identity);

            var context = new AuthorizationHandlerContext(requirements, user, null);

            await _sut.HandleAsync(context);
            Assert.Equal(shouldSucceed, context.HasSucceeded);
        }
    }
}
