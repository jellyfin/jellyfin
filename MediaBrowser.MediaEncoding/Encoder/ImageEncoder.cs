using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Logging;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.MediaEncoding.Encoder
{
    public class ImageEncoder
    {
        private readonly string _ffmpegPath;
        private readonly ILogger _logger;
        private readonly CultureInfo _usCulture = new CultureInfo("en-US");

        private static readonly SemaphoreSlim ResourcePool = new SemaphoreSlim(5, 5);

        public ImageEncoder(string ffmpegPath, ILogger logger)
        {
            _ffmpegPath = ffmpegPath;
            _logger = logger;
        }

        public async Task<Stream> EncodeImage(ImageEncodingOptions options, CancellationToken cancellationToken)
        {
            ValidateInput(options);

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    FileName = _ffmpegPath,
                    Arguments = GetArguments(options),
                    WindowStyle = ProcessWindowStyle.Hidden,
                    ErrorDialog = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            await ResourcePool.WaitAsync(cancellationToken).ConfigureAwait(false);

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

            ResourcePool.Release();

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

        private string GetArguments(ImageEncodingOptions options)
        {
            var vfScale = GetFilterGraph(options);
            var outputFormat = GetOutputFormat(options);

            return string.Format("-i file:\"{0}\" {1} -f {2}",
                options.InputPath,
                vfScale,
                outputFormat);
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
                widthScale = "min(iw," + options.MaxWidth.Value.ToString(_usCulture) + ")";
            }
            else if (options.Width.HasValue)
            {
                widthScale = options.Width.Value.ToString(_usCulture);
            }

            if (options.MaxHeight.HasValue)
            {
                heightScale = "min(ih," + options.MaxHeight.Value.ToString(_usCulture) + ")";
            }
            else if (options.Height.HasValue)
            {
                heightScale = options.Height.Value.ToString(_usCulture);
            }

            var scaleMethod = "lanczos";

            return string.Format("-vf scale=\"{0}:{1}\" -sws_flags {2}", 
                widthScale, 
                heightScale,
                scaleMethod);
        }

        private string GetOutputFormat(ImageEncodingOptions options)
        {
            return options.Format;
        }

        private void ValidateInput(ImageEncodingOptions options)
        {

        }
    }
}
