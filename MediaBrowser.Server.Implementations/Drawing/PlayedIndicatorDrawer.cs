using ImageMagickSharp;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Drawing;
using System;
using System.IO;

namespace MediaBrowser.Server.Implementations.Drawing
{
    public class PlayedIndicatorDrawer
    {
        private const int FontSize = 52;
        private const int OffsetFromTopRightCorner = 38;

        private readonly IApplicationPaths _appPaths;

        public PlayedIndicatorDrawer(IApplicationPaths appPaths)
        {
            _appPaths = appPaths;
        }

        public void DrawPlayedIndicator(MagickWand wand, ImageSize imageSize)
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
                    draw.Font = ExtractFont("webdings.ttf", _appPaths);
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

        internal static string ExtractFont(string name, IApplicationPaths paths)
        {
            var filePath = Path.Combine(paths.ProgramDataPath, "fonts", name);

            if (File.Exists(filePath))
            {
                return filePath;
            }

            var namespacePath = typeof(PlayedIndicatorDrawer).Namespace + ".fonts." + name;
            var tempPath = Path.Combine(paths.TempDirectory, Guid.NewGuid().ToString("N") + ".ttf");
            Directory.CreateDirectory(Path.GetDirectoryName(tempPath));

            using (var stream = typeof(PlayedIndicatorDrawer).Assembly.GetManifestResourceStream(namespacePath))
            {
                using (var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.Read))
                {
                    stream.CopyTo(fileStream);
                }
            }

            Directory.CreateDirectory(Path.GetDirectoryName(filePath));

            try
            {
                File.Copy(tempPath, filePath, false);
            }
            catch (IOException)
            {
                
            }

            return tempPath;
        }
    }
}
