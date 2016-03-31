using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommonIO;

namespace MediaBrowser.Providers.ImagesByName
{
    public static class ImageUtils
    {
        /// <summary>
        /// Ensures the list.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="file">The file.</param>
        /// <param name="httpClient">The HTTP client.</param>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="semaphore">The semaphore.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public static async Task EnsureList(string url, string file, IHttpClient httpClient, IFileSystem fileSystem, SemaphoreSlim semaphore, CancellationToken cancellationToken)
        {
            var fileInfo = fileSystem.GetFileInfo(file);

            if (!fileInfo.Exists || (DateTime.UtcNow - fileSystem.GetLastWriteTimeUtc(fileInfo)).TotalDays > 1)
            {
                await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

                try
                {
                    var temp = await httpClient.GetTempFile(new HttpRequestOptions
                    {
                        CancellationToken = cancellationToken,
                        Progress = new Progress<double>(),
                        Url = url

                    }).ConfigureAwait(false);

					fileSystem.CreateDirectory(Path.GetDirectoryName(file));

					fileSystem.CopyFile(temp, file, true);
                }
                finally
                {
                    semaphore.Release();
                }
            }
        }

        public static string FindMatch(IHasImages item, IEnumerable<string> images)
        {
            var name = GetComparableName(item.Name);

            return images.FirstOrDefault(i => string.Equals(name, GetComparableName(i), StringComparison.OrdinalIgnoreCase));
        }

        private static string GetComparableName(string name)
        {
            return name.Replace(" ", string.Empty)
                .Replace(".", string.Empty)
                .Replace("&", string.Empty)
                .Replace("!", string.Empty)
                .Replace(",", string.Empty)
                .Replace("/", string.Empty);
        }

        public static IEnumerable<string> GetAvailableImages(string file)
        {
            using (var reader = new StreamReader(file))
            {
                var lines = new List<string>();

                while (!reader.EndOfStream)
                {
                    var text = reader.ReadLine();

                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        lines.Add(text);
                    }
                }

                return lines;
            }
        }
    }
}
