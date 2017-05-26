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

namespace Emby.Drawing.Skia
{
    public class PlayedIndicatorDrawer
    {
        private const int FontSize = 42;
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
                paint.Typeface = SKTypeface.FromFile(await DownloadFont("webdings.ttf", "https://github.com/MediaBrowser/Emby.Resources/raw/master/fonts/webdings.ttf",
                    _appPaths, _iHttpClient, _fileSystem).ConfigureAwait(false));
                paint.TextSize = FontSize;
                paint.IsAntialias = true;

                canvas.DrawText("a", (float)x-20, OffsetFromTopRightCorner + 12, paint);
            }
        }

        internal static string ExtractFont(string name, IApplicationPaths paths, IFileSystem fileSystem)
        {
            var filePath = Path.Combine(paths.ProgramDataPath, "fonts", name);

            if (fileSystem.FileExists(filePath))
            {
                return filePath;
            }

            var namespacePath = typeof(PlayedIndicatorDrawer).Namespace + ".fonts." + name;
            var tempPath = Path.Combine(paths.TempDirectory, Guid.NewGuid().ToString("N") + ".ttf");
            fileSystem.CreateDirectory(fileSystem.GetDirectoryName(tempPath));

            using (var stream = typeof(PlayedIndicatorDrawer).GetTypeInfo().Assembly.GetManifestResourceStream(namespacePath))
            {
                using (var fileStream = fileSystem.GetFileStream(tempPath, FileOpenMode.Create, FileAccessMode.Write, FileShareMode.Read))
                {
                    stream.CopyTo(fileStream);
                }
            }

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
                Progress = new Progress<double>()

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
