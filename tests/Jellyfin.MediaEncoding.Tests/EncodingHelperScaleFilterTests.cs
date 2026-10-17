using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using Xunit;

namespace Jellyfin.MediaEncoding.Tests;

public class EncodingHelperScaleFilterTests
{
    private const string X264 = "libx264";
    private const string V4l2 = "h264_v4l2m2m";
    private const string Mjpeg = "mjpeg";
    private const int SrcW = 1920;
    private const int SrcH = 1080;

    private const string HsbsFilter = @"crop=iw/2:ih:0:0,scale=(iw*2):ih,setdar=dar=a,crop=min(iw\,ih*dar):min(ih\,iw/dar):(iw-min(iw\,iw*sar))/2:(ih - min (ih\,ih/sar))/2,setsar=sar=1,scale=720:trunc(720/dar/2)*2";
    private const string FsbsFilter = @"crop=iw/2:ih:0:0,setdar=dar=a,crop=min(iw\,ih*dar):min(ih\,iw/dar):(iw-min(iw\,iw*sar))/2:(ih - min (ih\,ih/sar))/2,setsar=sar=1,scale=720:trunc(720/dar/2)*2";
    private const string HtabFilter = @"crop=iw:ih/2:0:0,scale=(iw*2):ih,setdar=dar=a,crop=min(iw\,ih*dar):min(ih\,iw/dar):(iw-min(iw\,iw*sar))/2:(ih - min (ih\,ih/sar))/2,setsar=sar=1,scale=720:trunc(720/dar/2)*2";
    private const string FtabFilter = @"crop=iw:ih/2:0:0,setdar=dar=a,crop=min(iw\,ih*dar):min(ih\,iw/dar):(iw-min(iw\,iw*sar))/2:(ih - min (ih\,ih/sar))/2,setsar=sar=1,scale=720:trunc(720/dar/2)*2";

    private static EncodingJobInfo NewState() => new EncodingJobInfo(TranscodingJobType.Hls);

    private static EncodingOptions NewOpts() => new EncodingOptions();

    [Theory]
    [InlineData("scale", "", "nv12", false, 1920, 1080, 1280, 720, null, null, "")]
    [InlineData("scale", "vaapi", null, false, 1920, 1080, null, null, null, null, "")]
    [InlineData("scale", "vaapi", "nv12", false, 1920, 1080, null, null, null, null, "scale_vaapi=format=nv12")]
    [InlineData("scale", "vaapi", null, false, 1920, 1080, 1280, 720, null, null, "scale_vaapi=w=1280:h=720")]
    [InlineData("scale", "vaapi", "nv12", false, 1920, 1080, 1280, 720, null, null, "scale_vaapi=w=1280:h=720:format=nv12")]
    [InlineData("vpp", "qsv", "nv12", false, 1920, 1080, 1280, 720, null, null, "vpp_qsv=w=1280:h=720:format=nv12")]
    [InlineData(null, "vaapi", "nv12", false, 1920, 1080, 1280, 720, null, null, "scale_vaapi=w=1280:h=720:format=nv12")]
    [InlineData("scale", "rkrga", "nv12", true, 1280, 720, null, null, 720, null, "scale_rkrga=w=404:h=720:format=nv12")]
    [InlineData("scale", "vaapi", "nv12", false, 7680, 4320, null, null, null, null, "scale_vaapi=w=4096:h=2304:format=nv12")]
    [InlineData("scale", "qsv", "nv12", false, 1920, 1080, 1280, 720, null, null, "scale_qsv=w=1280:h=720:format=nv12")]
    [InlineData("scale", "cuda", "yuv420p", false, 1920, 1080, 1280, 720, null, null, "scale_cuda=w=1280:h=720:format=yuv420p")]
    [InlineData("scale", "vt", "nv12", false, 1920, 1080, 1280, 720, null, null, "scale_vt=w=1280:h=720:format=nv12")]
    public void GetHwScaleFilter_ProducesExpectedString(string? prefix, string suffix, string? format, bool swap, int? videoW, int? videoH, int? reqW, int? reqH, int? maxW, int? maxH, string expected)
    {
        var result = EncodingHelper.GetHwScaleFilter(prefix, suffix, format, swap, videoW, videoH, reqW, reqH, maxW, maxH);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(X264, 1280, 720, null, null, "scale=trunc(1280/2)*2:trunc(720/2)*2")]
    [InlineData(V4l2, 1280, 720, null, null, "scale=trunc(1280/64)*64:trunc(720/2)*2")]
    [InlineData(X264, null, null, 1280, 720, @"scale=trunc(min(max(iw\,ih*a)\,min(1280\,720*a))/2)*2:trunc(min(max(iw/a\,ih)\,min(1280/a\,720))/2)*2")]
    [InlineData(V4l2, null, null, 1280, 720, @"scale=trunc(min(max(iw\,ih*a)\,min(1280\,720*a))/64)*64:trunc(min(max(iw/a\,ih)\,min(1280/a\,720))/2)*2")]
    [InlineData(Mjpeg, null, null, 1280, 720, @"scale=trunc(min(max(iw\,ih*(a*sar))\,min(1280\,720*(a*sar)))/2)*2:trunc(min(max(iw/(a*sar)\,ih)\,min(1280/(a*sar)\,720))/2)*2")]
    [InlineData(X264, 1280, null, null, null, "scale=1280:trunc(ow/a/2)*2")]
    [InlineData(Mjpeg, 1280, null, null, null, "scale=1280:trunc(ow/(a*sar)/2)*2")]
    [InlineData(X264, null, 720, null, null, "scale=trunc(oh*a/2)*2:720")]
    [InlineData(V4l2, null, 720, null, null, "scale=trunc(oh*a/64)*64:720")]
    [InlineData(X264, null, null, 1280, null, @"scale=trunc(min(max(iw\,ih*a)\,1280)/2)*2:trunc(ow/a/2)*2")]
    [InlineData(X264, null, null, null, 720, @"scale=trunc(oh*a/2)*2:min(max(iw/a\,ih)\,720)")]
    [InlineData(X264, null, null, null, null, "")]
    public void GetSwScaleFilter_NonThreeD_ProducesExpectedString(string encoder, int? reqW, int? reqH, int? maxW, int? maxH, string expectedFilter)
    {
        var filter = EncodingHelper.GetSwScaleFilter(NewState(), NewOpts(), encoder, SrcW, SrcH, null, reqW, reqH, maxW, maxH);
        Assert.Equal(expectedFilter, filter);
    }

    [Theory]
    [InlineData(Video3DFormat.HalfSideBySide, 720, 404, HsbsFilter)]
    [InlineData(Video3DFormat.FullSideBySide, 720, 404, FsbsFilter)]
    [InlineData(Video3DFormat.HalfTopAndBottom, 720, 404, HtabFilter)]
    [InlineData(Video3DFormat.FullTopAndBottom, 720, 404, FtabFilter)]
    [InlineData(Video3DFormat.HalfSideBySide, 720, null, HsbsFilter)]
    public void GetSwScaleFilter_ThreeD_ProducesExpectedString(Video3DFormat format, int reqW, int? reqH, string expectedFilter)
    {
        var filter = EncodingHelper.GetSwScaleFilter(NewState(), NewOpts(), X264, SrcW, SrcH, format, reqW, reqH, null, null);
        Assert.Equal(expectedFilter, filter);
    }
}
