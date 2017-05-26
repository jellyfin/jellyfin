using SkiaSharp;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Drawing;
using System.Globalization;
using System.Threading.Tasks;

using MediaBrowser.Controller.IO;
using MediaBrowser.Model.IO;

namespace Emby.Drawing.Skia
{
    public class UnplayedCountIndicator
    {
        private const int OffsetFromTopRightCorner = 38;

        private readonly IApplicationPaths _appPaths;
        private readonly IHttpClient _iHttpClient;
        private readonly IFileSystem _fileSystem;

        public UnplayedCountIndicator(IApplicationPaths appPaths, IHttpClient iHttpClient, IFileSystem fileSystem)
        {
            _appPaths = appPaths;
            _iHttpClient = iHttpClient;
            _fileSystem = fileSystem;
        }

        public void DrawUnplayedCountIndicator(SKCanvas canvas, ImageSize imageSize, int count)
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
                paint.Typeface = SKTypeface.FromFile(PlayedIndicatorDrawer.ExtractFont("robotoregular.ttf", _appPaths, _fileSystem));
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
