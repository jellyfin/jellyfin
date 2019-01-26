using System;
using MediaBrowser.Model.Drawing;
using SkiaSharp;

namespace Jellyfin.Drawing.Skia
{
    public static class PercentPlayedDrawer
    {
        private const int IndicatorHeight = 8;

        public static void Process(SKCanvas canvas, ImageDimensions imageSize, double percent)
        {
            using (var paint = new SKPaint())
            {
                var endX = imageSize.Width - 1;
                var endY = imageSize.Height - 1;

                paint.Color = SKColor.Parse("#99000000");
                paint.Style = SKPaintStyle.Fill;
                canvas.DrawRect(SKRect.Create(0, (float)endY - IndicatorHeight, (float)endX, (float)endY), paint);

                double foregroundWidth = endX;
                foregroundWidth *= percent;
                foregroundWidth /= 100;

                paint.Color = SKColor.Parse("#FF52B54B");
                canvas.DrawRect(SKRect.Create(0, (float)endY - IndicatorHeight, Convert.ToInt32(foregroundWidth), (float)endY), paint);
            }
        }
    }
}
