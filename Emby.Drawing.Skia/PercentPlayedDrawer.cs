using SkiaSharp;
using MediaBrowser.Model.Drawing;
using System;

namespace Emby.Drawing.Skia
{
    public class PercentPlayedDrawer
    {
        private const int IndicatorHeight = 8;

        public void Process(SKCanvas canvas, ImageSize imageSize, double percent)
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
                canvas.DrawRect(SKRect.Create(0, (float)endY - IndicatorHeight, Convert.ToInt32(Math.Round(foregroundWidth)), (float)endY), paint);
            }
        }
    }
}
