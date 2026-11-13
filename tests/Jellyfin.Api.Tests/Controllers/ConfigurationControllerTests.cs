using Jellyfin.Api.Controllers;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Jellyfin.Api.Tests.Controllers;

public class ConfigurationControllerTests
{
    private readonly ConfigurationController _subject;
    private readonly Mock<IServerConfigurationManager> _mockConfig;
    private readonly Mock<IMediaEncoder> _mockMediaEncoder;
    private readonly EncodingOptions _encodingOptions;

    public ConfigurationControllerTests()
    {
        _encodingOptions = new EncodingOptions
        {
            EncodingThreadCount = 2
        };
        _mockConfig = new Mock<IServerConfigurationManager>();
        _mockConfig.Setup(m => m.GetConfiguration("encoding")).Returns(_encodingOptions);
        _mockMediaEncoder = new Mock<IMediaEncoder>();
        _subject = new ConfigurationController(_mockConfig.Object, _mockMediaEncoder.Object);
    }

    [Fact]
    public void GetRecommendedEncodingOptions_Found_ReturnsCopyWithoutSaving()
    {
        _mockMediaEncoder
            .Setup(m => m.ApplyRecommendedHardwareAcceleration(It.IsAny<EncodingOptions>()))
            .Callback<EncodingOptions>(o => o.HardwareAccelerationType = HardwareAccelerationType.videotoolbox)
            .Returns(true);

        var result = _subject.GetRecommendedEncodingOptions();

        Assert.NotNull(result.Value);
        Assert.Equal(HardwareAccelerationType.videotoolbox, result.Value.HardwareAccelerationType);
        Assert.Equal(2, result.Value.EncodingThreadCount);
        Assert.Equal(HardwareAccelerationType.none, _encodingOptions.HardwareAccelerationType);
        _mockConfig.Verify(m => m.SaveConfiguration(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
    }

    [Fact]
    public void GetRecommendedEncodingOptions_NotFound_ReturnsNotFound()
    {
        _mockMediaEncoder
            .Setup(m => m.ApplyRecommendedHardwareAcceleration(It.IsAny<EncodingOptions>()))
            .Returns(false);

        var result = _subject.GetRecommendedEncodingOptions();

        Assert.IsType<NotFoundResult>(result.Result);
    }
}
