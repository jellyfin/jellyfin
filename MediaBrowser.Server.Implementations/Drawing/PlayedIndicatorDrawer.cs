using ImageMagickSharp;
using MediaBrowser.Model.Drawing;

namespace MediaBrowser.Server.Implementations.Drawing
{
    public class PlayedIndicatorDrawer
    {
        private const int FontSize = 52;
        private const int OffsetFromTopRightCorner = 38;

        public void DrawPlayedIndicator(MagickWand wand, ImageSize imageSize)
        {
            var x = imageSize.Width - OffsetFromTopRightCorner;

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
                    draw.Font = "Webdings";
                    draw.FontSize = FontSize;
                    draw.FontStyle = FontStyleType.NormalStyle;
                    draw.TextAlignment = TextAlignType.CenterAlign;
                    draw.FontWeight = FontWeightType.RegularStyle;
                    draw.TextAntialias = true;
                    draw.DrawAnnotation(x + 4, OffsetFromTopRightCorner + 14, "a");

                    draw.FillColor = pixel;
                    wand.CurrentImage.DrawImage(draw);
                }

            }
        }
    }
}
