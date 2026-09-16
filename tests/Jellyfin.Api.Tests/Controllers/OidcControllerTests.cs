using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Constants;
using Jellyfin.Api.Controllers;
using Jellyfin.Api.Results;
using MediaBrowser.Common;
using MediaBrowser.Controller.Authentication;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Jellyfin.Api.Tests.Controllers;

public class OidcControllerTests
{
    [Fact]
    public async Task GetProviders_WhenProviderSchemesRegistered_ReturnsAbsoluteCallbackUri()
    {
        var configurationManager = new Mock<IOidcConfigurationManager>();
        configurationManager
            .Setup(manager => manager.GetProviderInfos())
            .Returns(new List<OidcProviderInfo>
            {
                new()
                {
                    ProviderId = "authelia",
                    Name = "Authelia",
                    Authority = "https://auth.example.com"
                }
            });

        var schemeProvider = new Mock<IAuthenticationSchemeProvider>();
        schemeProvider
            .Setup(provider => provider.GetSchemeAsync(AuthenticationSchemes.GetOidcScheme("authelia")))
            .ReturnsAsync(new AuthenticationScheme(AuthenticationSchemes.GetOidcScheme("authelia"), "OIDC", typeof(IAuthenticationHandler)));
        schemeProvider
            .Setup(provider => provider.GetSchemeAsync(AuthenticationSchemes.GetOidcExternalCookieScheme("authelia")))
            .ReturnsAsync(new AuthenticationScheme(AuthenticationSchemes.GetOidcExternalCookieScheme("authelia"), "OIDC Cookie", typeof(IAuthenticationHandler)));

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("jellyfin.example.org");
        httpContext.Request.PathBase = "/jellyfin";
        var controller = new OidcController(
            configurationManager.Object,
            Mock.Of<IOidcAuthenticationManager>(),
            schemeProvider.Object,
            Mock.Of<IUserManager>(),
            Mock.Of<IApplicationHost>())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            }
        };

        var result = await controller.GetProviders();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var providers = Assert.IsAssignableFrom<IReadOnlyList<OidcProviderInfo>>(okResult.Value);
        var provider = Assert.Single(providers);
        Assert.Equal("https://jellyfin.example.org/jellyfin/auth/oidc/authelia/callback", provider.RedirectUri);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    public async Task Complete_WhenProviderOrCookieSchemeMissing_ReturnsNotFound(bool oidcSchemeRegistered, bool cookieSchemeRegistered)
    {
        var configurationManager = new Mock<IOidcConfigurationManager>();
        configurationManager
            .Setup(manager => manager.GetEnabledProvider("authelia"))
            .Returns(new OidcProviderOptions
            {
                Enabled = true,
                ProviderId = "authelia"
            });
        var authenticationManager = new Mock<IOidcAuthenticationManager>();
        var schemeProvider = new Mock<IAuthenticationSchemeProvider>();
        schemeProvider
            .Setup(provider => provider.GetSchemeAsync(AuthenticationSchemes.GetOidcScheme("authelia")))
            .ReturnsAsync(oidcSchemeRegistered ? new AuthenticationScheme(AuthenticationSchemes.GetOidcScheme("authelia"), "OIDC", typeof(IAuthenticationHandler)) : null);
        schemeProvider
            .Setup(provider => provider.GetSchemeAsync(AuthenticationSchemes.GetOidcExternalCookieScheme("authelia")))
            .ReturnsAsync(cookieSchemeRegistered ? new AuthenticationScheme(AuthenticationSchemes.GetOidcExternalCookieScheme("authelia"), "OIDC Cookie", typeof(IAuthenticationHandler)) : null);

        var controller = new OidcController(
            configurationManager.Object,
            authenticationManager.Object,
            schemeProvider.Object,
            Mock.Of<IUserManager>(),
            Mock.Of<IApplicationHost>());

        var result = await controller.Complete("authelia", CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
        authenticationManager.Verify(
            manager => manager.CompleteSignInAsync(It.IsAny<OidcExternalIdentityRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Complete_WhenOnlyWrongProviderExternalCookieExists_ReturnsUnauthorized()
    {
        var configurationManager = new Mock<IOidcConfigurationManager>();
        configurationManager
            .Setup(manager => manager.GetEnabledProvider("authelia"))
            .Returns(new OidcProviderOptions
            {
                Enabled = true,
                ProviderId = "authelia",
                UsernameClaim = "preferred_username",
                RoleClaim = "groups",
                EmailClaim = "email"
            });

        var authenticationManager = new Mock<IOidcAuthenticationManager>();
        var schemeProvider = new Mock<IAuthenticationSchemeProvider>();
        schemeProvider
            .Setup(provider => provider.GetSchemeAsync(AuthenticationSchemes.GetOidcScheme("authelia")))
            .ReturnsAsync(new AuthenticationScheme(AuthenticationSchemes.GetOidcScheme("authelia"), "OIDC", typeof(IAuthenticationHandler)));
        schemeProvider
            .Setup(provider => provider.GetSchemeAsync(AuthenticationSchemes.GetOidcExternalCookieScheme("authelia")))
            .ReturnsAsync(new AuthenticationScheme(AuthenticationSchemes.GetOidcExternalCookieScheme("authelia"), "OIDC Cookie", typeof(IAuthenticationHandler)));

        var wrongProviderPrincipal = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim("iss", "https://issuer.example.com"),
                new Claim("sub", "subject"),
                new Claim("preferred_username", "alice")
            ],
            AuthenticationSchemes.GetOidcExternalCookieScheme("other")));
        var wrongProviderTicket = new AuthenticationTicket(
            wrongProviderPrincipal,
            new AuthenticationProperties(),
            AuthenticationSchemes.GetOidcExternalCookieScheme("other"));
        var authenticationService = new Mock<IAuthenticationService>();
        authenticationService
            .Setup(service => service.AuthenticateAsync(It.IsAny<HttpContext>(), AuthenticationSchemes.GetOidcExternalCookieScheme("authelia")))
            .ReturnsAsync(AuthenticateResult.NoResult());
        authenticationService
            .Setup(service => service.AuthenticateAsync(It.IsAny<HttpContext>(), AuthenticationSchemes.GetOidcExternalCookieScheme("other")))
            .ReturnsAsync(AuthenticateResult.Success(wrongProviderTicket));

        var controller = CreateController(
            configurationManager.Object,
            authenticationManager.Object,
            schemeProvider.Object,
            authenticationService.Object);

        var result = await controller.Complete("authelia", CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
        authenticationService.Verify(
            service => service.AuthenticateAsync(It.IsAny<HttpContext>(), AuthenticationSchemes.GetOidcExternalCookieScheme("authelia")),
            Times.Once);
        authenticationService.Verify(
            service => service.AuthenticateAsync(It.IsAny<HttpContext>(), AuthenticationSchemes.GetOidcExternalCookieScheme("other")),
            Times.Never);
        authenticationManager.Verify(
            manager => manager.CompleteSignInAsync(It.IsAny<OidcExternalIdentityRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Complete_WhenReturnUrlContainsFragment_AppendsExchangeCodeBeforeFragment()
    {
        var configurationManager = new Mock<IOidcConfigurationManager>();
        configurationManager
            .Setup(manager => manager.GetEnabledProvider("authelia"))
            .Returns(new OidcProviderOptions
            {
                Enabled = true,
                ProviderId = "authelia",
                UsernameClaim = "preferred_username",
                RoleClaim = "groups",
                EmailClaim = "email"
            });

        var authenticationManager = new Mock<IOidcAuthenticationManager>();
        authenticationManager
            .Setup(manager => manager.CompleteSignInAsync(It.IsAny<OidcExternalIdentityRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("exchange-code");
        var schemeProvider = CreateRegisteredSchemeProvider("authelia");

        var authenticationProperties = new AuthenticationProperties();
        authenticationProperties.Items["jellyfin:app"] = "Jellyfin Web";
        authenticationProperties.Items["jellyfin:appVersion"] = "1.0.0";
        authenticationProperties.Items["jellyfin:deviceId"] = "device-id";
        authenticationProperties.Items["jellyfin:deviceName"] = "Browser";
        authenticationProperties.Items[OidcConstants.ReturnUrlProperty] = "/web/#/login";
        var authenticationTicket = new AuthenticationTicket(
            CreateOidcPrincipal(AuthenticationSchemes.GetOidcExternalCookieScheme("authelia")),
            authenticationProperties,
            AuthenticationSchemes.GetOidcExternalCookieScheme("authelia"));
        var authenticationService = new Mock<IAuthenticationService>();
        authenticationService
            .Setup(service => service.AuthenticateAsync(It.IsAny<HttpContext>(), AuthenticationSchemes.GetOidcExternalCookieScheme("authelia")))
            .ReturnsAsync(AuthenticateResult.Success(authenticationTicket));
        authenticationService
            .Setup(service => service.SignOutAsync(It.IsAny<HttpContext>(), AuthenticationSchemes.GetOidcExternalCookieScheme("authelia"), It.IsAny<AuthenticationProperties>()))
            .Returns(Task.CompletedTask);

        var controller = CreateController(
            configurationManager.Object,
            authenticationManager.Object,
            schemeProvider.Object,
            authenticationService.Object);

        var result = await controller.Complete("authelia", CancellationToken.None);

        var redirect = Assert.IsType<LocalRedirectResult>(result);
        Assert.Equal("/web/?oidc_code=exchange-code#/login", redirect.Url);
    }

    [Fact]
    public async Task Complete_WhenLocalValidationFailsWithReturnUrl_RedirectsWithOidcError()
    {
        var configurationManager = new Mock<IOidcConfigurationManager>();
        configurationManager
            .Setup(manager => manager.GetEnabledProvider("authelia"))
            .Returns(new OidcProviderOptions
            {
                Enabled = true,
                ProviderId = "authelia",
                UsernameClaim = "preferred_username",
                RoleClaim = "groups",
                EmailClaim = "email"
            });

        var authenticationManager = new Mock<IOidcAuthenticationManager>();
        authenticationManager
            .Setup(manager => manager.CompleteSignInAsync(It.IsAny<OidcExternalIdentityRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new MediaBrowser.Controller.Net.SecurityException("Forbidden."));
        var schemeProvider = CreateRegisteredSchemeProvider("authelia");

        var authenticationProperties = new AuthenticationProperties();
        authenticationProperties.Items["jellyfin:app"] = "Jellyfin Web";
        authenticationProperties.Items["jellyfin:appVersion"] = "1.0.0";
        authenticationProperties.Items["jellyfin:deviceId"] = "device-id";
        authenticationProperties.Items["jellyfin:deviceName"] = "Browser";
        authenticationProperties.Items[OidcConstants.ReturnUrlProperty] = "/web/#/login";
        var authenticationTicket = new AuthenticationTicket(
            CreateOidcPrincipal(AuthenticationSchemes.GetOidcExternalCookieScheme("authelia")),
            authenticationProperties,
            AuthenticationSchemes.GetOidcExternalCookieScheme("authelia"));
        var authenticationService = new Mock<IAuthenticationService>();
        authenticationService
            .Setup(service => service.AuthenticateAsync(It.IsAny<HttpContext>(), AuthenticationSchemes.GetOidcExternalCookieScheme("authelia")))
            .ReturnsAsync(AuthenticateResult.Success(authenticationTicket));
        authenticationService
            .Setup(service => service.SignOutAsync(It.IsAny<HttpContext>(), AuthenticationSchemes.GetOidcExternalCookieScheme("authelia"), It.IsAny<AuthenticationProperties>()))
            .Returns(Task.CompletedTask);

        var controller = CreateController(
            configurationManager.Object,
            authenticationManager.Object,
            schemeProvider.Object,
            authenticationService.Object);
        var urlHelper = new Mock<IUrlHelper>();
        urlHelper
            .Setup(url => url.IsLocalUrl("/web/#/login"))
            .Returns(true);
        controller.Url = urlHelper.Object;

        var result = await controller.Complete("authelia", CancellationToken.None);

        var redirect = Assert.IsType<LocalRedirectResult>(result);
        Assert.Equal("/web/?oidc_error=local_failure#/login", redirect.Url);
    }

    [Fact]
    public async Task CreateLinkStartUrl_WhenAuthenticated_ReturnsStartUrl()
    {
        var controller = CreateAuthenticatedController(
            "authelia",
            out var authenticationManager,
            out _,
            out var urlHelper);
        authenticationManager
            .Setup(manager => manager.CreateLinkCodeAsync("authelia", It.IsAny<Guid>(), "/web", It.IsAny<CancellationToken>()))
            .ReturnsAsync("link-code");
        urlHelper
            .Setup(url => url.Action(It.IsAny<UrlActionContext>()))
            .Returns("/auth/oidc/authelia/link/launch?code=link-code");
        urlHelper
            .Setup(url => url.IsLocalUrl("/web"))
            .Returns(true);

        var result = await controller.CreateLinkStartUrl("authelia", new OidcLinkStartRequest { ReturnUrl = "/web" }, CancellationToken.None);

        var okResult = Assert.IsType<OkResult<OidcStartResult>>(result.Result);
        var startResult = Assert.IsType<OidcStartResult>(okResult.Value);
        Assert.Equal("/auth/oidc/authelia/link/launch?code=link-code", startResult.Url);
    }

    [Fact]
    public async Task CreateLinkStartUrl_WhenReturnUrlUnsafe_ReturnsBadRequest()
    {
        var controller = CreateAuthenticatedController(
            "authelia",
            out var authenticationManager,
            out _,
            out var urlHelper);
        urlHelper
            .Setup(url => url.IsLocalUrl("https://evil.example"))
            .Returns(false);

        var result = await controller.CreateLinkStartUrl("authelia", new OidcLinkStartRequest { ReturnUrl = "https://evil.example" }, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Return URL must be a relative URL.", badRequest.Value);
        authenticationManager.Verify(
            manager => manager.CreateLinkCodeAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task LaunchLink_WhenLinkCodeValid_ReturnsOidcChallenge()
    {
        var configurationManager = new Mock<IOidcConfigurationManager>();
        configurationManager
            .Setup(manager => manager.GetEnabledProvider("authelia"))
            .Returns(new OidcProviderOptions
            {
                Enabled = true,
                ProviderId = "authelia"
            });
        var userId = Guid.NewGuid();
        var authenticationManager = new Mock<IOidcAuthenticationManager>();
        authenticationManager
            .Setup(manager => manager.ConsumeLinkCodeAsync("authelia", "link-code", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OidcLinkRequest
            {
                ProviderId = "authelia",
                UserId = userId,
                ReturnUrl = "/web"
            });
        var schemeProvider = CreateRegisteredSchemeProvider("authelia");
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("jellyfin.example.org");
        var urlHelper = new Mock<IUrlHelper>();
        urlHelper
            .SetupGet(url => url.ActionContext)
            .Returns(new ActionContext(httpContext, new RouteData(), new ActionDescriptor()));
        urlHelper
            .Setup(url => url.Action(It.IsAny<UrlActionContext>()))
            .Returns("/auth/oidc/authelia/link/complete");
        var controller = new OidcController(
            configurationManager.Object,
            authenticationManager.Object,
            schemeProvider.Object,
            Mock.Of<IUserManager>(),
            Mock.Of<IApplicationHost>())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            },
            Url = urlHelper.Object
        };

        var result = await controller.LaunchLink("authelia", "link-code", CancellationToken.None);

        var challenge = Assert.IsType<ChallengeResult>(result);
        Assert.Contains(AuthenticationSchemes.GetOidcScheme("authelia"), challenge.AuthenticationSchemes);
        Assert.Equal("/auth/oidc/authelia/link/complete", challenge.Properties?.RedirectUri);
    }

    [Fact]
    public async Task CompleteLink_WhenLinkUserIdMissing_ReturnsUnauthorized()
    {
        var configurationManager = new Mock<IOidcConfigurationManager>();
        configurationManager
            .Setup(manager => manager.GetEnabledProvider("authelia"))
            .Returns(new OidcProviderOptions
            {
                Enabled = true,
                ProviderId = "authelia",
                UsernameClaim = "preferred_username",
                RoleClaim = "groups",
                EmailClaim = "email"
            });

        var authenticationManager = new Mock<IOidcAuthenticationManager>();
        var schemeProvider = CreateRegisteredSchemeProvider("authelia");
        var authenticationTicket = new AuthenticationTicket(
            CreateOidcPrincipal(AuthenticationSchemes.GetOidcExternalCookieScheme("authelia")),
            new AuthenticationProperties(),
            AuthenticationSchemes.GetOidcExternalCookieScheme("authelia"));
        var authenticationService = new Mock<IAuthenticationService>();
        authenticationService
            .Setup(service => service.AuthenticateAsync(It.IsAny<HttpContext>(), AuthenticationSchemes.GetOidcExternalCookieScheme("authelia")))
            .ReturnsAsync(AuthenticateResult.Success(authenticationTicket));
        authenticationService
            .Setup(service => service.SignOutAsync(It.IsAny<HttpContext>(), AuthenticationSchemes.GetOidcExternalCookieScheme("authelia"), It.IsAny<AuthenticationProperties>()))
            .Returns(Task.CompletedTask);

        var controller = CreateController(
            configurationManager.Object,
            authenticationManager.Object,
            schemeProvider.Object,
            authenticationService.Object);

        var result = await controller.CompleteLink("authelia", CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
        authenticationManager.Verify(
            manager => manager.LinkExternalIdentityAsync(It.IsAny<Guid>(), It.IsAny<OidcExternalIdentityRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public void GetConfiguration_WhenProviderConfigured_AddsAbsoluteCallbackUri()
    {
        var configurationManager = new Mock<IOidcConfigurationManager>();
        configurationManager
            .Setup(manager => manager.GetConfiguration())
            .Returns(new OidcConfigurationDto
            {
                Providers = new List<OidcProviderConfigurationDto>
                {
                    new()
                    {
                        ProviderId = "authelia",
                        Name = "Authelia"
                    }
                }
            });
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("jellyfin.example.org");
        httpContext.Request.PathBase = "/jellyfin";
        var controller = new OidcController(
            configurationManager.Object,
            Mock.Of<IOidcAuthenticationManager>(),
            Mock.Of<IAuthenticationSchemeProvider>(),
            Mock.Of<IUserManager>(),
            Mock.Of<IApplicationHost>())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            }
        };

        var result = controller.GetConfiguration();

        var okResult = Assert.IsType<OkResult<OidcConfigurationDto>>(result.Result);
        var configuration = Assert.IsType<OidcConfigurationDto>(okResult.Value);
        var provider = Assert.Single(configuration.Providers);
        Assert.Equal("https://jellyfin.example.org/jellyfin/auth/oidc/authelia/callback", provider.RedirectUri);
    }

    [Fact]
    public async Task UpdateConfiguration_WhenConfigurationInvalid_ReturnsBadRequest()
    {
        var configurationManager = new Mock<IOidcConfigurationManager>();
        var applicationHost = new Mock<IApplicationHost>();
        configurationManager
            .Setup(manager => manager.UpdateConfigurationAsync(It.IsAny<OidcConfigurationUpdateDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("invalid configuration"));

        var controller = new OidcController(
            configurationManager.Object,
            Mock.Of<IOidcAuthenticationManager>(),
            Mock.Of<IAuthenticationSchemeProvider>(),
            Mock.Of<IUserManager>(),
            applicationHost.Object);

        var result = await controller.UpdateConfiguration(new OidcConfigurationUpdateDto(), CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("invalid configuration", badRequest.Value);
        applicationHost.Verify(host => host.NotifyPendingRestart(), Times.Never);
    }

    [Fact]
    public async Task UpdateConfiguration_WhenConfigurationValid_ReturnsRestartRequired()
    {
        var configurationManager = new Mock<IOidcConfigurationManager>();
        configurationManager
            .Setup(manager => manager.GetConfiguration())
            .Returns(new OidcConfigurationDto { RequiresRestart = true });
        var applicationHost = new Mock<IApplicationHost>();

        var controller = new OidcController(
            configurationManager.Object,
            Mock.Of<IOidcAuthenticationManager>(),
            Mock.Of<IAuthenticationSchemeProvider>(),
            Mock.Of<IUserManager>(),
            applicationHost.Object);

        var result = await controller.UpdateConfiguration(new OidcConfigurationUpdateDto(), CancellationToken.None);

        var okResult = Assert.IsType<OkResult<OidcConfigurationUpdateResult>>(result.Result);
        var updateResult = Assert.IsType<OidcConfigurationUpdateResult>(okResult.Value);
        Assert.True(updateResult.RequiresRestart);
        applicationHost.Verify(host => host.NotifyPendingRestart(), Times.Once);
    }

    [Fact]
    public async Task UpdateConfiguration_WhenSavedConfigurationMatchesActiveConfiguration_ReturnsNoRestartRequired()
    {
        var configurationManager = new Mock<IOidcConfigurationManager>();
        configurationManager
            .Setup(manager => manager.GetConfiguration())
            .Returns(new OidcConfigurationDto());
        var applicationHost = new Mock<IApplicationHost>();

        var controller = new OidcController(
            configurationManager.Object,
            Mock.Of<IOidcAuthenticationManager>(),
            Mock.Of<IAuthenticationSchemeProvider>(),
            Mock.Of<IUserManager>(),
            applicationHost.Object);

        var result = await controller.UpdateConfiguration(new OidcConfigurationUpdateDto(), CancellationToken.None);

        var okResult = Assert.IsType<OkResult<OidcConfigurationUpdateResult>>(result.Result);
        var updateResult = Assert.IsType<OidcConfigurationUpdateResult>(okResult.Value);
        Assert.False(updateResult.RequiresRestart);
        applicationHost.Verify(host => host.NotifyPendingRestart(), Times.Never);
    }

    private static OidcController CreateController(
        IOidcConfigurationManager configurationManager,
        IOidcAuthenticationManager authenticationManager,
        IAuthenticationSchemeProvider schemeProvider,
        IAuthenticationService authenticationService)
    {
        var services = new ServiceCollection()
            .AddSingleton(authenticationService)
            .BuildServiceProvider();

        return new OidcController(
            configurationManager,
            authenticationManager,
            schemeProvider,
            Mock.Of<IUserManager>(),
            Mock.Of<IApplicationHost>())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    RequestServices = services
                }
            }
        };
    }

    private static OidcController CreateAuthenticatedController(
        string providerId,
        out Mock<IOidcAuthenticationManager> authenticationManager,
        out Mock<IAuthenticationSchemeProvider> schemeProvider,
        out Mock<IUrlHelper> urlHelper)
    {
        var configurationManager = new Mock<IOidcConfigurationManager>();
        configurationManager
            .Setup(manager => manager.GetEnabledProvider(providerId))
            .Returns(new OidcProviderOptions
            {
                Enabled = true,
                ProviderId = providerId
            });
        authenticationManager = new Mock<IOidcAuthenticationManager>();
        schemeProvider = CreateRegisteredSchemeProvider(providerId);
        urlHelper = new Mock<IUrlHelper>();
        var userId = Guid.NewGuid();
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(InternalClaimTypes.UserId, userId.ToString("N"))],
                AuthenticationSchemes.CustomAuthentication))
        };
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("jellyfin.example.org");
        urlHelper
            .SetupGet(url => url.ActionContext)
            .Returns(new ActionContext(httpContext, new RouteData(), new ActionDescriptor()));

        return new OidcController(
            configurationManager.Object,
            authenticationManager.Object,
            schemeProvider.Object,
            Mock.Of<IUserManager>(),
            Mock.Of<IApplicationHost>())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            },
            Url = urlHelper.Object
        };
    }

    private static Mock<IAuthenticationSchemeProvider> CreateRegisteredSchemeProvider(string providerId)
    {
        var schemeProvider = new Mock<IAuthenticationSchemeProvider>();
        schemeProvider
            .Setup(provider => provider.GetSchemeAsync(AuthenticationSchemes.GetOidcScheme(providerId)))
            .ReturnsAsync(new AuthenticationScheme(AuthenticationSchemes.GetOidcScheme(providerId), "OIDC", typeof(IAuthenticationHandler)));
        schemeProvider
            .Setup(provider => provider.GetSchemeAsync(AuthenticationSchemes.GetOidcExternalCookieScheme(providerId)))
            .ReturnsAsync(new AuthenticationScheme(AuthenticationSchemes.GetOidcExternalCookieScheme(providerId), "OIDC Cookie", typeof(IAuthenticationHandler)));

        return schemeProvider;
    }

    private static ClaimsPrincipal CreateOidcPrincipal(string authenticationType)
    {
        return new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim("iss", "https://issuer.example.com"),
                new Claim("sub", "subject"),
                new Claim("preferred_username", "alice"),
                new Claim("email", "alice@example.com"),
                new Claim("groups", "jellyfin")
            ],
            authenticationType));
    }
}
