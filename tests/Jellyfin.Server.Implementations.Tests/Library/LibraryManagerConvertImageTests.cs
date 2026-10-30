using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoMoq;
using Emby.Naming.Common;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using Moq;
using Xunit;
using ServerLibraryManager = Emby.Server.Implementations.Library.LibraryManager;

namespace Jellyfin.Server.Implementations.Tests.Library;

public class LibraryManagerConvertImageTests
{
    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.Forbidden)]
    public async Task ConvertImageToLocal_RejectedDownload_KeepsTheResponseAsInnerException(HttpStatusCode status)
    {
        // The Live TV guide reads the provider's error body off the inner exception to stop
        // requesting images from an account that is refusing them.
        const string Body = "{\"response\":\"MAX_IMAGE_DOWNLOADS\",\"code\":5002}";
        var rejection = new HttpResponseBodyException("test", status, Body);

        var fixture = new Fixture().Customize(new AutoMoqCustomization());
        fixture.Register(() => new NamingOptions());
        var configuration = fixture.Freeze<Mock<IServerConfigurationManager>>();
        configuration.Setup(c => c.Configuration).Returns(new ServerConfiguration());
        configuration.Setup(c => c.ApplicationPaths.ProgramDataPath).Returns("/data");
        var providerManager = fixture.Freeze<Mock<IProviderManager>>();
        providerManager.Setup(p => p.SaveImage(It.IsAny<BaseItem>(), It.IsAny<string>(), It.IsAny<ImageType>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(rejection);
        var manager = fixture.Create<ServerLibraryManager>();

        var item = new Movie { Id = Guid.NewGuid(), Name = "Test" };
        var image = new ItemImageInfo { Path = "https://example.com/image.jpg", Type = ImageType.Primary };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => manager.ConvertImageToLocal(item, image, 0, removeOnFailure: false));

        Assert.Same(rejection, ex.InnerException);
    }
}
