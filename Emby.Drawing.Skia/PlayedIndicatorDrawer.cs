using SkiaSharp;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.IO;

namespace Emby.Drawing.Skia
{
    public class PlayedIndicatorDrawer
    {
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

        public void DrawPlayedIndicator(SKCanvas canvas, ImageSize imageSize)
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

                canvas.DrawText(text, (float)x-20, OffsetFromTopRightCorner + 12, paint);
            }
        }
    }
}
