using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Entities;
using Xunit;

namespace Jellyfin.MediaEncoding.Tests
{
    public class EncodingHelperTests
    {
        [Theory]
        [InlineData(null, "")]
        [InlineData(Video3DFormat.MVC, "")]
        [InlineData(Video3DFormat.FullSideBySide, "crop=trunc(iw/4)*2:ih:0:0")]
        [InlineData(Video3DFormat.HalfSideBySide, "crop=trunc(iw/4)*2:ih:0:0,scale=iw*2:ih,setsar=sar=1")]
        [InlineData(Video3DFormat.FullTopAndBottom, "crop=iw:trunc(ih/4)*2:0:0")]
        [InlineData(Video3DFormat.HalfTopAndBottom, "crop=iw:trunc(ih/4)*2:0:0,scale=iw:ih*2,setsar=sar=1")]
        public void GetVideo3DFilter_Valid_Success(Video3DFormat? threedFormat, string expected)
        {
            Assert.Equal(expected, EncodingHelper.GetVideo3DFilter(threedFormat));
        }

        [Theory]
        [InlineData(null, 3840, 806, 3840, 806)]
        [InlineData(Video3DFormat.MVC, 3840, 806, 3840, 806)]
        [InlineData(Video3DFormat.FullSideBySide, 3840, 806, 1920, 806)]
        [InlineData(Video3DFormat.HalfSideBySide, 3840, 806, 3840, 806)]
        [InlineData(Video3DFormat.FullTopAndBottom, 3840, 806, 3840, 402)]
        [InlineData(Video3DFormat.HalfTopAndBottom, 3840, 806, 3840, 806)]
        public void GetVideo3DFlattenedSize_Valid_Success(Video3DFormat? threedFormat, int? width, int? height, int? expectedWidth, int? expectedHeight)
        {
            Assert.Equal((expectedWidth, expectedHeight), EncodingHelper.GetVideo3DFlattenedSize(threedFormat, width, height));
        }

        [Theory]
        [InlineData(Video3DFormat.FullSideBySide, 3840, 806, 1920, 806)]
        [InlineData(Video3DFormat.HalfSideBySide, 3840, 806, 1920, 806)]
        [InlineData(Video3DFormat.FullTopAndBottom, 3840, 806, 3840, 402)]
        [InlineData(Video3DFormat.HalfTopAndBottom, 3840, 806, 3840, 402)]
        public void GetVideo3DCropSize_Packed_ReturnsSingleView(Video3DFormat? threedFormat, int? width, int? height, int expectedWidth, int expectedHeight)
        {
            Assert.Equal((expectedWidth, expectedHeight), EncodingHelper.GetVideo3DCropSize(threedFormat, width, height));
        }

        [Theory]
        [InlineData(null, 3840, 806)]
        [InlineData(Video3DFormat.MVC, 3840, 806)]
        [InlineData(Video3DFormat.FullSideBySide, null, 806)]
        [InlineData(Video3DFormat.FullSideBySide, 3840, null)]
        public void GetVideo3DCropSize_NothingToCrop_ReturnsNull(Video3DFormat? threedFormat, int? width, int? height)
        {
            Assert.Null(EncodingHelper.GetVideo3DCropSize(threedFormat, width, height));
        }

        [Theory]
        [InlineData("scale", "vaapi", "nv12", "", "scale_vaapi=format=nv12")]
        [InlineData("vpp", "qsv", "nv12", "cw=1920:ch=806:cx=0:cy=0", "vpp_qsv=w=3840:h=806:cw=1920:ch=806:cx=0:cy=0:format=nv12")]
        [InlineData("vpp", "qsv", "", "cw=1920:ch=806:cx=0:cy=0", "vpp_qsv=w=3840:h=806:cw=1920:ch=806:cx=0:cy=0")]
        public void GetHwScaleFilter_CropArgs_ForcesExplicitSize(string prefix, string suffix, string format, string? cropArgs, string expected)
        {
            Assert.Equal(
                expected,
                EncodingHelper.GetHwScaleFilter(prefix, suffix, format, false, 3840, 806, null, null, null, null, cropArgs));
        }
    }
}
