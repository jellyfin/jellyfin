using ImageMagickSharp;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.IO;

namespace Emby.Drawing.ImageMagick
{
    public class PlayedIndicatorDrawer
    {
        private const int FontSize = 52;
        private const int OffsetFromTopRightCorner = 38;

        private readonly IApplicationPaths _appPaths;
        private readonly IHttpClient _iHttpClient;
        private readonly IFileSystem _fileSystem;

        public PlayedIndicatorDrawer(IApplicationPaths appPaths, IHttpClient iHttpClient, IFileSystem fileSystem)
        {
            _appPaths = appPaths;
            _iHttpClient = iHttpClient;
            _fileSystem = fileSystem;
        }

        public void DrawPlayedIndicator(MagickWand wand, ImageSize imageSize)
        {
            double x = imageSize.Width - OffsetFromTopRightCorner;

            using (var draw = new DrawingWand())
            {
                using (PixelWand pixel = new PixelWand())
                {
                    pixel.Color = "#52B54B";
                    pixel.Opacity = 0.2;
                    draw.FillColor = pixel;
                    draw.DrawCircle(x, OffsetFromTopRightCorner, x - 20, OffsetFromTopRightCorner - 20);

                    pixel.Opacity = 0;
                    pixel.Color = "white";
                    draw.FillColor = pixel;
                    draw.FontSize = FontSize;
                    draw.FontStyle = FontStyleType.NormalStyle;
                    draw.TextAlignment = TextAlignType.CenterAlign;
                    draw.FontWeight = FontWeightType.RegularStyle;
                    draw.TextAntialias = true;
                    draw.DrawAnnotation(x + 4, OffsetFromTopRightCorner + 14, "✓");

                    draw.FillColor = pixel;
                    wand.CurrentImage.DrawImage(draw);
                }
            }
        }
    }
}
