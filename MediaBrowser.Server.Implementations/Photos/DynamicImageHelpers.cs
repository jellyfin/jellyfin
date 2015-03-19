using ImageMagickSharp;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Photos
{
    public static class DynamicImageHelpers
    {
        public static async Task<Stream> GetThumbCollage(List<string> files,
            IFileSystem fileSystem,
            int width,
            int height, IApplicationPaths appPaths)
        {
            if (files.Any(string.IsNullOrWhiteSpace))
            {
                throw new ArgumentException("Empty file found in files list");
            }

            if (files.Count == 0)
            {
                return null;
            }

            if (files.Count < 3)
            {
                return await GetSingleImage(files, fileSystem).ConfigureAwait(false);
            }

            const int rows = 1;
            const int cols = 3;

            int cellWidth = 2 * (width / 3);
            int cellHeight = height;
            var index = 0;

            using (var wand = new MagickWand(width, height, new PixelWand(ColorName.None, 1)))
            {
                for (var row = 0; row < rows; row++)
                {
                    for (var col = 0; col < cols; col++)
                    {
                        var x = col * (cellWidth / 2);
                        var y = row * cellHeight;

                        if (files.Count > index)
                        {
                            using (var innerWand = new MagickWand(files[index]))
                            {
                                innerWand.CurrentImage.ResizeImage(cellWidth, cellHeight);
                                wand.CurrentImage.CompositeImage(innerWand, CompositeOperator.OverCompositeOp, x, y);
                            }
                        }

                        index++;
                    }
                }

                return GetStream(wand, appPaths);
            }
        }

        public static async Task<Stream> GetSquareCollage(List<string> files,
            IFileSystem fileSystem,
            int size, IApplicationPaths appPaths)
        {
            if (files.Any(string.IsNullOrWhiteSpace))
            {
                throw new ArgumentException("Empty file found in files list");
            }

            if (files.Count == 0)
            {
                return null;
            }

            if (files.Count < 4)
            {
                return await GetSingleImage(files, fileSystem).ConfigureAwait(false);
            }

            const int rows = 2;
            const int cols = 2;

            int singleSize = size / 2;
            var index = 0;

            using (var wand = new MagickWand(size, size, new PixelWand(ColorName.None, 1)))
            {
                for (var row = 0; row < rows; row++)
                {
                    for (var col = 0; col < cols; col++)
                    {
                        var x = col * singleSize;
                        var y = row * singleSize;

                        using (var innerWand = new MagickWand(files[index]))
                        {
                            innerWand.CurrentImage.ResizeImage(singleSize, singleSize);
                            wand.CurrentImage.CompositeImage(innerWand, CompositeOperator.OverCompositeOp, x, y);
                        }

                        index++;
                    }
                }

                return GetStream(wand, appPaths);
            }
        }

        private static Task<Stream> GetSingleImage(List<string> files, IFileSystem fileSystem)
        {
            return Task.FromResult<Stream>(fileSystem.GetFileStream(files[0], FileMode.Open, FileAccess.Read, FileShare.Read));
        }

        internal static Stream GetStream(MagickWand image, IApplicationPaths appPaths)
        {
            var tempFile = Path.Combine(appPaths.TempDirectory, Guid.NewGuid().ToString("N") + ".png");

            Directory.CreateDirectory(Path.GetDirectoryName(tempFile));

            image.CurrentImage.CompressionQuality = 100;
            image.SaveImage(tempFile);

            return File.OpenRead(tempFile);
        }
    }
}
