using System.Globalization;
using ImageMagickSharp;
using MediaBrowser.Model.Drawing;

namespace MediaBrowser.Server.Implementations.Drawing
{
    public class UnplayedCountIndicator
    {
        private const int OffsetFromTopRightCorner = 38;

        public void DrawUnplayedCountIndicator(MagickWand wand, ImageSize imageSize, int count)
        {
            var x = imageSize.Width - OffsetFromTopRightCorner;
            var text = count.ToString(CultureInfo.InvariantCulture);

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
                    draw.Font = "Sans-Serif";
                    draw.FontStyle = FontStyleType.NormalStyle;
                    draw.TextAlignment = TextAlignType.CenterAlign;
                    draw.FontWeight = FontWeightType.RegularStyle;
                    draw.TextAntialias = true;

                    var fontSize = 30;
                    var y = OffsetFromTopRightCorner + 11;

                    if (text.Length == 1)
                    {
                        x += 2;
                    }
                    else if (text.Length == 2)
                    {
                        x += 1;
                    }
                    else if (text.Length >= 3)
                    {
                        x += 1;
                        y -= 2;
                        fontSize = 24;
                    }

                    draw.FontSize = fontSize;
                    draw.DrawAnnotation(x, y, text);

                    draw.FillColor = pixel;
                    wand.CurrentImage.DrawImage(draw);
                }

            }
        }
    }
}
