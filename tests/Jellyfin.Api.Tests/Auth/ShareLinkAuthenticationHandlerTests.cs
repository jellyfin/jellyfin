using System;
using System.Globalization;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Attributes;
using Jellyfin.Api.Auth;
using Jellyfin.Api.Constants;
using MediaBrowser.Controller.Authentication;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.ShareLinks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Jellyfin.Api.Tests.Auth
{
    public class ShareLinkAuthenticationHandlerTests
    {
        private const string ShareToken = "share-token";
        private const string SchemeName = "CustomAuthentication";

        [Fact]
        public async Task ShareToken_OnNonMarkerEndpoint_ReturnsNoResultAndSkipsValidation()
        {
            var tokenService = new Mock<IShareLinkTokenService>(MockBehavior.Strict);
            var handler = CreateHandler(tokenService.Object, new EndpointMetadataCollection());

            var result = await handler.AuthenticateAsync();

            Assert.True(result.None);
            tokenService.Verify(
                s => s.ValidateAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShareToken_OnMarkerEndpoint_ReturnsSharePrincipal()
        {
            var itemId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var tokenService = new Mock<IShareLinkTokenService>();
            tokenService
                .Setup(s => s.ValidateAsync(ShareToken, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ShareLinkValidationResult("jti-1", userId, itemId, 1700000000, 1700003600, 3600, null));

            var handler = CreateHandler(
                tokenService.Object,
                new EndpointMetadataCollection(new ShareLinkAccessAttribute()));

            var result = await handler.AuthenticateAsync();

            Assert.True(result.Succeeded);
            var principal = Assert.IsType<ClaimsPrincipal>(result.Principal);
            Assert.True(principal.HasClaim(InternalClaimTypes.IsShareLink, bool.TrueString));
            Assert.Equal(
                userId.ToString("N", CultureInfo.InvariantCulture),
                principal.FindFirst(InternalClaimTypes.UserId)?.Value);
        }

        private static CustomAuthenticationHandler CreateHandler(
            IShareLinkTokenService tokenService,
            EndpointMetadataCollection metadata)
        {
            var authService = new Mock<IAuthService>();
            authService
                .Setup(a => a.Authenticate(It.IsAny<HttpRequest>()))
                .ReturnsAsync(new AuthorizationInfo { Token = null, User = null });

            var optionsMonitor = new Mock<IOptionsMonitor<AuthenticationSchemeOptions>>();
            optionsMonitor
                .Setup(o => o.Get(It.IsAny<string>()))
                .Returns(new AuthenticationSchemeOptions());

            var handler = new CustomAuthenticationHandler(
                authService.Object,
                tokenService,
                optionsMonitor.Object,
                new NullLoggerFactory(),
                UrlEncoder.Default);

            var httpContext = new DefaultHttpContext();
            httpContext.Request.QueryString = new QueryString($"?share_token={ShareToken}");
            httpContext.SetEndpoint(new Endpoint(null, metadata, "share-link"));

            var scheme = new AuthenticationScheme(SchemeName, null, typeof(CustomAuthenticationHandler));
            handler.InitializeAsync(scheme, httpContext).GetAwaiter().GetResult();

            return handler;
        }
    }
}
