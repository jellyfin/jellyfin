using System;
using System.Globalization;
using System.Security.Claims;
using System.Threading.Tasks;
using Jellyfin.Api.Constants;
using Jellyfin.Api.Controllers;
using Jellyfin.Data;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Activity;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.MediaInfo;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Jellyfin.Api.Tests.Controllers;

[Collection("LibraryManagerTests")]
public sealed class LibraryControllerTests : IDisposable
{
    private const string FileBookPath = "/media/books/test-book.epub";
    private const string FileVideoPath = "/media/movies/test-movie.mkv";

    private readonly LibraryController _subject;
    private readonly Mock<ILibraryManager> _mockLibraryManager;
    private readonly Mock<IUserManager> _mockUserManager;
    private readonly Mock<IActivityManager> _mockActivityManager;
    private readonly Mock<ILocalizationManager> _mockLocalization;
    private readonly IMediaSourceManager? _previousMediaSourceManager;

    public LibraryControllerTests()
    {
        _previousMediaSourceManager = BaseItem.MediaSourceManager;
        var mediaSourceManager = new Mock<IMediaSourceManager>();
        mediaSourceManager.Setup(x => x.GetPathProtocol(FileBookPath)).Returns(MediaProtocol.File);
        mediaSourceManager.Setup(x => x.GetPathProtocol(FileVideoPath)).Returns(MediaProtocol.File);
        BaseItem.MediaSourceManager = mediaSourceManager.Object;

        _mockLibraryManager = new Mock<ILibraryManager>();
        _mockUserManager = new Mock<IUserManager>();
        _mockActivityManager = new Mock<IActivityManager>();
        _mockLocalization = new Mock<ILocalizationManager>();

        _subject = new LibraryController(
            new Mock<IProviderManager>().Object,
            new Mock<ISimilarItemsManager>().Object,
            _mockLibraryManager.Object,
            _mockUserManager.Object,
            new Mock<ICollectionManager>().Object,
            new Mock<IDtoService>().Object,
            _mockActivityManager.Object,
            _mockLocalization.Object,
            new Mock<ILibraryMonitor>().Object,
            new Mock<ILogger<LibraryController>>().Object,
            new Mock<IServerConfigurationManager>().Object);
    }

    public void Dispose()
    {
        BaseItem.MediaSourceManager = _previousMediaSourceManager!;
    }

    [Fact]
    public async Task GetDownload_NonexistentItem_ReturnsNotFound()
    {
        var userId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var user = CreateUser(canDownload: true);
        SetupUser(userId, user);

        _mockLibraryManager
            .Setup(l => l.GetItemById<BaseItem>(itemId, user))
            .Returns((BaseItem?)null);

        var result = await _subject.GetDownload(itemId);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetDownload_UserWithoutDownloadPermission_Video_ReturnsForbid()
    {
        var userId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var user = CreateUser(canDownload: false);
        SetupUser(userId, user);

        var video = new Video { Id = itemId, Path = FileVideoPath };
        _mockLibraryManager
            .Setup(l => l.GetItemById<BaseItem>(itemId, user))
            .Returns(video);

        var result = await _subject.GetDownload(itemId);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task GetDownload_UserWithoutDownloadPermission_Book_ReturnsPhysicalFile()
    {
        var userId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var user = CreateUser(canDownload: false);
        SetupUser(userId, user);

        var book = new Book { Id = itemId, Path = FileBookPath };
        _mockLibraryManager
            .Setup(l => l.GetItemById<BaseItem>(itemId, user))
            .Returns(book);

        var result = await _subject.GetDownload(itemId);

        var fileResult = Assert.IsType<PhysicalFileResult>(result);
        Assert.Equal(FileBookPath, fileResult.FileName);
    }

    [Fact]
    public async Task GetDownload_UserWithDownloadPermission_Video_ReturnsPhysicalFile()
    {
        var userId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var user = CreateUser(canDownload: true);
        SetupUser(userId, user);

        var video = new Video { Id = itemId, Path = FileVideoPath };
        _mockLibraryManager
            .Setup(l => l.GetItemById<BaseItem>(itemId, user))
            .Returns(video);

        var result = await _subject.GetDownload(itemId);

        var fileResult = Assert.IsType<PhysicalFileResult>(result);
        Assert.Equal(FileVideoPath, fileResult.FileName);
    }

    [Fact]
    public async Task GetDownload_ApiKeyCaller_Video_ReturnsPhysicalFile()
    {
        var itemId = Guid.NewGuid();
        SetupApiKeyCaller();

        var video = new Video { Id = itemId, Path = FileVideoPath };
        _mockLibraryManager
            .Setup(l => l.GetItemById<BaseItem>(itemId, null))
            .Returns(video);

        var result = await _subject.GetDownload(itemId);

        var fileResult = Assert.IsType<PhysicalFileResult>(result);
        Assert.Equal(FileVideoPath, fileResult.FileName);
    }

    private void SetupApiKeyCaller()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Role, UserRoles.Administrator),
            new Claim(InternalClaimTypes.UserId, Guid.Empty.ToString("N", CultureInfo.InvariantCulture))
        };

        _subject.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims))
            }
        };
    }

    private void SetupUser(Guid userId, User user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Role, UserRoles.User),
            new Claim(InternalClaimTypes.UserId, userId.ToString("N", CultureInfo.InvariantCulture))
        };

        _subject.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims))
            }
        };

        _mockUserManager
            .Setup(m => m.GetUserById(userId))
            .Returns(user);
    }

    private static User CreateUser(bool canDownload)
    {
        var user = new User("jellyfin", "auth-provider", "reset-provider");
        user.AddDefaultPermissions();
        user.SetPermission(PermissionKind.EnableContentDownloading, canDownload);
        return user;
    }
}
