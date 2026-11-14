using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Constants;
using Jellyfin.Api.Controllers;
using Jellyfin.Api.Models.ShareLinks;
using Jellyfin.Data;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Server.Implementations.ShareLinks;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.ShareLinks;
using MediaBrowser.Model.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Jellyfin.Api.Tests.Controllers
{
    public sealed class ShareLinksControllerTests
    {
        private readonly Guid _userId = Guid.NewGuid();
        private readonly Guid _itemId = Guid.NewGuid();
        private readonly Mock<IShareLinkTokenService> _tokenService = new();
        private readonly Mock<IShareLinkRepository> _repository = new();
        private readonly Mock<ILibraryManager> _libraryManager = new();
        private readonly Mock<IUserManager> _userManager = new();
        private readonly Mock<IServerConfigurationManager> _configurationManager = new();

        [Fact]
        public async Task CreateShareLink_WithDuration_ReturnsCreatedWithUrl()
        {
            _tokenService
                .Setup(s => s.CreateAsync(_userId, _itemId, (long?)null, 3600, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ShareLinkCreationResult(
                    "token-abc",
                    new ShareLink("jti-1", _userId, _itemId, 1700000000) { DurationSeconds = 3600 },
                    null));

            var controller = CreateController();

            var result = await controller.CreateShareLink(
                _itemId,
                new CreateShareLinkRequestDto { DurationSeconds = 3600 });

            var objectResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status201Created, objectResult.StatusCode);
            var dto = Assert.IsType<ShareLinkDto>(objectResult.Value);
            Assert.Equal("jti-1", dto.Id);
            Assert.Equal($"https://media.example.com/Videos/{_itemId:N}/stream?share_token=token-abc", dto.Url);
        }

        [Fact]
        public async Task CreateShareLink_WithoutExpirationOrDuration_ReturnsBadRequest()
        {
            var controller = CreateController();

            var result = await controller.CreateShareLink(_itemId, new CreateShareLinkRequestDto());

            Assert.IsType<BadRequestResult>(result.Result);
            Assert.Equal(
                ShareLinkCreateErrorCode.ExpirationOrDurationRequired.ToString(),
                controller.Response.Headers["X-ShareLink-Error"].ToString());
        }

        [Fact]
        public async Task CreateShareLink_MissingItem_ReturnsNotFound()
        {
            var controller = CreateController(itemExists: false);

            var result = await controller.CreateShareLink(
                _itemId,
                new CreateShareLinkRequestDto { DurationSeconds = 3600 });

            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task CreateShareLink_WhenDisabled_ReturnsForbidden()
        {
            var controller = CreateController(mediaSharingPolicy: MediaSharingPolicy.Disabled);

            var result = await controller.CreateShareLink(
                _itemId,
                new CreateShareLinkRequestDto { DurationSeconds = 3600 });

            var statusResult = Assert.IsType<StatusCodeResult>(result.Result);
            Assert.Equal(StatusCodes.Status403Forbidden, statusResult.StatusCode);
        }

        [Fact]
        public async Task GetShareLinks_ReturnsOwnLinks()
        {
            _repository
                .Setup(r => r.ListByUserAsync(_userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ShareLink> { new("jti-1", _userId, _itemId, 1700000000) });

            var controller = CreateController();

            var result = await controller.GetShareLinks(null);

            var okResult = Assert.IsAssignableFrom<OkObjectResult>(result.Result);
            var dtos = Assert.IsAssignableFrom<IEnumerable<ShareLinkDto>>(okResult.Value);
            Assert.Single(dtos);
        }

        [Fact]
        public async Task DeleteShareLink_ExistingLink_ReturnsNoContent()
        {
            _repository
                .Setup(r => r.GetByIdAsync("jti-1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ShareLink("jti-1", _userId, _itemId, 1700000000));
            _repository
                .Setup(r => r.RevokeAsync("jti-1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var controller = CreateController();

            var result = await controller.DeleteShareLink("jti-1");

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task DeleteShareLink_MissingLink_ReturnsNotFound()
        {
            _repository
                .Setup(r => r.GetByIdAsync("missing", It.IsAny<CancellationToken>()))
                .ReturnsAsync((ShareLink?)null);

            var controller = CreateController();

            var result = await controller.DeleteShareLink("missing");

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task CreateShareLink_WhenDisabled_ReturnsForbiddenAndErrorCode()
        {
            var controller = CreateController(mediaSharingPolicy: MediaSharingPolicy.Disabled);

            var result = await controller.CreateShareLink(
                _itemId,
                new CreateShareLinkRequestDto { DurationSeconds = 3600 });

            var statusResult = Assert.IsType<StatusCodeResult>(result.Result);
            Assert.Equal(StatusCodes.Status403Forbidden, statusResult.StatusCode);
            Assert.Equal(
                ShareLinkCreateErrorCode.SharingDisabled.ToString(),
                controller.Response.Headers["X-ShareLink-Error"].ToString());
        }

        [Fact]
        public async Task CreateShareLink_PerUserDefaultNo_UnsetUser_ReturnsForbidden()
        {
            var controller = CreateController(
                mediaSharingPolicy: MediaSharingPolicy.PerUserDefaultNo,
                userMediaSharingPolicy: UserMediaSharingPolicy.Default);

            var result = await controller.CreateShareLink(
                _itemId,
                new CreateShareLinkRequestDto { DurationSeconds = 3600 });

            Assert.Equal(StatusCodes.Status403Forbidden, Assert.IsType<StatusCodeResult>(result.Result).StatusCode);
        }

        [Fact]
        public async Task CreateShareLink_PerUserDefaultNo_ExplicitlyEnabledUser_Creates()
        {
            _tokenService
                .Setup(s => s.CreateAsync(_userId, _itemId, (long?)null, 3600, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ShareLinkCreationResult(
                    "token-abc",
                    new ShareLink("jti-1", _userId, _itemId, 1700000000) { DurationSeconds = 3600 },
                    null));

            var controller = CreateController(
                mediaSharingPolicy: MediaSharingPolicy.PerUserDefaultNo,
                userMediaSharingPolicy: UserMediaSharingPolicy.Enabled);

            var result = await controller.CreateShareLink(
                _itemId,
                new CreateShareLinkRequestDto { DurationSeconds = 3600 });

            Assert.Equal(StatusCodes.Status201Created, Assert.IsType<ObjectResult>(result.Result).StatusCode);
        }

        [Fact]
        public async Task CreateShareLink_HardServerEnable_IgnoresUserDisable()
        {
            _tokenService
                .Setup(s => s.CreateAsync(_userId, _itemId, (long?)null, 3600, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ShareLinkCreationResult(
                    "token-abc",
                    new ShareLink("jti-1", _userId, _itemId, 1700000000) { DurationSeconds = 3600 },
                    null));

            var controller = CreateController(
                mediaSharingPolicy: MediaSharingPolicy.Enabled,
                userMediaSharingPolicy: UserMediaSharingPolicy.Disabled);

            var result = await controller.CreateShareLink(
                _itemId,
                new CreateShareLinkRequestDto { DurationSeconds = 3600 });

            Assert.Equal(StatusCodes.Status201Created, Assert.IsType<ObjectResult>(result.Result).StatusCode);
        }

        private ShareLinksController CreateController(
            MediaSharingPolicy mediaSharingPolicy = MediaSharingPolicy.Enabled,
            UserMediaSharingPolicy userMediaSharingPolicy = UserMediaSharingPolicy.Default,
            bool itemExists = true,
            bool itemVisible = true)
        {
            _configurationManager
                .Setup(c => c.Configuration)
                .Returns(new ServerConfiguration { MediaSharingPolicy = mediaSharingPolicy });

            var user = new User("test", "test", "test")
            {
                Id = _userId,
                MediaSharingPolicy = userMediaSharingPolicy
            };
            user.AddDefaultPermissions();
            _userManager.Setup(m => m.GetUserById(_userId)).Returns(user);

            if (itemExists)
            {
                var item = new Mock<BaseItem>();
                item.Setup(i => i.IsVisible(It.IsAny<User>(), It.IsAny<bool>())).Returns(itemVisible);
                _libraryManager.Setup(m => m.GetItemById(_itemId)).Returns(item.Object);
            }
            else
            {
                _libraryManager.Setup(m => m.GetItemById(_itemId)).Returns((BaseItem?)null);
            }

            var controller = new ShareLinksController(
                _tokenService.Object,
                _repository.Object,
                _libraryManager.Object,
                _userManager.Object,
                new ShareLinkPolicyService(_configurationManager.Object));

            var httpContext = new DefaultHttpContext
            {
                User = CreatePrincipal()
            };
            httpContext.Request.Scheme = "https";
            httpContext.Request.Host = new HostString("media.example.com");
            httpContext.Request.RouteValues["itemId"] = _itemId.ToString("N", CultureInfo.InvariantCulture);

            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

            return controller;
        }

        private ClaimsPrincipal CreatePrincipal()
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, "test"),
                new Claim(ClaimTypes.Role, UserRoles.User),
                new Claim(InternalClaimTypes.UserId, _userId.ToString("N", CultureInfo.InvariantCulture))
            };

            return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        }
    }
}
