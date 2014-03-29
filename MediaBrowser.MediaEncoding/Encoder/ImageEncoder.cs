using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.IO;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Logging;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.MediaEncoding.Encoder
{
    public class ImageEncoder
    {
        private readonly string _ffmpegPath;
        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;
        private readonly IApplicationPaths _appPaths;

        private readonly CultureInfo _usCulture = new CultureInfo("en-US");

        private static readonly SemaphoreSlim ResourcePool = new SemaphoreSlim(10, 10);

        public ImageEncoder(string ffmpegPath, ILogger logger, IFileSystem fileSystem, IApplicationPaths appPaths)
        {
            _ffmpegPath = ffmpegPath;
            _logger = logger;
            _fileSystem = fileSystem;
            _appPaths = appPaths;
        }

        public async Task<Stream> EncodeImage(ImageEncodingOptions options, CancellationToken cancellationToken)
        {
            ValidateInput(options);

            await ResourcePool.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                return await EncodeImageInternal(options, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                ResourcePool.Release();
            }
        }

        private async Task<Stream> EncodeImageInternal(ImageEncodingOptions options, CancellationToken cancellationToken)
        {
            ValidateInput(options);

            var inputPath = options.InputPath;
            var filename = Path.GetFileName(inputPath);

            if (HasDiacritics(filename))
            {
                inputPath = GetTempFile(inputPath);
                filename = Path.GetFileName(inputPath);
            }

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    FileName = _ffmpegPath,
                    Arguments = GetArguments(options, filename),
                    WindowStyle = ProcessWindowStyle.Hidden,
                    ErrorDialog = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WorkingDirectory = Path.GetDirectoryName(inputPath)
                }
            };

            _logger.Debug("ffmpeg " + process.StartInfo.Arguments);

            process.Start();

            var memoryStream = new MemoryStream();

#pragma warning disable 4014
            // Important - don't await the log task or we won't be able to kill ffmpeg when the user stops playback
            process.StandardOutput.BaseStream.CopyToAsync(memoryStream);
#pragma warning restore 4014

            // MUST read both stdout and stderr asynchronously or a deadlock may occurr
            process.BeginErrorReadLine();

            var ranToCompletion = process.WaitForExit(5000);

            if (!ranToCompletion)
            {
                try
                {
                    _logger.Info("Killing ffmpeg process");

                    process.Kill();

                    process.WaitForExit(1000);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error killing process", ex);
                }
            }

            var exitCode = ranToCompletion ? process.ExitCode : -1;

            process.Dispose();

            if (exitCode == -1 || memoryStream.Length == 0)
            {
                memoryStream.Dispose();

                var msg = string.Format("ffmpeg image encoding failed for {0}", options.InputPath);

                _logger.Error(msg);

                throw new ApplicationException(msg);
            }

            memoryStream.Position = 0;
            return memoryStream;
        }

        private string GetTempFile(string path)
        {
            var extension = Path.GetExtension(path) ?? string.Empty;

            var tempPath = Path.Combine(_appPaths.TempDirectory, Guid.NewGuid().ToString("N") + extension);

            File.Copy(path, tempPath);

            return tempPath;
        }
        
        private string GetArguments(ImageEncodingOptions options, string inputFilename)
        {
            var vfScale = GetFilterGraph(options);
            var outputFormat = GetOutputFormat(options.Format);

            var quality = (options.Quality ?? 100) * .3;
            quality = 31 - quality;
            var qualityValue = Convert.ToInt32(Math.Max(quality, 1));

            return string.Format("-f image2 -i file:\"{3}\" -q:v {0} {1} -f image2pipe -vcodec {2} -",
                qualityValue.ToString(_usCulture),
                vfScale,
                outputFormat,
                inputFilename);
        }

        private string GetFilterGraph(ImageEncodingOptions options)
        {
            if (!options.Width.HasValue &&
                !options.Height.HasValue &&
                !options.MaxHeight.HasValue &&
                !options.MaxWidth.HasValue)
            {
                return string.Empty;
            }

            var widthScale = "-1";
            var heightScale = "-1";

            if (options.MaxWidth.HasValue)
            {
                widthScale = "min(iw\\," + options.MaxWidth.Value.ToString(_usCulture) + ")";
            }
            else if (options.Width.HasValue)
            {
                widthScale = options.Width.Value.ToString(_usCulture);
            }

            if (options.MaxHeight.HasValue)
            {
                heightScale = "min(ih\\," + options.MaxHeight.Value.ToString(_usCulture) + ")";
            }
            else if (options.Height.HasValue)
            {
                heightScale = options.Height.Value.ToString(_usCulture);
            }

            var scaleMethod = "lanczos";

            return string.Format("-vf scale=\"{0}:{1}\"",
                widthScale,
                heightScale);
        }

        private string GetOutputFormat(string format)
        {
            if (string.Equals(format, "jpeg", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(format, "jpg", StringComparison.OrdinalIgnoreCase))
            {
                return "mjpeg";
            }
            return format;
        }

        private void ValidateInput(ImageEncodingOptions options)
        {

        }

        /// <summary>
        /// Determines whether the specified text has diacritics.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns><c>true</c> if the specified text has diacritics; otherwise, <c>false</c>.</returns>
        private bool HasDiacritics(string text)
        {
            return !String.Equals(text, RemoveDiacritics(text), StringComparison.Ordinal);
        }

        /// <summary>
        /// Removes the diacritics.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns>System.String.</returns>
        private string RemoveDiacritics(string text)
        {
            return String.Concat(
                text.Normalize(NormalizationForm.FormD)
                .Where(ch => CharUnicodeInfo.GetUnicodeCategory(ch) !=
                                              UnicodeCategory.NonSpacingMark)
              ).Normalize(NormalizationForm.FormC);
        }
    }
}
