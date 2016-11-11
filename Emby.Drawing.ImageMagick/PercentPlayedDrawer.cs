using ImageMagickSharp;
using System;

namespace Emby.Drawing.ImageMagick
{
    public class PercentPlayedDrawer
    {
        private const int IndicatorHeight = 8;

        public void Process(MagickWand wand, double percent)
        {
            var currentImage = wand.CurrentImage;
            var height = currentImage.Height;

            using (var draw = new DrawingWand())
            {
                using (PixelWand pixel = new PixelWand())
                {
                    var endX = currentImage.Width - 1;
                    var endY = height - 1;

                    pixel.Color = "black";
                    pixel.Opacity = 0.4;
                    draw.FillColor = pixel;
                    draw.DrawRectangle(0, endY - IndicatorHeight, endX, endY);

                    double foregroundWidth = endX;
                    foregroundWidth *= percent;
                    foregroundWidth /= 100;

                    pixel.Color = "#52B54B";
                    pixel.Opacity = 0;
                    draw.FillColor = pixel;
                    draw.DrawRectangle(0, endY - IndicatorHeight, Convert.ToInt32(Math.Round(foregroundWidth)), endY);
                    wand.CurrentImage.DrawImage(draw);
                }
            }
        }
    }
}
