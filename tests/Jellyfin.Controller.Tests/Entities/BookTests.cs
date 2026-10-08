using System;
using Jellyfin.Data;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.MediaInfo;
using Moq;
using Xunit;

namespace Jellyfin.Controller.Tests.Entities;

[Collection("LibraryManagerTests")]
public sealed class BookTests : IDisposable
{
    private const string FilePath = "/media/books/book.epub";
    private const string RemotePath = "https://example.com/book.epub";

    private readonly IMediaSourceManager? _previousMediaSourceManager = BaseItem.MediaSourceManager;

    public BookTests()
    {
        var mediaSourceManager = new Mock<IMediaSourceManager>();
        mediaSourceManager.Setup(x => x.GetPathProtocol(FilePath)).Returns(MediaProtocol.File);
        mediaSourceManager.Setup(x => x.GetPathProtocol(RemotePath)).Returns(MediaProtocol.Http);
        BaseItem.MediaSourceManager = mediaSourceManager.Object;
    }

    public void Dispose()
    {
        BaseItem.MediaSourceManager = _previousMediaSourceManager!;
    }

    [Fact]
    public void CanDownload_UserWithoutDownloadPermission_FileBook_ReturnsTrue()
    {
        var book = new Book { Path = FilePath };

        Assert.True(book.CanDownload(CreateUser(canDownload: false)));
    }

    [Fact]
    public void CanDownload_UserWithDownloadPermission_FileBook_ReturnsTrue()
    {
        var book = new Book { Path = FilePath };

        Assert.True(book.CanDownload(CreateUser(canDownload: true)));
    }

    [Fact]
    public void CanDownload_UserWithoutDownloadPermission_RemoteBook_ReturnsFalse()
    {
        var book = new Book { Path = RemotePath };

        Assert.False(book.CanDownload(CreateUser(canDownload: false)));
    }

    [Fact]
    public void CanDownload_UserWithoutDownloadPermission_AudioBook_ReturnsFalse()
    {
        var audioBook = new AudioBook { Path = FilePath };

        Assert.False(audioBook.CanDownload(CreateUser(canDownload: false)));
    }

    [Fact]
    public void CanDownload_UserWithDownloadPermission_AudioBook_ReturnsTrue()
    {
        var audioBook = new AudioBook { Path = FilePath };

        Assert.True(audioBook.CanDownload(CreateUser(canDownload: true)));
    }

    [Fact]
    public void CanDownload_UserWithoutDownloadPermission_Video_ReturnsFalse()
    {
        var video = new Video { Path = FilePath };

        Assert.False(video.CanDownload(CreateUser(canDownload: false)));
    }

    private static User CreateUser(bool canDownload)
    {
        var user = new User("jellyfin", "auth-provider", "reset-provider");
        user.AddDefaultPermissions();
        user.SetPermission(PermissionKind.EnableContentDownloading, canDownload);
        return user;
    }
}
