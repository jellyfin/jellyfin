using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Controllers;
using MediaBrowser.Controller.Authentication;
using MediaBrowser.Model.Authentication;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Jellyfin.Api.Tests.Controllers;

public class OidcControllerTests
{
    [Fact]
    public async Task UpdateConfiguration_WhenConfigurationInvalid_ReturnsBadRequest()
    {
        var configurationManager = new Mock<IOidcConfigurationManager>();
        configurationManager
            .Setup(manager => manager.UpdateConfigurationAsync(It.IsAny<OidcConfigurationUpdateDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("invalid configuration"));

        var controller = new OidcController(configurationManager.Object, Mock.Of<IOidcAuthenticationManager>());

        var result = await controller.UpdateConfiguration(new OidcConfigurationUpdateDto(), CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("invalid configuration", badRequest.Value);
    }
}
