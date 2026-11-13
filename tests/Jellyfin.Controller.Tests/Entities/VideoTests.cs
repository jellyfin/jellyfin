using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using Xunit;

namespace Jellyfin.Controller.Tests.Entities;

public class VideoTests
{
    [Theory]
    [InlineData(null, IsoType.BluRay, IsoType.BluRay, true)]
    [InlineData(IsoType.Dvd, IsoType.BluRay, IsoType.BluRay, true)]
    [InlineData(IsoType.BluRay, null, IsoType.BluRay, false)]
    [InlineData(IsoType.BluRay, IsoType.BluRay, IsoType.BluRay, false)]
    [InlineData(null, null, null, false)]
    public void UpdateFromResolvedItem_IsoType_UpdatesWhenResolved(IsoType? current, IsoType? resolved, IsoType? expected, bool updated)
    {
        var video = new Video { VideoType = VideoType.Iso, IsoType = current };
        var newVideo = new Video { VideoType = VideoType.Iso, IsoType = resolved };

        var updateType = video.UpdateFromResolvedItem(newVideo);

        Assert.Equal(expected, video.IsoType);
        Assert.Equal(updated, updateType.HasFlag(ItemUpdateType.MetadataImport));
    }
}
