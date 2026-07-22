using System;
using System.Threading.Tasks;
using Jellyfin.Api.Controllers;
using MediaBrowser.Controller.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Jellyfin.Api.Tests.Controllers;

public sealed class ApiKeyControllerTests
{
    private readonly Mock<IAuthenticationManager> _authenticationManager = new();

    private ApiKeyController CreateController() =>
        new ApiKeyController(_authenticationManager.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

    [Fact]
    public async Task CreateKey_ReturnsManagerResultDirectly()
    {
        var expected = new AuthenticationInfo
        {
            AppName = "my-app",
            AccessToken = "generated-token",
            DateCreated = DateTime.UtcNow
        };
        _authenticationManager.Setup(m => m.CreateApiKey("my-app")).ReturnsAsync(expected);

        var result = await CreateController().CreateKey("my-app");

        Assert.Same(expected, result.Value);
        Assert.Null(result.Result);
    }
}
