using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using MediaBrowser.Api.Transcoding;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Controller;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Api.HttpHandlers
{
    public class AudioHandler : StaticFileHandler
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

        public override string Path
        {
            get
            {
                return TranscodedPath;
            }
        }

        private string _TranscodedPath;
        /// <summary>
        /// Gets the library item that will be played, if any
        /// </summary>
        private string TranscodedPath
        {
            get
            {
                if (_TranscodedPath == null)
                {
                    string originalMediaPath = LibraryItem == null ? base.Path : LibraryItem.Path;
                    
                    if (!RequiresTranscoding())
                    {
                        _TranscodedPath = originalMediaPath;
                    }
                    else
                    {
                        string outputPath = GetOutputFilePath(originalMediaPath);

                        // Find the job in the list
                        TranscodingJob job = ApiService.GetTranscodingJob(outputPath);

                        if (job == null && !File.Exists(outputPath))
                        {
                            job = GetNewTranscodingJob(originalMediaPath, outputPath);
                            job.Start();
                        }

                        if (job != null)
                        {
                            job.WaitForExit();
                        }

                        _TranscodedPath = outputPath;
                    }
                }

                return _TranscodedPath;
            }
        }

        public string AudioFormat
        {
            get
            {
                string val = QueryString["audiobitrate"];

                if (string.IsNullOrEmpty(val))
                {
                    return "mp3";
                }

                return val;
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

        public int? NumAudioChannels
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

        private static string _StreamsDirectory = null;
        /// <summary>
        /// Gets the folder path to where transcodes will be cached
        /// </summary>
        public static string StreamsDirectory
        {
            get
            {
                if (_StreamsDirectory == null)
                {
                    _StreamsDirectory = System.IO.Path.Combine(ApplicationPaths.ProgramDataPath, "streams");

                    if (!Directory.Exists(_StreamsDirectory))
                    {
                        Directory.CreateDirectory(_StreamsDirectory);
                    }
                }

                return _StreamsDirectory;
            }
        }

        private static string _FFMpegDirectory = null;
        /// <summary>
        /// Gets the folder path to ffmpeg
        /// </summary>
        public static string FFMpegDirectory
        {
            get
            {
                if (_FFMpegDirectory == null)
                {
                    _FFMpegDirectory = System.IO.Path.Combine(ApplicationPaths.ProgramDataPath, "ffmpeg");

                    if (!Directory.Exists(_FFMpegDirectory))
                    {
                        Directory.CreateDirectory(_FFMpegDirectory);

                        // Extract ffmpeg
                        using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("MediaBrowser.Api.ffmpeg.ffmpeg.exe"))
                        {
                            using (FileStream fileStream = new FileStream(FFMpegPath, FileMode.Create))
                            {
                                stream.CopyTo(fileStream);
                            }
                        }
                    }
                }

                return _FFMpegDirectory;
            }
        }

        private static string FFMpegPath
        {
            get
            {
                return System.IO.Path.Combine(FFMpegDirectory, "ffmpeg.exe");
            }
        }

        private string GetOutputFilePath(string input)
        {
            string hash = Kernel.GetMD5(input).ToString();

            if (AudioBitRate.HasValue)
            {
                hash += "_ab" + AudioBitRate;
            }
            if (NumAudioChannels.HasValue)
            {
                hash += "_ac" + NumAudioChannels;
            }
            if (AudioSampleRate.HasValue)
            {
                hash += "_ar" + AudioSampleRate;
            }

            string filename = hash + "." + AudioFormat.ToLower();

            return System.IO.Path.Combine(StreamsDirectory, filename);
        }

        /// <summary>
        /// Determines whether or not the original file requires transcoding
        /// </summary>
        private bool RequiresTranscoding()
        {
            // Only support skipping transcoding for library items
            if (LibraryItem == null)
            {
                return true;
            }

            // If it's not in the same format, we need to transcode
            if (!LibraryItem.Path.EndsWith(AudioFormat, StringComparison.OrdinalIgnoreCase))
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
            if (NumAudioChannels.HasValue)
            {
                if (NumAudioChannels.Value < LibraryItem.Channels)
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

        /// <summary>
        /// Creates a new transcoding job
        /// </summary>
        private TranscodingJob GetNewTranscodingJob(string input, string output)
        {
            return new TranscodingJob()
            {
                InputFile = input,
                OutputFile = output,
                TranscoderPath = FFMpegPath,
                Arguments = GetAudioArguments(input, output)
            };
        }

        /// <summary>
        /// Creates arguments to pass to ffmpeg
        /// </summary>
        private string GetAudioArguments(string input, string output)
        {
            List<string> audioTranscodeParams = new List<string>();

            if (AudioBitRate.HasValue)
            {
                audioTranscodeParams.Add("-ab " + AudioBitRate.Value);
            }

            if (NumAudioChannels.HasValue)
            {
                audioTranscodeParams.Add("-ac " + NumAudioChannels.Value);
            }

            if (AudioSampleRate.HasValue)
            {
                audioTranscodeParams.Add("-ar " + AudioSampleRate.Value);
            }

            audioTranscodeParams.Add("-f " + AudioFormat);

            return "-i \"" + input + "\" -vn " + string.Join(" ", audioTranscodeParams.ToArray()) + " \"" + output + "\"";
        }
    }
}
