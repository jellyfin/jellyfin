using System;
using System.Linq;
using MediaBrowser.Controller.Utilities;
using Xunit;

namespace Jellyfin.Controller.Tests.Utilities;

public class ImageOrderingUtilitiesTests
{
    [Theory]
    [InlineData(null, int.MaxValue)]
    [InlineData("", int.MaxValue)]
    [InlineData("/media/Movie/fanart.jpg", int.MaxValue)]
    [InlineData("/media/Movie/fanart1.jpg", 1)]
    [InlineData("/media/Movie/fanart-002.jpg", 2)]
    [InlineData("/media/Movie/fanart10.jpg", 10)]
    [InlineData(@"C:\Media\Movie\backdrop123.jpg", 123)]
    public void GetNumericImageIndex_ReturnsTrailingNumberOnly(string? path, int expected)
    {
        Assert.Equal(expected, ImageOrderingUtilities.GetNumericImageIndex(path));
    }

    [Theory]
    [InlineData("/media/Movie/Movie-fanart.jpg", "Movie", 0)]
    [InlineData("/media/Movie/fanart.jpg", "Movie", 1)]
    [InlineData("/media/Movie/fanart-1.jpg", "Movie", 2)]
    [InlineData("/media/Movie/background-1.jpg", "Movie", 3)]
    [InlineData("/media/Movie/art-1.jpg", "Movie", 4)]
    [InlineData("/media/Movie/extrafanart/fanart.jpg", "Movie", 5)]
    [InlineData(@"C:\Media\Movie\extrafanart\fanart1.jpg", "Movie", 5)]
    [InlineData("/media/Movie/backdrop1.jpg", "Movie", 6)]
    [InlineData("/media/Movie/unknown.jpg", "Movie", ImageOrderingUtilities.UnknownImagePriority)]
    public void GetImageOrderPriority_ReturnsExpectedPriority(string path, string mediaFileName, int expected)
    {
        Assert.Equal(expected, ImageOrderingUtilities.GetImageOrderPriority(path, mediaFileName));
    }

    [Fact]
    public void ImageOrdering_UsesPriorityThenNaturalNumberThenPath()
    {
        var paths = new[]
        {
            "/media/Movie/backdrop1.jpg",
            "/media/Movie/fanart-10.jpg",
            "/media/Movie/Movie-fanart.jpg",
            "/media/Movie/fanart-2.jpg",
            "/media/Movie/fanart-1.jpg"
        };

        var ordered = paths
            .OrderBy(path => ImageOrderingUtilities.GetImageOrderPriority(path, "Movie"))
            .ThenBy(ImageOrderingUtilities.GetNumericImageIndex)
            .ThenBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Assert.Equal(
            [
                "/media/Movie/Movie-fanart.jpg",
                "/media/Movie/fanart-1.jpg",
                "/media/Movie/fanart-2.jpg",
                "/media/Movie/fanart-10.jpg",
                "/media/Movie/backdrop1.jpg"
            ],
            ordered);
    }
}
