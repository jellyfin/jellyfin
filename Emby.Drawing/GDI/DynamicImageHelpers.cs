using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using CommonIO;

namespace Emby.Drawing.GDI
{
    public static class DynamicImageHelpers
    {
        public static void CreateThumbCollage(List<string> files,
            IFileSystem fileSystem,
            string file,
            int width,
            int height)
        {
            const int numStrips = 4;
            files = ImageHelpers.ProjectPaths(files, numStrips);

            const int rows = 1;
            int cols = numStrips;

            int cellWidth = 2 * (width / 3);
            int cellHeight = height;
            var index = 0;

            using (var img = new Bitmap(width, height, PixelFormat.Format32bppPArgb))
            {
                using (var graphics = Graphics.FromImage(img))
                {
                    graphics.CompositingQuality = CompositingQuality.HighQuality;
                    graphics.SmoothingMode = SmoothingMode.HighQuality;
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                    // SourceCopy causes the image to be blank in OSX
                    //graphics.CompositingMode = CompositingMode.SourceCopy;

                    for (var row = 0; row < rows; row++)
                    {
                        for (var col = 0; col < cols; col++)
                        {
                            var x = col * (cellWidth / 2);
                            var y = row * cellHeight;

                            if (files.Count > index)
                            {
                                using (var imgtemp = Image.FromFile(files[index]))
                                {
                                    graphics.DrawImage(imgtemp, x, y, cellWidth, cellHeight);
                                }
                            }

                            index++;
                        }
                    }
                    img.Save(file);
                }
            }
        }

        public static void CreateSquareCollage(List<string> files,
            IFileSystem fileSystem,
            string file,
            int width,
            int height)
        {
            files = ImageHelpers.ProjectPaths(files, 4);

            const int rows = 2;
            const int cols = 2;

            int singleSize = width / 2;
            var index = 0;

            using (var img = new Bitmap(width, height, PixelFormat.Format32bppPArgb))
            {
                using (var graphics = Graphics.FromImage(img))
                {
                    graphics.CompositingQuality = CompositingQuality.HighQuality;
                    graphics.SmoothingMode = SmoothingMode.HighQuality;
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                    // SourceCopy causes the image to be blank in OSX
                    //graphics.CompositingMode = CompositingMode.SourceCopy;

                    for (var row = 0; row < rows; row++)
                    {
                        for (var col = 0; col < cols; col++)
                        {
                            var x = col * singleSize;
                            var y = row * singleSize;

                            using (var imgtemp = Image.FromFile(files[index]))
                            {
                                graphics.DrawImage(imgtemp, x, y, singleSize, singleSize);
                            }
                            index++;
                        }
                    }
                    img.Save(file);
                }
            }
        }
    }
}
