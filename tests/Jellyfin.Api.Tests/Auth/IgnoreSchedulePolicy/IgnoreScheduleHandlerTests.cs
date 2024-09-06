using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoMoq;
using Jellyfin.Api.Auth.DefaultAuthorizationPolicy;
using Jellyfin.Api.Constants;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace Jellyfin.Api.Tests.Auth.IgnoreSchedulePolicy
{
    public class IgnoreScheduleHandlerTests
    {
        private readonly Mock<IConfigurationManager> _configurationManagerMock;
        private readonly List<IAuthorizationRequirement> _requirements;
        private readonly DefaultAuthorizationHandler _sut;
        private readonly Mock<IUserManager> _userManagerMock;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessor;

        /// <summary>
        /// Globally disallow access.
        /// </summary>
        private readonly AccessSchedule[] _accessSchedules = { new AccessSchedule(DynamicDayOfWeek.Everyday, 0, 0, Guid.Empty) };

        public IgnoreScheduleHandlerTests()
        {
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            _configurationManagerMock = fixture.Freeze<Mock<IConfigurationManager>>();
            _requirements = new List<IAuthorizationRequirement> { new DefaultAuthorizationRequirement(validateParentalSchedule: false) };
            _userManagerMock = fixture.Freeze<Mock<IUserManager>>();
            _httpContextAccessor = fixture.Freeze<Mock<IHttpContextAccessor>>();

            _sut = fixture.Create<DefaultAuthorizationHandler>();
        }

        [Theory]
        [InlineData(UserRoles.Administrator, true)]
        [InlineData(UserRoles.User, true)]
        [InlineData(UserRoles.Guest, true)]
        public async Task ShouldAllowScheduleCorrectly(string role, bool shouldSucceed)
        {
            TestHelpers.SetupConfigurationManager(_configurationManagerMock, true);
            var claims = TestHelpers.SetupUser(
                _userManagerMock,
                _httpContextAccessor,
                role,
                _accessSchedules);

            var context = new AuthorizationHandlerContext(_requirements, claims, null);

            await _sut.HandleAsync(context);
            Assert.Equal(shouldSucceed, context.HasSucceeded);
        }
    }
}
