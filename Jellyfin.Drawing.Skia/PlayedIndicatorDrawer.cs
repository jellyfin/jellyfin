using MediaBrowser.Model.Drawing;
using SkiaSharp;

namespace Jellyfin.Drawing.Skia
{
    public static class PlayedIndicatorDrawer
    {
        private const int OffsetFromTopRightCorner = 38;

        public static void DrawPlayedIndicator(SKCanvas canvas, ImageDimensions imageSize)
        {
            var x = imageSize.Width - OffsetFromTopRightCorner;

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

                paint.TextSize = 30;
                paint.IsAntialias = true;

                var text = "✔️";
                var emojiChar = StringUtilities.GetUnicodeCharacterCode(text, SKTextEncoding.Utf32);
                // or:
                //var emojiChar = 0x1F680;

                // ask the font manager for a font with that character
                var fontManager = SKFontManager.Default;
                var emojiTypeface = fontManager.MatchCharacter(emojiChar);

                paint.Typeface = emojiTypeface;

                canvas.DrawText(text, (float)x - 20, OffsetFromTopRightCorner + 12, paint);
            }
        }
    }
}
