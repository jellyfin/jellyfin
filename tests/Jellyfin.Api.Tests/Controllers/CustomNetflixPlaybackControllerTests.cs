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

public sealed class CustomNetflixPlaybackControllerTests
{
    [Fact]
    public async Task ReportProgress_RejectsWriteWhenRequestedProfileIsNotActive()
    {
        var userId = Guid.NewGuid();
        var profileId = Guid.NewGuid();
        var progressService = new Mock<ICustomNetflixWatchProgressService>();
        var historyService = new Mock<ICustomNetflixWatchHistoryService>();
        var activeProfileService = new Mock<ICustomNetflixActiveProfileService>();
        activeProfileService
            .Setup(mock => mock.GetActiveProfileForWriteAsync(
                userId,
                "token",
                profileId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((CustomNetflixProfileDto?)null);
        var controller = new CustomNetflixPlaybackController(
            progressService.Object,
            historyService.Object,
            activeProfileService.Object,
            Mock.Of<ICustomNetflixAutoplayService>())
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

        var result = await controller.ReportProgress(
            profileId,
            new CustomNetflixWatchProgressReportRequest { ItemId = Guid.NewGuid() },
            TestContext.Current.CancellationToken);

        var conflict = Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status409Conflict, conflict.StatusCode);
        progressService.Verify(
            mock => mock.ReportProgressAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<CustomNetflixWatchProgressReportRequest>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        activeProfileService.VerifyAll();
    }

    [Fact]
    public async Task ConfirmStillWatching_RejectsWriteWhenRequestedProfileIsNotActive()
    {
        var userId = Guid.NewGuid();
        var profileId = Guid.NewGuid();
        var autoplayService = new Mock<ICustomNetflixAutoplayService>();
        var activeProfileService = new Mock<ICustomNetflixActiveProfileService>();
        activeProfileService
            .Setup(mock => mock.GetActiveProfileForWriteAsync(
                userId,
                "token",
                profileId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((CustomNetflixProfileDto?)null);
        var controller = new CustomNetflixPlaybackController(
            Mock.Of<ICustomNetflixWatchProgressService>(),
            Mock.Of<ICustomNetflixWatchHistoryService>(),
            activeProfileService.Object,
            autoplayService.Object)
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

        var result = await controller.ConfirmStillWatching(
            profileId,
            TestContext.Current.CancellationToken);

        var conflict = Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status409Conflict, conflict.StatusCode);
        autoplayService.Verify(
            mock => mock.ConfirmStillWatchingAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        activeProfileService.VerifyAll();
    }
}
