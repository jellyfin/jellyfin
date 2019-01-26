using System.Globalization;
using MediaBrowser.Model.Drawing;
using SkiaSharp;

namespace Jellyfin.Drawing.Skia
{
    public static class UnplayedCountIndicator
    {
        private const int OffsetFromTopRightCorner = 38;

        public static void DrawUnplayedCountIndicator(SKCanvas canvas, ImageDimensions imageSize, int count)
        {
            var x = imageSize.Width - OffsetFromTopRightCorner;
            var text = count.ToString(CultureInfo.InvariantCulture);

            using (var paint = new SKPaint())
            {
                paint.Color = SKColor.Parse("#CC52B54B");
                paint.Style = SKPaintStyle.Fill;
                canvas.DrawCircle((float)x, OffsetFromTopRightCorner, 20, paint);
            }
            using (var paint = new SKPaint())
            {
                paint.Color = new SKColor(255, 255, 255, 255);
                paint.Style = SKPaintStyle.Fill;

                paint.TextSize = 24;
                paint.IsAntialias = true;

                var y = OffsetFromTopRightCorner + 9;

                if (text.Length == 1)
                {
                    x -= 7;
                }
                if (text.Length == 2)
                {
                    x -= 13;
                }
                else if (text.Length >= 3)
                {
                    x -= 15;
                    y -= 2;
                    paint.TextSize = 18;
                }

                canvas.DrawText(text, (float)x, y, paint);
            }
        }
    }
}
