using System;
using System.Collections.Generic;
using System.Linq;
using BDInfo;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.MediaInfo;

namespace MediaBrowser.MediaEncoding.BdInfo
{
    /// <summary>
    /// Class BdInfoExaminer
    /// </summary>
    public class BdInfoExaminer : IBlurayExaminer
    {
        private readonly IFileSystem _fileSystem;

        public BdInfoExaminer(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        /// <summary>
        /// Gets the disc info.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>BlurayDiscInfo.</returns>
        public BlurayDiscInfo GetDiscInfo(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            var bdrom = new BDROM(BdInfoDirectoryInfo.FromFileSystemPath(_fileSystem, path));

            bdrom.Scan();

            // Get the longest playlist
            var playlist = bdrom.PlaylistFiles.Values.OrderByDescending(p => p.TotalLength).FirstOrDefault(p => p.IsValid);

            var outputStream = new BlurayDiscInfo
            {
                MediaStreams = new MediaStream[] { }
            };

            if (playlist == null)
            {
                return outputStream;
            }

            outputStream.Chapters = playlist.Chapters.ToArray();

            outputStream.RunTimeTicks = TimeSpan.FromSeconds(playlist.TotalLength).Ticks;

            var mediaStreams = new List<MediaStream>();

            foreach (var stream in playlist.SortedStreams)
            {
                var videoStream = stream as TSVideoStream;

                if (videoStream != null)
                {
                    AddVideoStream(mediaStreams, videoStream);
                    continue;
                }

                var audioStream = stream as TSAudioStream;

                if (audioStream != null)
                {
                    AddAudioStream(mediaStreams, audioStream);
                    continue;
                }

                var textStream = stream as TSTextStream;

                if (textStream != null)
                {
                    AddSubtitleStream(mediaStreams, textStream);
                    continue;
                }

                var graphicsStream = stream as TSGraphicsStream;

                if (graphicsStream != null)
                {
                    AddSubtitleStream(mediaStreams, graphicsStream);
                }
            }

            outputStream.MediaStreams = mediaStreams.ToArray();

            outputStream.PlaylistName = playlist.Name;

            if (playlist.StreamClips != null && playlist.StreamClips.Any())
            {
                // Get the files in the playlist
                outputStream.Files = playlist.StreamClips.Select(i => i.StreamFile.Name).ToArray();
            }

            return outputStream;
        }

        /// <summary>
        /// Adds the video stream.
        /// </summary>
        /// <param name="streams">The streams.</param>
        /// <param name="videoStream">The video stream.</param>
        private void AddVideoStream(List<MediaStream> streams, TSVideoStream videoStream)
        {
            var mediaStream = new MediaStream
            {
                BitRate = Convert.ToInt32(videoStream.BitRate),
                Width = videoStream.Width,
                Height = videoStream.Height,
                Codec = videoStream.CodecShortName,
                IsInterlaced = videoStream.IsInterlaced,
                Type = MediaStreamType.Video,
                Index = streams.Count
            };

            if (videoStream.FrameRateDenominator > 0)
            {
                float frameRateEnumerator = videoStream.FrameRateEnumerator;
                float frameRateDenominator = videoStream.FrameRateDenominator;

                mediaStream.AverageFrameRate = mediaStream.RealFrameRate = frameRateEnumerator / frameRateDenominator;
            }

            streams.Add(mediaStream);
        }

        /// <summary>
        /// Adds the audio stream.
        /// </summary>
        /// <param name="streams">The streams.</param>
        /// <param name="audioStream">The audio stream.</param>
        private void AddAudioStream(List<MediaStream> streams, TSAudioStream audioStream)
        {
            var stream = new MediaStream
            {
                Codec = audioStream.CodecShortName,
                Language = audioStream.LanguageCode,
                Channels = audioStream.ChannelCount,
                SampleRate = audioStream.SampleRate,
                Type = MediaStreamType.Audio,
                Index = streams.Count
            };

            var bitrate = Convert.ToInt32(audioStream.BitRate);

            if (bitrate > 0)
            {
                stream.BitRate = bitrate;
            }

            if (audioStream.LFE > 0)
            {
                stream.Channels = audioStream.ChannelCount + 1;
            }

            streams.Add(stream);
        }

        /// <summary>
        /// Adds the subtitle stream.
        /// </summary>
        /// <param name="streams">The streams.</param>
        /// <param name="textStream">The text stream.</param>
        private void AddSubtitleStream(List<MediaStream> streams, TSTextStream textStream)
        {
            streams.Add(new MediaStream
            {
                Language = textStream.LanguageCode,
                Codec = textStream.CodecShortName,
                Type = MediaStreamType.Subtitle,
                Index = streams.Count
            });
        }

        /// <summary>
        /// Adds the subtitle stream.
        /// </summary>
        /// <param name="streams">The streams.</param>
        /// <param name="textStream">The text stream.</param>
        private void AddSubtitleStream(List<MediaStream> streams, TSGraphicsStream textStream)
        {
            streams.Add(new MediaStream
            {
                Language = textStream.LanguageCode,
                Codec = textStream.CodecShortName,
                Type = MediaStreamType.Subtitle,
                Index = streams.Count
            });
        }
    }
}
