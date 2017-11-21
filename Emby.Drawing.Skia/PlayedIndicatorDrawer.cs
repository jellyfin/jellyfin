using SkiaSharp;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Drawing;
using System;
using System.IO;
using System.Threading.Tasks;

using MediaBrowser.Controller.IO;
using MediaBrowser.Model.IO;
using System.Reflection;
using MediaBrowser.Common.Progress;

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

        public async Task DrawPlayedIndicator(SKCanvas canvas, ImageSize imageSize)
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

        internal static async Task<string> DownloadFont(string name, string url, IApplicationPaths paths, IHttpClient httpClient, IFileSystem fileSystem)
        {
            var filePath = Path.Combine(paths.ProgramDataPath, "fonts", name);

            if (fileSystem.FileExists(filePath))
            {
                return filePath;
            }

            var tempPath = await httpClient.GetTempFile(new HttpRequestOptions
            {
                Url = url,
                Progress = new SimpleProgress<double>()

            }).ConfigureAwait(false);

            fileSystem.CreateDirectory(fileSystem.GetDirectoryName(filePath));

            try
            {
                fileSystem.CopyFile(tempPath, filePath, false);
            }
            catch (IOException)
            {

            }

            return tempPath;
        }
    }
}
