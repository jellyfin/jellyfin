using MediaBrowser.Common.IO;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Photos
{
    public static class DynamicImageHelpers
    {
        public static async Task<Stream> GetThumbCollage(List<string> files,
            IFileSystem fileSystem,
            int width,
            int height)
        {
            if (files.Count < 3)
            {
                return await GetSingleImage(files, fileSystem).ConfigureAwait(false);
            }

            const int rows = 1;
            const int cols = 3;

            int cellWidth = 2 * (width / 3);
            int cellHeight = height;
            var index = 0;

            var img = new Bitmap(width, height, PixelFormat.Format32bppPArgb);

            using (var graphics = Graphics.FromImage(img))
            {
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.CompositingMode = CompositingMode.SourceCopy;

                for (var row = 0; row < rows; row++)
                {
                    for (var col = 0; col < cols; col++)
                    {
                        var x = col * (cellWidth / 2);
                        var y = row * cellHeight;

                        if (files.Count > index)
                        {
                            using (var fileStream = fileSystem.GetFileStream(files[index], FileMode.Open, FileAccess.Read, FileShare.Read, true))
                            {
                                using (var memoryStream = new MemoryStream())
                                {
                                    await fileStream.CopyToAsync(memoryStream).ConfigureAwait(false);

                                    memoryStream.Position = 0;

                                    using (var imgtemp = Image.FromStream(memoryStream, true, false))
                                    {
                                        graphics.DrawImage(imgtemp, x, y, cellWidth, cellHeight);
                                    }
                                }
                            }
                        }

                        index++;
                    }
                }
            }

            return GetStream(img);
        }

        public static async Task<Stream> GetSquareCollage(List<string> files,
            IFileSystem fileSystem,
            int size)
        {
            if (files.Count < 4)
            {
                return await GetSingleImage(files, fileSystem).ConfigureAwait(false);
            }

            const int rows = 2;
            const int cols = 2;

            int singleSize = size / 2;
            var index = 0;

            var img = new Bitmap(size, size, PixelFormat.Format32bppPArgb);

            using (var graphics = Graphics.FromImage(img))
            {
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.CompositingMode = CompositingMode.SourceCopy;

                for (var row = 0; row < rows; row++)
                {
                    for (var col = 0; col < cols; col++)
                    {
                        var x = col * singleSize;
                        var y = row * singleSize;

                        using (var fileStream = fileSystem.GetFileStream(files[index], FileMode.Open, FileAccess.Read, FileShare.Read, true))
                        {
                            using (var memoryStream = new MemoryStream())
                            {
                                await fileStream.CopyToAsync(memoryStream).ConfigureAwait(false);

                                memoryStream.Position = 0;

                                using (var imgtemp = Image.FromStream(memoryStream, true, false))
                                {
                                    graphics.DrawImage(imgtemp, x, y, singleSize, singleSize);
                                }
                            }
                        }

                        index++;
                    }
                }
            }

            return GetStream(img);
        }

        private static Task<Stream> GetSingleImage(List<string> files, IFileSystem fileSystem)
        {
            return Task.FromResult<Stream>(fileSystem.GetFileStream(files[0], FileMode.Open, FileAccess.Read, FileShare.Read));
        }

        private static Stream GetStream(Image image)
        {
            var ms = new MemoryStream();

            image.Save(ms, ImageFormat.Png);

            ms.Position = 0;

            return ms;
        }

        private static async Task<Image> GetImage(string file, IFileSystem fileSystem)
        {
            using (var fileStream = fileSystem.GetFileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, true))
            {
                var memoryStream = new MemoryStream();

                await fileStream.CopyToAsync(memoryStream).ConfigureAwait(false);

                memoryStream.Position = 0;

                return Image.FromStream(memoryStream, true, false);
            }
        }
    }
}
