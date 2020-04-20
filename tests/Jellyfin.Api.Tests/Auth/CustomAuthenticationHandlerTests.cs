using System;
using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoMoq;
using Jellyfin.Api.Auth;
using Jellyfin.Api.Constants;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Jellyfin.Api.Tests.Auth
{
    public class CustomAuthenticationHandlerTests
    {
        private readonly IFixture _fixture;

        private readonly Mock<IAuthService> _jellyfinAuthServiceMock;
        private readonly Mock<IOptionsMonitor<AuthenticationSchemeOptions>> _optionsMonitorMock;
        private readonly Mock<ISystemClock> _clockMock;
        private readonly Mock<IServiceProvider> _serviceProviderMock;
        private readonly Mock<IAuthenticationService> _authenticationServiceMock;
        private readonly UrlEncoder _urlEncoder;
        private readonly HttpContext _context;

        private readonly CustomAuthenticationHandler _sut;
        private readonly AuthenticationScheme _scheme;

        public CustomAuthenticationHandlerTests()
        {
            var fixtureCustomizations = new AutoMoqCustomization
            {
                ConfigureMembers = true
            };

            _fixture = new Fixture().Customize(fixtureCustomizations);
            AllowFixtureCircularDependencies();

            _jellyfinAuthServiceMock = _fixture.Freeze<Mock<IAuthService>>();
            _optionsMonitorMock = _fixture.Freeze<Mock<IOptionsMonitor<AuthenticationSchemeOptions>>>();
            _clockMock = _fixture.Freeze<Mock<ISystemClock>>();
            _serviceProviderMock = _fixture.Freeze<Mock<IServiceProvider>>();
            _authenticationServiceMock = _fixture.Freeze<Mock<IAuthenticationService>>();
            _fixture.Register<ILoggerFactory>(() => new NullLoggerFactory());

            _urlEncoder = UrlEncoder.Default;

            _serviceProviderMock.Setup(s => s.GetService(typeof(IAuthenticationService)))
                .Returns(_authenticationServiceMock.Object);

            _optionsMonitorMock.Setup(o => o.Get(It.IsAny<string>()))
                .Returns(new AuthenticationSchemeOptions
                {
                    ForwardAuthenticate = null
                });

            _context = new DefaultHttpContext
            {
                RequestServices = _serviceProviderMock.Object
            };

            _scheme = new AuthenticationScheme(
                _fixture.Create<string>(),
                null,
                typeof(CustomAuthenticationHandler));

            _sut = _fixture.Create<CustomAuthenticationHandler>();
            _sut.InitializeAsync(_scheme, _context).Wait();
        }

        [Fact]
        public async Task HandleAuthenticateAsyncShouldFailWithNullUser()
        {
            _jellyfinAuthServiceMock.Setup(
                    a => a.Authenticate(
                        It.IsAny<HttpRequest>(),
                        It.IsAny<AuthenticatedAttribute>()))
                .Returns((User?)null);

            var authenticateResult = await _sut.AuthenticateAsync();

            Assert.False(authenticateResult.Succeeded);
            Assert.Equal("Invalid user", authenticateResult.Failure.Message);
        }

        [Fact]
        public async Task HandleAuthenticateAsyncShouldFailOnSecurityException()
        {
            var errorMessage = _fixture.Create<string>();

            _jellyfinAuthServiceMock.Setup(
                    a => a.Authenticate(
                        It.IsAny<HttpRequest>(),
                        It.IsAny<AuthenticatedAttribute>()))
                .Throws(new SecurityException(errorMessage));

            var authenticateResult = await _sut.AuthenticateAsync();

            Assert.False(authenticateResult.Succeeded);
            Assert.Equal(errorMessage, authenticateResult.Failure.Message);
        }

        [Fact]
        public async Task HandleAuthenticateAsyncShouldSucceedWithUser()
        {
            SetupUser();
            var authenticateResult = await _sut.AuthenticateAsync();

            Assert.True(authenticateResult.Succeeded);
            Assert.Null(authenticateResult.Failure);
        }

        [Fact]
        public async Task HandleAuthenticateAsyncShouldAssignNameClaim()
        {
            var user = SetupUser();
            var authenticateResult = await _sut.AuthenticateAsync();

            Assert.True(authenticateResult.Principal.HasClaim(ClaimTypes.Name, user.Name));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task HandleAuthenticateAsyncShouldAssignRoleClaim(bool isAdmin)
        {
            var user = SetupUser(isAdmin);
            var authenticateResult = await _sut.AuthenticateAsync();

            var expectedRole = user.Policy.IsAdministrator ? UserRoles.Administrator : UserRoles.User;
            Assert.True(authenticateResult.Principal.HasClaim(ClaimTypes.Role, expectedRole));
        }

        [Fact]
        public async Task HandleAuthenticateAsyncShouldAssignTicketCorrectScheme()
        {
            SetupUser();
            var authenticatedResult = await _sut.AuthenticateAsync();

            Assert.Equal(_scheme.Name, authenticatedResult.Ticket.AuthenticationScheme);
        }

        private User SetupUser(bool isAdmin = false)
        {
            var user = _fixture.Create<User>();
            user.Policy.IsAdministrator = isAdmin;

            _jellyfinAuthServiceMock.Setup(
                    a => a.Authenticate(
                        It.IsAny<HttpRequest>(),
                        It.IsAny<AuthenticatedAttribute>()))
                .Returns(user);

            return user;
        }

        private void AllowFixtureCircularDependencies()
        {
            // A circular dependency exists in the User entity around parent folders,
            // this allows Autofixture to generate a User regardless, rather than throw
            // an error.
            _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
                .ForEach(b => _fixture.Behaviors.Remove(b));
            _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        }
    }
}
