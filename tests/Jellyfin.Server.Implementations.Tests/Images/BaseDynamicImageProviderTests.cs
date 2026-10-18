using System;
using System.Collections.Generic;
using Emby.Server.Implementations.Images;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Images;

public sealed class BaseDynamicImageProviderTests
{
    private const string ImagePath = "/metadata/folder.png";

    [Fact]
    public void HasChangedByDate_StoredTimeTruncatedToMicroseconds_NoChange()
    {
        var fileTime = new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddTicks(1234567);
        var storedTime = fileTime.AddTicks(-(fileTime.Ticks % 10));

        Assert.False(HasChangedByDate(storedTime, fileTime));
    }

    [Fact]
    public void HasChangedByDate_FileWrittenLater_Changed()
    {
        var storedTime = new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        Assert.True(HasChangedByDate(storedTime, storedTime.AddSeconds(2)));
    }

    private static bool HasChangedByDate(DateTime storedTime, DateTime fileTime)
    {
        var fileSystem = new Mock<IFileSystem>();
        fileSystem.Setup(fs => fs.GetLastWriteTimeUtc(ImagePath))
            .Returns(fileTime);

        var provider = new TestableDynamicImageProvider(fileSystem.Object);

        return provider.ImageHasChangedByDate(new Folder(), new ItemImageInfo { Path = ImagePath, DateModified = storedTime });
    }

    private sealed class TestableDynamicImageProvider : BaseDynamicImageProvider<Folder>
    {
        public TestableDynamicImageProvider(IFileSystem fileSystem)
            : base(fileSystem, Mock.Of<IProviderManager>(), Mock.Of<IApplicationPaths>(), Mock.Of<IImageProcessor>())
        {
        }

        public bool ImageHasChangedByDate(BaseItem item, ItemImageInfo image) => HasChangedByDate(item, image);

        protected override IReadOnlyList<BaseItem> GetItemsWithImages(BaseItem item) => [];
    }
}
