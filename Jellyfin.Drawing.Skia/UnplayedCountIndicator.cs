using System.Globalization;
using MediaBrowser.Model.Drawing;
using SkiaSharp;

namespace Jellyfin.Drawing.Skia
{
    /// <summary>
    /// Static helper class for drawing unplayed count indicators.
    /// </summary>
    public static class UnplayedCountIndicator
    {
        /// <summary>
        /// The x-offset used when drawing an unplayed count indicator.
        /// </summary>
        private const int OffsetFromTopRightCorner = 38;

        /// <summary>
        /// Draw an unplayed count indicator in the top right corner of a canvas.
        /// </summary>
        /// <param name="canvas">The canvas to draw the indicator on.</param>
        /// <param name="imageSize">
        /// The dimensions of the image to draw the indicator on. The width is used to determine the x-position of the
        /// indicator.
        /// </param>
        /// <param name="count">The number to draw in the indicator.</param>
        public static void DrawUnplayedCountIndicator(SKCanvas canvas, ImageDimensions imageSize, int count)
        {
            var x = imageSize.Width - OffsetFromTopRightCorner;
            var text = count.ToString(CultureInfo.InvariantCulture);

            using (var paint = new SKPaint())
            {
                paint.Color = SKColor.Parse("#CC00A4DC");
                paint.Style = SKPaintStyle.Fill;
                canvas.DrawCircle(x, OffsetFromTopRightCorner, 20, paint);
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

                canvas.DrawText(text, x, y, paint);
            }
        }
    }
}
