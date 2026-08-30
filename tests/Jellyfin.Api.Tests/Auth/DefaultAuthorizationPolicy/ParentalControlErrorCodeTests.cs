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
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Jellyfin.Api.Tests.Auth.DefaultAuthorizationPolicy
{
    public class ParentalControlErrorCodeTests
    {
        private readonly Mock<IConfigurationManager> _configurationManagerMock;
        private readonly List<IAuthorizationRequirement> _requirements;
        private readonly DefaultAuthorizationHandler _sut;
        private readonly Mock<IUserManager> _userManagerMock;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessor;

        private readonly AccessSchedule[] _disallowedSchedule = { new AccessSchedule(DynamicDayOfWeek.Everyday, 0, 0, Guid.Empty) };

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
        public async Task ShouldLogTheReasonWhenOutsideAccessSchedule()
        {
            var logger = new CollectingLogger();
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            fixture.Inject<ILogger<DefaultAuthorizationHandler>>(logger);
            var configurationManager = fixture.Freeze<Mock<IConfigurationManager>>();
            var userManager = fixture.Freeze<Mock<IUserManager>>();
            var accessor = fixture.Freeze<Mock<IHttpContextAccessor>>();
            var sut = fixture.Create<DefaultAuthorizationHandler>();

            TestHelpers.SetupConfigurationManager(configurationManager, true);
            var claims = TestHelpers.SetupUser(userManager, accessor, UserRoles.User, _disallowedSchedule);
            var httpContext = new DefaultHttpContext();
            httpContext.Connection.RemoteIpAddress = new IPAddress(0);
            httpContext.Request.Path = "/Sessions/Playing/Progress";
            accessor.Setup(h => h.HttpContext).Returns(httpContext);

            await sut.HandleAsync(new AuthorizationHandlerContext(_requirements, claims, null));

            Assert.Contains(logger.Messages, m => m.Contains("parental restrictions", StringComparison.Ordinal));
            Assert.Contains(logger.Messages, m => m.Contains("/Sessions/Playing/Progress", StringComparison.Ordinal));
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
            var (httpContext, claims) = SetupHandler(UserRoles.Administrator, _disallowedSchedule);
            var context = new AuthorizationHandlerContext(_requirements, claims, null);

            await _sut.HandleAsync(context);

            Assert.True(context.HasSucceeded);
            Assert.False(httpContext.Response.Headers.ContainsKey(ApplicationErrorCodes.HeaderName));
        }

        private (DefaultHttpContext HttpContext, ClaimsPrincipal Claims) SetupHandler(string role, AccessSchedule[] schedules)
        {
            TestHelpers.SetupConfigurationManager(_configurationManagerMock, true);
            var claims = TestHelpers.SetupUser(_userManagerMock, _httpContextAccessor, role, schedules);

            var httpContext = new DefaultHttpContext();
            httpContext.Connection.RemoteIpAddress = new IPAddress(0);
            _httpContextAccessor.Setup(h => h.HttpContext).Returns(httpContext);

            return (httpContext, claims);
        }

        private sealed class CollectingLogger : ILogger<DefaultAuthorizationHandler>
        {
            public List<string> Messages { get; } = new();

            public IDisposable? BeginScope<TState>(TState state)
                where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
                => Messages.Add(formatter(state, exception));
        }
    }
}
