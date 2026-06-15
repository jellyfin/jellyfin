using System;
using System.Runtime.Versioning;
using MediaBrowser.MediaEncoding.Encoder;
using Xunit;

namespace Jellyfin.MediaEncoding.Tests;

[SupportedOSPlatform("macos")]
public class ApplePlatformHelperTests
{
    [Fact]
    public void GetSysctlValue_CpuBrand_NotEmpty()
    {
        Assert.SkipUnless(OperatingSystem.IsMacOS(), "macOS-only test");

        Assert.NotEmpty(ApplePlatformHelper.GetSysctlValue("machdep.cpu.brand_string"));
    }
}
