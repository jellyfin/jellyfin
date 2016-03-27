using ImageMagickSharp;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Drawing;
using System;
using System.IO;
using System.Threading.Tasks;
using CommonIO;

namespace Emby.Drawing.ImageMagick
{
    public class PlayedIndicatorDrawer
    {
        private const int FontSize = 52;
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

        public async Task DrawPlayedIndicator(MagickWand wand, ImageSize imageSize)
        {
            var x = imageSize.Width - OffsetFromTopRightCorner;

            using (var draw = new DrawingWand())
            {
                using (PixelWand pixel = new PixelWand())
                {
                    pixel.Color = "#52B54B";
                    pixel.Opacity = 0.2;
                    draw.FillColor = pixel;
                    draw.DrawCircle(x, OffsetFromTopRightCorner, x - 20, OffsetFromTopRightCorner - 20);

                    pixel.Opacity = 0;
                    pixel.Color = "white";
                    draw.FillColor = pixel;
                    draw.Font = await DownloadFont("webdings.ttf", "https://github.com/MediaBrowser/Emby.Resources/raw/master/fonts/webdings.ttf", _appPaths, _iHttpClient, _fileSystem).ConfigureAwait(false);
                    draw.FontSize = FontSize;
                    draw.FontStyle = FontStyleType.NormalStyle;
                    draw.TextAlignment = TextAlignType.CenterAlign;
                    draw.FontWeight = FontWeightType.RegularStyle;
                    draw.TextAntialias = true;
                    draw.DrawAnnotation(x + 4, OffsetFromTopRightCorner + 14, "a");

                    draw.FillColor = pixel;
                    wand.CurrentImage.DrawImage(draw);
                }
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
			fileSystem.CreateDirectory(Path.GetDirectoryName(tempPath));

            using (var stream = typeof(PlayedIndicatorDrawer).Assembly.GetManifestResourceStream(namespacePath))
            {
                using (var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.Read))
                {
                    stream.CopyTo(fileStream);
                }
            }

			fileSystem.CreateDirectory(Path.GetDirectoryName(filePath));

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

			fileSystem.CreateDirectory(Path.GetDirectoryName(filePath));

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
