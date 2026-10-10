using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Constants;
using Jellyfin.Api.Controllers;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Activity;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Jellyfin.Api.Tests.Controllers
{
    public class LibraryControllerTests
    {
        [Theory]
        [InlineData(UserRoles.Administrator, false, true)]
        [InlineData(UserRoles.Administrator, true, true)]
        [InlineData(UserRoles.User, true, true)]
        [InlineData(UserRoles.User, false, false)]
        [InlineData(null, true, true)]
        [InlineData(null, false, false)]
        public async Task RefreshLibrary_RequiresAdministratorUnlessLocalNetworkRefreshIsAllowed(string? role, bool allowLocalNetworkLibraryRefresh, bool shouldRefresh)
        {
            var libraryManager = new Mock<ILibraryManager>();
            var serverConfigurationManager = new Mock<IServerConfigurationManager>();
            serverConfigurationManager
                .Setup(c => c.Configuration)
                .Returns(new ServerConfiguration { AllowLocalNetworkLibraryRefresh = allowLocalNetworkLibraryRefresh });

            var controller = new LibraryController(
                Mock.Of<IProviderManager>(),
                Mock.Of<ISimilarItemsManager>(),
                libraryManager.Object,
                Mock.Of<IUserManager>(),
                Mock.Of<ICollectionManager>(),
                Mock.Of<IDtoService>(),
                Mock.Of<IActivityManager>(),
                Mock.Of<ILocalizationManager>(),
                Mock.Of<ILibraryMonitor>(),
                Mock.Of<ILogger<LibraryController>>(),
                serverConfigurationManager.Object);

            var identity = role is null ? new ClaimsIdentity() : new ClaimsIdentity([new Claim(ClaimTypes.Role, role)], "test");
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };

            var result = await controller.RefreshLibrary();

            Assert.Equal(shouldRefresh, result is NoContentResult);
            Assert.Equal(!shouldRefresh, result is ForbidResult);
            libraryManager.Verify(
                m => m.ValidateMediaLibrary(It.IsAny<IProgress<double>>(), It.IsAny<CancellationToken>()),
                shouldRefresh ? Times.Once() : Times.Never());
        }
    }
}
