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
    private readonly Mock<IAuthenticationManager> _mockAuthManager = new();

    private ApiKeyController CreateController() =>
        new ApiKeyController(_mockAuthManager.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

    [Fact]
    public async Task CreateKey_Returns201WithCreatedKey()
    {
        const string appName = "TestApp";
        var expectedKey = new AuthenticationInfo
        {
            AppName = appName,
            AccessToken = Guid.NewGuid().ToString("N"),
            DateCreated = DateTime.UtcNow,
            DeviceId = string.Empty,
            DeviceName = string.Empty,
            AppVersion = string.Empty
        };

        _mockAuthManager
            .Setup(m => m.CreateApiKey(appName))
            .ReturnsAsync(expectedKey);

        var result = await CreateController().CreateKey(appName);

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status201Created, objectResult.StatusCode);
        var returnedKey = Assert.IsType<AuthenticationInfo>(objectResult.Value);
        Assert.Equal(appName, returnedKey.AppName);
        Assert.Equal(expectedKey.AccessToken, returnedKey.AccessToken);
        Assert.Equal(expectedKey.DateCreated, returnedKey.DateCreated);
    }
}
