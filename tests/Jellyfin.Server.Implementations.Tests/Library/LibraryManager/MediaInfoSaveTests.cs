using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoMoq;
using Emby.Naming.Common;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Library.LibraryManager;

public class MediaInfoSaveTests
{
    [Fact]
    public async Task UpdateItems_TechnicalScope_UsesScopedSaveWithoutMetadataSavers()
    {
        var fixture = new Fixture().Customize(new AutoMoqCustomization());
        fixture.Register(() => new NamingOptions());
        fixture.Freeze<Mock<IServerConfigurationManager>>()
            .Setup(c => c.ApplicationPaths.ProgramDataPath).Returns("/data");
        var persistence = fixture.Freeze<Mock<IItemPersistenceService>>();
        var providers = fixture.Freeze<Mock<IProviderManager>>();
        fixture.Inject(new Lazy<IProviderManager>(() => providers.Object));
        var manager = fixture.Create<Emby.Server.Implementations.Library.LibraryManager>();
        var item = new Movie { Id = Guid.NewGuid(), Path = "/media/movie.mkv" };
        var stamp = item.DateLastSaved;
        await manager.UpdateItemsAsync([item], null!, ItemUpdateType.MediaInfo, TestContext.Current.CancellationToken);
        persistence.Verify(p => p.SaveMediaInfoAsync(item, It.IsAny<CancellationToken>()), Times.Once);
        persistence.Verify(p => p.SaveItems(It.IsAny<IReadOnlyList<BaseItem>>(), It.IsAny<CancellationToken>()), Times.Never);
        providers.VerifyNoOtherCalls();
        Assert.Equal(stamp, item.DateLastSaved);
    }
}
