using BDInfo;
using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.MediaInfo;
using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace MediaBrowser.Controller.Providers.MediaInfo
{
    /// <summary>
    /// Extracts dvd information using VgtMpeg
    /// </summary>
    internal static class BDInfoProvider
    {
        internal static void FetchBdInfo(BaseItem item, string inputPath, FileSystemRepository bdInfoCache, CancellationToken cancellationToken)
        {
            var video = (Video)item;

            // Get the path to the cache file
            var cacheName = item.Id + "_" + item.DateModified.Ticks;

            var cacheFile = bdInfoCache.GetResourcePath(cacheName, ".pb");

            BDInfoResult result;

            try
            {
                result = Kernel.Instance.ProtobufSerializer.DeserializeFromFile<BDInfoResult>(cacheFile);
            }
            catch (FileNotFoundException)
            {
                result = GetBDInfo(inputPath);

                Kernel.Instance.ProtobufSerializer.SerializeToFile(result, cacheFile);
            }

            cancellationToken.ThrowIfCancellationRequested();

            int? currentHeight = null;
            int? currentWidth = null;
            int? currentBitRate = null;

            var videoStream = video.MediaStreams.FirstOrDefault(s => s.Type == MediaStreamType.Video);

            // Grab the values that ffprobe recorded
            if (videoStream != null)
            {
                currentBitRate = videoStream.BitRate;
                currentWidth = videoStream.Width;
                currentHeight = videoStream.Height;
            }

            // Fill video properties from the BDInfo result
            Fetch(video, inputPath, result);

            videoStream = video.MediaStreams.FirstOrDefault(s => s.Type == MediaStreamType.Video);

            // Use the ffprobe values if these are empty
            if (videoStream != null)
            {
                videoStream.BitRate = IsEmpty(videoStream.BitRate) ? currentBitRate : videoStream.BitRate;
                videoStream.Width = IsEmpty(videoStream.Width) ? currentWidth : videoStream.Width;
                videoStream.Height = IsEmpty(videoStream.Height) ? currentHeight : videoStream.Height;
            }
        }

        /// <summary>
        /// Determines whether the specified num is empty.
        /// </summary>
        /// <param name="num">The num.</param>
        /// <returns><c>true</c> if the specified num is empty; otherwise, <c>false</c>.</returns>
        private static bool IsEmpty(int? num)
        {
            return !num.HasValue || num.Value == 0;
        }

        /// <summary>
        /// Fills video properties from the VideoStream of the largest playlist
        /// </summary>
        /// <param name="video">The video.</param>
        /// <param name="inputPath">The input path.</param>
        /// <param name="stream">The stream.</param>
        private static void Fetch(Video video, string inputPath, BDInfoResult stream)
        {
            // Check all input for null/empty/zero

            video.MediaStreams = stream.MediaStreams;

            if (stream.RunTimeTicks.HasValue && stream.RunTimeTicks.Value > 0)
            {
                video.RunTimeTicks = stream.RunTimeTicks;
            }

            video.PlayableStreamFileNames = stream.Files.ToList();

            if (stream.Chapters != null)
            {
                video.Chapters = stream.Chapters.Select(c => new ChapterInfo
                {
                    StartPositionTicks = TimeSpan.FromSeconds(c).Ticks

                }).ToList();
            }
        }

        /// <summary>
        /// Gets information about the longest playlist on a bdrom
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>VideoStream.</returns>
        private static BDInfoResult GetBDInfo(string path)
        {
            var bdrom = new BDROM(path);

            bdrom.Scan();

            // Get the longest playlist
            var playlist = bdrom.PlaylistFiles.Values.OrderByDescending(p => p.TotalLength).FirstOrDefault(p => p.IsValid);

            var outputStream = new BDInfoResult
            {
                MediaStreams = new List<MediaStream>()
            };

            if (playlist == null)
            {
                return outputStream;
            }

            outputStream.Chapters = playlist.Chapters;

            outputStream.RunTimeTicks = TimeSpan.FromSeconds(playlist.TotalLength).Ticks;

            var mediaStreams = new List<MediaStream> {};

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

            outputStream.MediaStreams = mediaStreams;

            if (playlist.StreamClips != null && playlist.StreamClips.Any())
            {
                // Get the files in the playlist
                outputStream.Files = playlist.StreamClips.Select(i => i.StreamFile.Name).ToList();
            }

            return outputStream;
        }

        /// <summary>
        /// Adds the video stream.
        /// </summary>
        /// <param name="streams">The streams.</param>
        /// <param name="videoStream">The video stream.</param>
        private static void AddVideoStream(List<MediaStream> streams, TSVideoStream videoStream)
        {
            var mediaStream = new MediaStream
            {
                BitRate = Convert.ToInt32(videoStream.BitRate),
                Width = videoStream.Width,
                Height = videoStream.Height,
                Codec = videoStream.CodecShortName,
                ScanType = videoStream.IsInterlaced ? "interlaced" : "progressive",
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
        private static void AddAudioStream(List<MediaStream> streams, TSAudioStream audioStream)
        {
            streams.Add(new MediaStream
            {
                BitRate = Convert.ToInt32(audioStream.BitRate),
                Codec = audioStream.CodecShortName,
                Language = audioStream.LanguageCode,
                Channels = audioStream.ChannelCount,
                SampleRate = audioStream.SampleRate,
                Type = MediaStreamType.Audio,
                Index = streams.Count
            });
        }

        /// <summary>
        /// Adds the subtitle stream.
        /// </summary>
        /// <param name="streams">The streams.</param>
        /// <param name="textStream">The text stream.</param>
        private static void AddSubtitleStream(List<MediaStream> streams, TSTextStream textStream)
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
        private static void AddSubtitleStream(List<MediaStream> streams, TSGraphicsStream textStream)
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
