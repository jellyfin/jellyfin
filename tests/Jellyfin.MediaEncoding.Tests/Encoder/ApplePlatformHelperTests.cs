using System;
using System.Runtime.Versioning;
using MediaBrowser.MediaEncoding.Encoder;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using Xunit;

namespace Jellyfin.MediaEncoding.Tests;

[SupportedOSPlatform("macos")]
public class ApplePlatformHelperTests
{
    [Fact]
    public void GetSysctlValue_CpuBrand_NotEmpty()
    {
        Assert.SkipUnless(OperatingSystem.IsMacOS(), "macOS-only test");

        var value = ApplePlatformHelper.GetSysctlValue("machdep.cpu.brand_string");
        Assert.NotEmpty(value);

        // Make sure we don't include the null terminator
        Assert.DoesNotContain("\0", value, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("MacBookAir7,2", false)] // MacBook Air (13-inch, 2017)
    [InlineData("MacBookAir8,1", true)] // MacBook Air (Retina, 13-inch, 2018)
    [InlineData("MacBookPro13,3", false)] // MacBook Pro (15-inch, 2016)
    [InlineData("MacBookPro14,1", true)] // MacBook Pro (13-inch, 2017)
    [InlineData("MacBookPro16,1", true)] // MacBook Pro (16-inch, 2019)
    [InlineData("MacBook9,1", false)] // MacBook (Retina, 12-inch, Early 2016)
    [InlineData("MacBook10,1", true)] // MacBook (Retina, 12-inch, 2017)
    [InlineData("iMac17,1", false)] // iMac (Retina 5K, 27-inch, Late 2015)
    [InlineData("iMac18,3", true)] // iMac (Retina 5K, 27-inch, 2017)
    [InlineData("iMacPro1,1", true)] // iMac Pro (2017)
    [InlineData("Macmini7,1", false)] // Mac mini (Late 2014)
    [InlineData("Macmini8,1", true)] // Mac mini (2018)
    [InlineData("MacPro6,1", false)] // Mac Pro (Late 2013)
    [InlineData("MacPro7,1", true)] // Mac Pro (2019)
    [InlineData("VirtualMac2,1", false)]
    [InlineData("MacBookPro99999999999,1", false)]
    [InlineData("MacBookPro", false)]
    [InlineData("", false)]
    public void IsFullVideoToolboxIntelModel_Valid_Success(string model, bool expected)
    {
        Assert.Equal(expected, ApplePlatformHelper.IsFullVideoToolboxIntelModel(model));
    }

    [Theory]
    [InlineData("MacBookAir7,2", false, false, new[] { "h264" }, false)]
    [InlineData("MacBookPro14,1", false, false, new[] { "h264", "hevc" }, true)]
    [InlineData("MacBookAir10,1", true, false, new[] { "h264", "hevc" }, true)]
    [InlineData("Mac15,3", true, true, new[] { "h264", "hevc", "av1" }, true)]
    public void ApplyRecommendedVideoToolboxOptions_Valid_Success(string model, bool isAppleSilicon, bool hasAv1HardwareAccel, string[] expectedCodecs, bool expectedTonemapping)
    {
        var options = new EncodingOptions
        {
            EnableHardwareEncoding = false
        };

        ApplePlatformHelper.ApplyRecommendedVideoToolboxOptions(options, model, isAppleSilicon, hasAv1HardwareAccel);

        Assert.Equal(HardwareAccelerationType.videotoolbox, options.HardwareAccelerationType);
        Assert.True(options.EnableHardwareEncoding);
        Assert.Equal(expectedCodecs, options.HardwareDecodingCodecs);
        Assert.Equal(expectedTonemapping, options.EnableVideoToolboxTonemapping);
    }
}
