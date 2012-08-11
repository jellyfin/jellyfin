using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using MediaBrowser.Common.Logging;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Controller;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Api.HttpHandlers
{
    public class AudioHandler : BaseHandler
    {
        private Audio _LibraryItem;
        /// <summary>
        /// Gets the library item that will be played, if any
        /// </summary>
        private Audio LibraryItem
        {
            get
            {
                if (_LibraryItem == null)
                {
                    string id = QueryString["id"];

                    if (!string.IsNullOrEmpty(id))
                    {
                        _LibraryItem = Kernel.Instance.GetItemById(Guid.Parse(id)) as Audio;
                    }
                }

                return _LibraryItem;
            }
        }

        public override bool CompressResponse
        {
            get
            {
                return false;
            }
        }

        protected override bool IsAsyncHandler
        {
            get
            {
                return true;
            }
        }

        public override string ContentType
        {
            get
            {
                return MimeTypes.GetMimeType("." + GetOutputFormat());
            }
        }

        public IEnumerable<string> AudioFormats
        {
            get
            {
                string val = QueryString["audioformat"];

                if (string.IsNullOrEmpty(val))
                {
                    return new string[] { "mp3" };
                }

                return val.Split(',');
            }
        }

        public int? AudioBitRate
        {
            get
            {
                string val = QueryString["audiobitrate"];

                if (string.IsNullOrEmpty(val))
                {
                    return null;
                }

                return int.Parse(val);
            }
        }

        public int? AudioChannels
        {
            get
            {
                string val = QueryString["audiochannels"];

                if (string.IsNullOrEmpty(val))
                {
                    return null;
                }

                return int.Parse(val);
            }
        }

        public int? AudioSampleRate
        {
            get
            {
                string val = QueryString["audiosamplerate"];

                if (string.IsNullOrEmpty(val))
                {
                    return 44100;
                }

                return int.Parse(val);
            }
        }

        public override void ProcessRequest(HttpListenerContext ctx)
        {
            HttpListenerContext = ctx;

            if (!RequiresTranscoding())
            {
                new StaticFileHandler() { Path = LibraryItem.Path }.ProcessRequest(ctx);
                return;
            }

            base.ProcessRequest(ctx);
        }

        /// <summary>
        /// Determines whether or not the original file requires transcoding
        /// </summary>
        private bool RequiresTranscoding()
        {
            // If it's not in a format the consumer accepts, return true
            if (!AudioFormats.Any(f => LibraryItem.Path.EndsWith(f, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            // If the bitrate is greater than our desired bitrate, we need to transcode
            if (AudioBitRate.HasValue)
            {
                if (AudioBitRate.Value < LibraryItem.BitRate)
                {
                    return true;
                }
            }

            // If the number of channels is greater than our desired channels, we need to transcode
            if (AudioChannels.HasValue)
            {
                if (AudioChannels.Value < LibraryItem.Channels)
                {
                    return true;
                }
            }

            // If the sample rate is greater than our desired sample rate, we need to transcode
            if (AudioSampleRate.HasValue)
            {
                if (AudioSampleRate.Value < LibraryItem.SampleRate)
                {
                    return true;
                }
            }

            // Yay
            return false;
        }

        private string GetOutputFormat()
        {
            string format = AudioFormats.FirstOrDefault(f => LibraryItem.Path.EndsWith(f, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(format))
            {
                return format;
            }

            return AudioFormats.First();
        }

        /// <summary>
        /// Creates arguments to pass to ffmpeg
        /// </summary>
        private string GetAudioArguments()
        {
            List<string> audioTranscodeParams = new List<string>();

            if (AudioBitRate.HasValue)
            {
                audioTranscodeParams.Add("-ab " + AudioBitRate.Value);
            }

            if (AudioChannels.HasValue)
            {
                audioTranscodeParams.Add("-ac " + AudioChannels.Value);
            }

            if (AudioSampleRate.HasValue)
            {
                audioTranscodeParams.Add("-ar " + AudioSampleRate.Value);
            }

            audioTranscodeParams.Add("-f " + GetOutputFormat());

            return "-i \"" + LibraryItem.Path + "\" -vn " + string.Join(" ", audioTranscodeParams.ToArray()) + " -";
        }

        protected async override void WriteResponseToOutputStream(Stream stream)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();

            startInfo.CreateNoWindow = true;

            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;

            startInfo.FileName = ApiService.FFMpegPath;
            startInfo.WorkingDirectory = ApiService.FFMpegDirectory;
            startInfo.Arguments = GetAudioArguments();

            Logger.LogInfo("Audio Handler Transcode: " + ApiService.FFMpegPath + " " + startInfo.Arguments);

            Process process = new Process();
            process.StartInfo = startInfo;

            try
            {
                process.Start();

                await process.StandardOutput.BaseStream.CopyToAsync(stream);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
            finally
            {
                DisposeResponseStream();

                process.Dispose();
            }
        }
    }
}
