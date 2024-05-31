using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoMoq;
using Jellyfin.Api.Auth.DefaultAuthorizationPolicy;
using Jellyfin.Api.Auth.FirstTimeSetupPolicy;
using Jellyfin.Api.Constants;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Jellyfin.Api.Tests.Auth.FirstTimeSetupPolicy
{
    public class FirstTimeSetupHandlerTests
    {
        private readonly Mock<IConfigurationManager> _configurationManagerMock;
        private readonly List<IAuthorizationRequirement> _requirements;
        private readonly DefaultAuthorizationHandler _defaultAuthorizationHandler;
        private readonly FirstTimeSetupHandler _firstTimeSetupHandler;
        private readonly IAuthorizationService _authorizationService;
        private readonly Mock<IUserManager> _userManagerMock;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessor;

        public FirstTimeSetupHandlerTests()
        {
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            _configurationManagerMock = fixture.Freeze<Mock<IConfigurationManager>>();
            _requirements = new List<IAuthorizationRequirement> { new FirstTimeSetupRequirement() };
            _userManagerMock = fixture.Freeze<Mock<IUserManager>>();
            _httpContextAccessor = fixture.Freeze<Mock<IHttpContextAccessor>>();

            _firstTimeSetupHandler = fixture.Create<FirstTimeSetupHandler>();
            _defaultAuthorizationHandler = fixture.Create<DefaultAuthorizationHandler>();

            var services = new ServiceCollection();
            services.AddAuthorizationCore();
            services.AddLogging();
            services.AddOptions();
            services.AddSingleton<IAuthorizationHandler>(_defaultAuthorizationHandler);
            services.AddSingleton<IAuthorizationHandler>(_firstTimeSetupHandler);
            services.AddAuthorization(options =>
            {
                options.AddPolicy("FirstTime", policy => policy.Requirements.Add(new FirstTimeSetupRequirement()));
                options.AddPolicy("FirstTimeNoAdmin", policy => policy.Requirements.Add(new FirstTimeSetupRequirement(false, false)));
                options.AddPolicy("FirstTimeSchedule", policy => policy.Requirements.Add(new FirstTimeSetupRequirement(true, false)));
            });
            _authorizationService = services.BuildServiceProvider().GetRequiredService<IAuthorizationService>();
        }

        [Theory]
        [InlineData(UserRoles.Administrator)]
        [InlineData(UserRoles.Guest)]
        [InlineData(UserRoles.User)]
        public async Task ShouldSucceedIfStartupWizardIncomplete(string userRole)
        {
            TestHelpers.SetupConfigurationManager(_configurationManagerMock, false);
            var claims = TestHelpers.SetupUser(
                _userManagerMock,
                _httpContextAccessor,
                userRole);

            var allowed = await _authorizationService.AuthorizeAsync(claims, "FirstTime");

            Assert.True(allowed.Succeeded);
        }

        [Theory]
        [InlineData(UserRoles.Administrator, true)]
        [InlineData(UserRoles.Guest, false)]
        [InlineData(UserRoles.User, false)]
        public async Task ShouldRequireAdministratorIfStartupWizardComplete(string userRole, bool shouldSucceed)
        {
            TestHelpers.SetupConfigurationManager(_configurationManagerMock, true);
            var claims = TestHelpers.SetupUser(
                _userManagerMock,
                _httpContextAccessor,
                userRole);

            var allowed = await _authorizationService.AuthorizeAsync(claims, "FirstTime");

            Assert.Equal(shouldSucceed, allowed.Succeeded);
        }

        [Theory]
        [InlineData(UserRoles.Administrator, true)]
        [InlineData(UserRoles.Guest, false)]
        [InlineData(UserRoles.User, true)]
        public async Task ShouldRequireUserIfNotAdministrator(string userRole, bool shouldSucceed)
        {
            TestHelpers.SetupConfigurationManager(_configurationManagerMock, true);
            var claims = TestHelpers.SetupUser(
                _userManagerMock,
                _httpContextAccessor,
                userRole);

            var allowed = await _authorizationService.AuthorizeAsync(claims, "FirstTimeNoAdmin");

            Assert.Equal(shouldSucceed, allowed.Succeeded);
        }

        [Fact]
        public async Task ShouldDisallowUserIfOutsideSchedule()
        {
            AccessSchedule[] accessSchedules = { new AccessSchedule(DynamicDayOfWeek.Everyday, 0, 0, Guid.Empty) };

            TestHelpers.SetupConfigurationManager(_configurationManagerMock, true);
            var claims = TestHelpers.SetupUser(
                _userManagerMock,
                _httpContextAccessor,
                UserRoles.User,
                accessSchedules);

            var allowed = await _authorizationService.AuthorizeAsync(claims, "FirstTimeSchedule");

            Assert.False(allowed.Succeeded);
        }
    }
}
