using Jellyfin.LiveTv.Guide;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Entities;
using Xunit;

namespace Jellyfin.LiveTv.Tests.Guide;

public class GuideManagerImageTests
{
    private const string ImageUrl = "https://example.com/program.jpg";
    private const string CachedPath = "/config/metadata/library/ab/program.jpg";

    [Fact]
    public void UpdateImages_PreCachedImageWithUnchangedUrl_ReportsNoChange()
    {
        // PreCacheImages rewrites Path to the local file, so comparing Path against the guide URL
        // used to look like a change and re-download every image on every refresh (#17259).
        var program = new LiveTvProgram();
        SeedImage(program, ImageType.Primary, path: CachedPath, source: ImageUrl);

        var updated = GuideManager.UpdateImages(program, new ProgramInfo { ImageUrl = ImageUrl });

        Assert.False(updated);
        Assert.Equal(CachedPath, program.GetImagePath(ImageType.Primary, 0));
    }

    [Theory]
    [InlineData(ImageType.Primary)]
    [InlineData(ImageType.Thumb)]
    [InlineData(ImageType.Logo)]
    [InlineData(ImageType.Backdrop)]
    public void UpdateImages_PreCachedImageOfAnyType_ReportsNoChange(ImageType imageType)
    {
        var program = new LiveTvProgram();
        SeedImage(program, imageType, path: CachedPath, source: ImageUrl);

        var updated = GuideManager.UpdateImages(program, InfoWithUrl(imageType, ImageUrl));

        Assert.False(updated);
        Assert.Equal(CachedPath, program.GetImagePath(imageType, 0));
    }

    [Fact]
    public void UpdateImages_ChangedUrl_AppliesUrlAndRecordsSource()
    {
        const string NewUrl = "https://example.com/new.jpg";
        var program = new LiveTvProgram();
        SeedImage(program, ImageType.Primary, path: CachedPath, source: ImageUrl);

        var updated = GuideManager.UpdateImages(program, new ProgramInfo { ImageUrl = NewUrl });

        Assert.True(updated);

        var image = program.GetImageInfo(ImageType.Primary, 0);
        Assert.NotNull(image);
        Assert.Equal(NewUrl, image.Path);
        // Recorded up front so the next refresh compares against the new URL even if the download
        // has not happened yet.
        Assert.Equal(NewUrl, image.Source);
    }

    [Fact]
    public void UpdateImages_NotYetCachedImageWithUnchangedUrl_ReportsNoChange()
    {
        // Programs outside the pre-cache window keep the remote URL as their path.
        var program = new LiveTvProgram();
        SeedImage(program, ImageType.Primary, path: ImageUrl, source: ImageUrl);

        var updated = GuideManager.UpdateImages(program, new ProgramInfo { ImageUrl = ImageUrl });

        Assert.False(updated);
    }

    [Fact]
    public void UpdateImages_LegacyRowWithoutSourceStillHoldingUrl_ReportsNoChange()
    {
        // Rows written before Source was tracked fall back to comparing Path.
        var program = new LiveTvProgram();
        SeedImage(program, ImageType.Primary, path: ImageUrl, source: null);

        var updated = GuideManager.UpdateImages(program, new ProgramInfo { ImageUrl = ImageUrl });

        Assert.False(updated);
    }

    [Fact]
    public void UpdateImages_LegacyPreCachedRowWithoutSource_ReappliesOnce()
    {
        // The one-time cost after upgrading: a locally cached image with no recorded source cannot be
        // matched to the URL, so it is re-applied once and gains a Source for later refreshes.
        var program = new LiveTvProgram();
        SeedImage(program, ImageType.Primary, path: CachedPath, source: null);

        var updated = GuideManager.UpdateImages(program, new ProgramInfo { ImageUrl = ImageUrl });

        Assert.True(updated);
        Assert.Equal(ImageUrl, program.GetImageInfo(ImageType.Primary, 0)!.Source);
    }

    [Fact]
    public void UpdateImages_LocalTunerPathPreferredOverUrl()
    {
        const string TunerPath = "/tuner/images/program.png";
        var program = new LiveTvProgram();

        var updated = GuideManager.UpdateImages(
            program,
            new ProgramInfo { ImagePath = TunerPath, ImageUrl = ImageUrl });

        Assert.True(updated);

        var image = program.GetImageInfo(ImageType.Primary, 0);
        Assert.NotNull(image);
        Assert.Equal(TunerPath, image.Path);
        Assert.Equal(TunerPath, image.Source);

        // A second refresh with the same tuner path must not report a change.
        Assert.False(GuideManager.UpdateImages(
            program,
            new ProgramInfo { ImagePath = TunerPath, ImageUrl = ImageUrl }));
    }

    [Fact]
    public void UpdateImages_ProviderStoppedSupplyingImage_ReportsRemoval()
    {
        // The removal only happens in memory, so it has to be reported or it is never persisted.
        var program = new LiveTvProgram();
        SeedImage(program, ImageType.Thumb, path: CachedPath, source: ImageUrl);

        var updated = GuideManager.UpdateImages(program, new ProgramInfo());

        Assert.True(updated);
        Assert.False(program.HasImage(ImageType.Thumb));
    }

    [Fact]
    public void UpdateImages_NoImagesAtAll_ReportsNoChange()
    {
        var program = new LiveTvProgram();

        var updated = GuideManager.UpdateImages(program, new ProgramInfo());

        Assert.False(updated);
        Assert.Empty(program.ImageInfos);
    }

    private static ProgramInfo InfoWithUrl(ImageType imageType, string url) => imageType switch
    {
        ImageType.Thumb => new ProgramInfo { ThumbImageUrl = url },
        ImageType.Logo => new ProgramInfo { LogoImageUrl = url },
        ImageType.Backdrop => new ProgramInfo { BackdropImageUrl = url },
        _ => new ProgramInfo { ImageUrl = url }
    };

    private static void SeedImage(BaseItem item, ImageType imageType, string path, string? source)
        => item.SetImage(
            new ItemImageInfo
            {
                Path = path,
                Type = imageType,
                Source = source
            },
            0);
}
