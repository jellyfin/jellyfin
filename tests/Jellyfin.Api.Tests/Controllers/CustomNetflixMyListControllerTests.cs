using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Constants;
using Jellyfin.Api.Controllers;
using MediaBrowser.Controller.CustomNetflix;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Jellyfin.Api.Tests.Controllers;

public sealed class CustomNetflixMyListControllerTests
{
    [Fact]
    public async Task Add_RejectsWriteWhenRequestedProfileIsNotActive()
    {
        var userId = Guid.NewGuid();
        var profileId = Guid.NewGuid();
        var myListService = new Mock<ICustomNetflixMyListService>();
        var activeProfileService = new Mock<ICustomNetflixActiveProfileService>();
        activeProfileService
            .Setup(mock => mock.GetActiveProfileForWriteAsync(
                userId,
                "token",
                profileId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((CustomNetflixProfileDto?)null);
        var controller = new CustomNetflixMyListController(
            myListService.Object,
            activeProfileService.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(
                        new ClaimsIdentity(
                        [
                            new Claim(InternalClaimTypes.UserId, userId.ToString("N")),
                            new Claim(InternalClaimTypes.Token, "token")
                        ]))
                }
            }
        };

        var result = await controller.Add(
            profileId,
            Guid.NewGuid(),
            TestContext.Current.CancellationToken);

        var conflict = Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status409Conflict, conflict.StatusCode);
        myListService.Verify(
            mock => mock.AddAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        activeProfileService.VerifyAll();
    }
}
