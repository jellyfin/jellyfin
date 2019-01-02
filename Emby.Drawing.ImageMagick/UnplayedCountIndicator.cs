using System;
using System.IO;
using System.Globalization;
using ImageMagickSharp;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.IO;

namespace Emby.Drawing.ImageMagick
{
    public class UnplayedCountIndicator
    {
        private const int OffsetFromTopRightCorner = 38;

        private readonly IApplicationPaths _appPaths;
        private readonly IFileSystem _fileSystem;

        public UnplayedCountIndicator(IApplicationPaths appPaths, IFileSystem fileSystem)
        {
            _appPaths = appPaths;
            _fileSystem = fileSystem;
        }

        public void DrawUnplayedCountIndicator(MagickWand wand, ImageSize imageSize, int count)
        {
            var x = imageSize.Width - OffsetFromTopRightCorner;
            var text = count.ToString(CultureInfo.InvariantCulture);

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
                    draw.Font = extractFont("robotoregular.ttf", _appPaths, _fileSystem);
                    draw.FontStyle = FontStyleType.NormalStyle;
                    draw.TextAlignment = TextAlignType.CenterAlign;
                    draw.FontWeight = FontWeightType.RegularStyle;
                    draw.TextAntialias = true;

                    var fontSize = 30;
                    var y = OffsetFromTopRightCorner + 11;

                    if (text.Length == 1)
                    {
                        x += 1;
                    }
                    else if (text.Length == 2)
                    {
                        x += 1;
                    }
                    else if (text.Length >= 3)
                    {
                        //x += 1;
                        y -= 2;
                        fontSize = 24;
                    }

                    draw.FontSize = fontSize;
                    draw.DrawAnnotation(x, y, text);

                    draw.FillColor = pixel;
                    wand.CurrentImage.DrawImage(draw);
                }

            }
        }

        private static string extractFont(string name, IApplicationPaths paths, IFileSystem fileSystem)
        {
            var filePath = Path.Combine(paths.ProgramDataPath, "fonts", name);

            if (fileSystem.FileExists(filePath))
            {
                return filePath;
            }

            var namespacePath = typeof(PlayedIndicatorDrawer).Namespace + ".fonts." + name;
            var tempPath = Path.Combine(paths.TempDirectory, Guid.NewGuid().ToString("N") + ".ttf");
            fileSystem.CreateDirectory(fileSystem.GetDirectoryName(tempPath));

            using (var stream = typeof(PlayedIndicatorDrawer).Assembly.GetManifestResourceStream(namespacePath))
            {
                using (var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.Read))
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
    }
}
