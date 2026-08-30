using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoMoq;
using Jellyfin.Api.Auth.DefaultAuthorizationPolicy;
using Jellyfin.Api.Constants;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace Jellyfin.Api.Tests.Auth.DefaultAuthorizationPolicy
{
    /// <summary>
    /// A parental-schedule rejection must identify itself with the
    /// <c>X-Application-Error-Code</c> header. Without it the resulting 403 is
    /// indistinguishable from an ordinary permission denial, and clients render it
    /// as an empty library instead of an actionable message.
    /// </summary>
    public class ParentalControlErrorCodeTests
    {
        private readonly Mock<IConfigurationManager> _configurationManagerMock;
        private readonly List<IAuthorizationRequirement> _requirements;
        private readonly DefaultAuthorizationHandler _sut;
        private readonly Mock<IUserManager> _userManagerMock;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessor;

        /// <summary>
        /// Globally disallow access.
        /// </summary>
        private readonly AccessSchedule[] _disallowedSchedule = { new AccessSchedule(DynamicDayOfWeek.Everyday, 0, 0, Guid.Empty) };

        /// <summary>
        /// Allow access at any hour of any day.
        /// </summary>
        private readonly AccessSchedule[] _allowedSchedule = { new AccessSchedule(DynamicDayOfWeek.Everyday, 0, 24, Guid.Empty) };

        public ParentalControlErrorCodeTests()
        {
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            _configurationManagerMock = fixture.Freeze<Mock<IConfigurationManager>>();
            _requirements = new List<IAuthorizationRequirement> { new DefaultAuthorizationRequirement() };
            _userManagerMock = fixture.Freeze<Mock<IUserManager>>();
            _httpContextAccessor = fixture.Freeze<Mock<IHttpContextAccessor>>();

            _sut = fixture.Create<DefaultAuthorizationHandler>();
        }

        [Fact]
        public async Task ShouldSetErrorCodeHeaderWhenOutsideAccessSchedule()
        {
            var (httpContext, claims) = SetupHandler(UserRoles.User, _disallowedSchedule);
            var context = new AuthorizationHandlerContext(_requirements, claims, null);

            await _sut.HandleAsync(context);

            Assert.False(context.HasSucceeded);
            Assert.Equal(
                ApplicationErrorCodes.ParentalControl,
                httpContext.Response.Headers[ApplicationErrorCodes.HeaderName].ToString());
        }

        [Fact]
        public async Task ShouldNotSetErrorCodeHeaderWhenInsideAccessSchedule()
        {
            var (httpContext, claims) = SetupHandler(UserRoles.User, _allowedSchedule);
            var context = new AuthorizationHandlerContext(_requirements, claims, null);

            await _sut.HandleAsync(context);

            Assert.True(context.HasSucceeded);
            Assert.False(httpContext.Response.Headers.ContainsKey(ApplicationErrorCodes.HeaderName));
        }

        [Fact]
        public async Task ShouldNotSetErrorCodeHeaderForAdministrator()
        {
            // Administrators bypass the schedule, so nothing should be flagged.
            var (httpContext, claims) = SetupHandler(UserRoles.Administrator, _disallowedSchedule);
            var context = new AuthorizationHandlerContext(_requirements, claims, null);

            await _sut.HandleAsync(context);

            Assert.True(context.HasSucceeded);
            Assert.False(httpContext.Response.Headers.ContainsKey(ApplicationErrorCodes.HeaderName));
        }

        /// <summary>
        /// Wires the handler up against a real <see cref="DefaultHttpContext"/> so the
        /// response headers it writes can be asserted on.
        /// </summary>
        private (DefaultHttpContext HttpContext, ClaimsPrincipal Claims) SetupHandler(string role, AccessSchedule[] schedules)
        {
            TestHelpers.SetupConfigurationManager(_configurationManagerMock, true);
            var claims = TestHelpers.SetupUser(_userManagerMock, _httpContextAccessor, role, schedules);

            var httpContext = new DefaultHttpContext();
            httpContext.Connection.RemoteIpAddress = new IPAddress(0);
            _httpContextAccessor.Setup(h => h.HttpContext).Returns(httpContext);

            return (httpContext, claims);
        }
    }
}
