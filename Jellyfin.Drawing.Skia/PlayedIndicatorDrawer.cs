using MediaBrowser.Model.Drawing;
using SkiaSharp;

namespace Jellyfin.Drawing.Skia
{
    /// <summary>
    /// Static helper class for drawing 'played' indicators.
    /// </summary>
    public static class PlayedIndicatorDrawer
    {
        private const int OffsetFromTopRightCorner = 38;

        /// <summary>
        /// Draw a 'played' indicator in the top right corner of a canvas.
        /// </summary>
        /// <param name="canvas">The canvas to draw the indicator on.</param>
        /// <param name="imageSize">
        /// The dimensions of the image to draw the indicator on. The width is used to determine the x-position of the
        /// indicator.
        /// </param>
        public static void DrawPlayedIndicator(SKCanvas canvas, ImageDimensions imageSize)
        {
            var x = imageSize.Width - OffsetFromTopRightCorner;

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

                paint.TextSize = 30;
                paint.IsAntialias = true;

                // or:
                // var emojiChar = 0x1F680;
                const string Text = "✔️";
                var emojiChar = StringUtilities.GetUnicodeCharacterCode(Text, SKTextEncoding.Utf32);

                // ask the font manager for a font with that character
                paint.Typeface = SKFontManager.Default.MatchCharacter(emojiChar);

                canvas.DrawText(Text, (float)x - 20, OffsetFromTopRightCorner + 12, paint);
            }
        }
    }
}
