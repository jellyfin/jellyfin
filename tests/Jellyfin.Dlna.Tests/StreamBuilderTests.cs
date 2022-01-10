using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Jellyfin.Extensions.Json;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Session;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.MediaBrowser.Model.Tests
{
    public class StreamBuilderTests
    {
        [Theory]
        // Chrome
        [InlineData("Chrome", "mp4-h264-aac-vtt-2600k", PlayMethod.DirectStream)] // #6450 should be DirectPlay
        [InlineData("Chrome", "mp4-h264-ac3-aac-srt-2600k", PlayMethod.Transcode)] // #6450 should be DirectPlay
        [InlineData("Chrome", "mp4-h264-ac3-srt-2600k", PlayMethod.Transcode)] // #6450 should be DirectStream
        [InlineData("Chrome", "mp4-hevc-aac-srt-15200k", PlayMethod.Transcode, true)]
        [InlineData("Chrome", "mp4-hevc-ac3-aac-srt-15200k", PlayMethod.Transcode, true)]
        [InlineData("Chrome", "mkv-vp9-aac-srt-2600k", PlayMethod.Transcode, true)] // #6450 should be 'false'
        [InlineData("Chrome", "mkv-vp9-ac3-srt-2600k", PlayMethod.Transcode, true)] // #6450 should be 'false'
        [InlineData("Chrome", "mkv-vp9-vorbis-vtt-2600k", PlayMethod.DirectStream)] // #6450 should be DirectPlay
        // Firefox
        [InlineData("Firefox", "mp4-h264-aac-vtt-2600k", PlayMethod.DirectStream)] // #6450 should be DirectPlay
        [InlineData("Firefox", "mp4-h264-ac3-aac-srt-2600k", PlayMethod.Transcode)] // #6450 should be DirectPlay
        [InlineData("Firefox", "mp4-h264-ac3-srt-2600k", PlayMethod.Transcode)] // #6450 should be DirectStream
        [InlineData("Firefox", "mp4-hevc-aac-srt-15200k", PlayMethod.Transcode, true)]
        [InlineData("Firefox", "mp4-hevc-ac3-aac-srt-15200k", PlayMethod.Transcode, true)]
        [InlineData("Firefox", "mkv-vp9-aac-srt-2600k", PlayMethod.Transcode, true)] // #6450 should be 'false'
        [InlineData("Firefox", "mkv-vp9-ac3-srt-2600k", PlayMethod.Transcode, true)] // #6450 should be 'false'
        [InlineData("Firefox", "mkv-vp9-vorbis-vtt-2600k", PlayMethod.DirectStream)] // #6450 should be DirectPlay
        // Safari
        [InlineData("SafariNext", "mp4-h264-aac-vtt-2600k", PlayMethod.DirectStream)] // #6450 should be DirectPlay
        [InlineData("SafariNext", "mp4-h264-ac3-aac-srt-2600k", PlayMethod.DirectStream)] // #6450 should be DirectPlay
        [InlineData("SafariNext", "mp4-h264-ac3-srt-2600k", PlayMethod.DirectStream)] // #6450 should be DirectPlay
        [InlineData("SafariNext", "mp4-hevc-aac-srt-15200k", PlayMethod.DirectStream)] // #6450 should probably be DirectPlay
        [InlineData("SafariNext", "mp4-hevc-ac3-aac-srt-15200k", PlayMethod.DirectStream)] // #6450 should probably be DirectPlay
        // AndroidPixel
        [InlineData("AndroidPixel", "mp4-h264-aac-srt-2600k", PlayMethod.DirectStream)] // #6450 should be DirectPlay
        [InlineData("AndroidPixel", "mp4-h264-ac3-aac-srt-2600k", PlayMethod.DirectStream)] // #6450 should be DirectPlay
        [InlineData("AndroidPixel", "mp4-h264-ac3-srt-2600k", PlayMethod.DirectStream)] // #6450 should be DirectPlay
        [InlineData("AndroidPixel", "mp4-hevc-aac-srt-15200k", PlayMethod.Transcode, true)]
        [InlineData("AndroidPixel", "mp4-hevc-ac3-aac-srt-15200k", PlayMethod.Transcode, true)]
        // Yatse
        [InlineData("Yatse", "mp4-h264-aac-srt-2600k", PlayMethod.DirectStream)] // #6450 should be DirectPlay
        [InlineData("Yatse", "mp4-h264-ac3-aac-srt-2600k", PlayMethod.Transcode)] // #6450 should be DirectPlay
        [InlineData("Yatse", "mp4-h264-ac3-srt-2600k", PlayMethod.Transcode, true)]
        [InlineData("Yatse", "mp4-hevc-aac-srt-15200k", PlayMethod.DirectStream)] // #6450 should be DirectPlay
        [InlineData("Yatse", "mp4-hevc-ac3-aac-srt-15200k", PlayMethod.Transcode, true)] // #6450 should be DirectPlay
        // RokuSSPlus
        [InlineData("RokuSSPlus", "mp4-h264-aac-srt-2600k", PlayMethod.DirectStream)] // #6450 should be DirectPlay
        [InlineData("RokuSSPlus", "mp4-h264-ac3-aac-srt-2600k", PlayMethod.Transcode)] // #6450 should be DirectPlay
        [InlineData("RokuSSPlus", "mp4-h264-ac3-srt-2600k", PlayMethod.Transcode)] // #6450 should be DirectStream
        [InlineData("RokuSSPlus", "mp4-hevc-aac-srt-15200k", PlayMethod.DirectStream)] // #6450 should be DirectPlay
        [InlineData("RokuSSPlus", "mp4-hevc-ac3-aac-srt-15200k", PlayMethod.Transcode, true)] // #6450 should be DirectPlay
        [InlineData("RokuSSPlus", "mp4-hevc-ac3-srt-15200k", PlayMethod.Transcode, true)] // #6450 should be DirectStream
        // JellyfinMediaPlayer
        [InlineData("JellyfinMediaPlayer", "mp4-h264-aac-vtt-2600k", PlayMethod.DirectStream)] // #6450 should be DirectPlay
        [InlineData("JellyfinMediaPlayer", "mp4-h264-ac3-aac-srt-2600k", PlayMethod.DirectStream)] // #6450 should be DirectPlay
        [InlineData("JellyfinMediaPlayer", "mp4-h264-ac3-srt-2600k", PlayMethod.DirectStream)] // #6450 should be DirectPlay
        [InlineData("JellyfinMediaPlayer", "mp4-hevc-aac-srt-15200k", PlayMethod.Transcode)] // #6450 should be DirectPlay
        [InlineData("JellyfinMediaPlayer", "mp4-hevc-ac3-aac-srt-15200k", PlayMethod.Transcode)] // #6450 should be DirectPlay
        [InlineData("JellyfinMediaPlayer", "mkv-vp9-aac-srt-2600k", PlayMethod.DirectStream)] // #6450 should be DirectPlay
        [InlineData("JellyfinMediaPlayer", "mkv-vp9-ac3-srt-2600k", PlayMethod.DirectStream)] // #6450 should be DirectPlay
        [InlineData("JellyfinMediaPlayer", "mkv-vp9-vorbis-vtt-2600k", PlayMethod.DirectStream)] // #6450 should be DirectPlay
        // TranscodeMedia
        [InlineData("TranscodeMedia", "mp4-h264-aac-vtt-2600k", PlayMethod.Transcode)] // #6450 should be DirectPlay
        [InlineData("TranscodeMedia", "mp4-h264-ac3-aac-srt-2600k", PlayMethod.Transcode)] // #6450 should be DirectPlay
        [InlineData("TranscodeMedia", "mp4-h264-ac3-srt-2600k", PlayMethod.Transcode)] // #6450 should be DirectPlay
        [InlineData("TranscodeMedia", "mp4-hevc-aac-srt-15200k", PlayMethod.Transcode, true)] // #6450 should be DirectPlay
        [InlineData("TranscodeMedia", "mp4-hevc-ac3-aac-srt-15200k", PlayMethod.Transcode, true)] // #6450 should be DirectPlay
        [InlineData("TranscodeMedia", "mkv-vp9-aac-srt-2600k", PlayMethod.Transcode, true)] // #6450 should be DirectPlay
        [InlineData("TranscodeMedia", "mkv-vp9-ac3-srt-2600k", PlayMethod.Transcode, true)] // #6450 should be DirectPlay
        [InlineData("TranscodeMedia", "mkv-vp9-vorbis-vtt-2600k", PlayMethod.Transcode, true)] // #6450 should be DirectPlay
        // DirectMedia
        [InlineData("DirectMedia", "mp4-h264-aac-vtt-2600k", PlayMethod.DirectStream)] // #6450 should be DirectPlay
        [InlineData("DirectMedia", "mp4-h264-ac3-aac-srt-2600k", PlayMethod.DirectStream)] // #6450 should be DirectPlay
        [InlineData("DirectMedia", "mp4-h264-ac3-srt-2600k", PlayMethod.DirectStream)] // #6450 should be DirectPlay
        [InlineData("DirectMedia", "mp4-hevc-aac-srt-15200k", PlayMethod.DirectStream)] // #6450 should be DirectPlay
        [InlineData("DirectMedia", "mp4-hevc-ac3-aac-srt-15200k", PlayMethod.DirectStream)] // #6450 should be DirectPlay
        [InlineData("DirectMedia", "mkv-vp9-aac-srt-2600k", PlayMethod.DirectStream)] // #6450 should be DirectPlay
        [InlineData("DirectMedia", "mkv-vp9-ac3-srt-2600k", PlayMethod.DirectStream)] // #6450 should be DirectPlay
        [InlineData("DirectMedia", "mkv-vp9-vorbis-vtt-2600k", PlayMethod.DirectStream)] // #6450 should be DirectPlay
        // LowBandwidth
        [InlineData("LowBandwidth", "mp4-h264-aac-vtt-2600k", PlayMethod.Transcode)] // #6450 should be DirectPlay
        [InlineData("LowBandwidth", "mp4-h264-ac3-aac-srt-2600k", PlayMethod.Transcode)] // #6450 should be DirectPlay
        [InlineData("LowBandwidth", "mp4-h264-ac3-srt-2600k", PlayMethod.Transcode)] // #6450 should be DirectPlay
        [InlineData("LowBandwidth", "mp4-hevc-aac-srt-15200k", PlayMethod.Transcode, true)] // #6450 should be DirectPlay
        [InlineData("LowBandwidth", "mp4-hevc-ac3-aac-srt-15200k", PlayMethod.Transcode, true)] // #6450 should be DirectPlay
        [InlineData("LowBandwidth", "mkv-vp9-aac-srt-2600k", PlayMethod.Transcode, true)] // #6450 should be DirectPlay
        [InlineData("LowBandwidth", "mkv-vp9-ac3-srt-2600k", PlayMethod.Transcode, true)] // #6450 should be DirectPlay
        [InlineData("LowBandwidth", "mkv-vp9-vorbis-vtt-2600k", PlayMethod.Transcode, true)] // #6450 should be DirectPlay
        // Null
        [InlineData("Null", "mp4-h264-aac-vtt-2600k", null)] // #6450 should be DirectPlay
        [InlineData("Null", "mp4-h264-ac3-aac-srt-2600k", null)] // #6450 should be DirectPlay
        [InlineData("Null", "mp4-h264-ac3-srt-2600k", null)] // #6450 should be DirectPlay
        [InlineData("Null", "mp4-hevc-aac-srt-15200k", null)] // #6450 should be DirectPlay
        [InlineData("Null", "mp4-hevc-ac3-aac-srt-15200k", null)] // #6450 should be DirectPlay
        [InlineData("Null", "mkv-vp9-aac-srt-2600k", null)] // #6450 should be DirectPlay
        [InlineData("Null", "mkv-vp9-ac3-srt-2600k", null)] // #6450 should be DirectPlay
        [InlineData("Null", "mkv-vp9-vorbis-vtt-2600k", null)] // #6450 should be DirectPlay
        public async Task BuildVideoItemSimple(string deviceName, string mediaSource, PlayMethod? playMethod, bool fullTranscode = false)
        {
            var builder = GetStreamBuilder();
            var options = await GetVideoOptions(deviceName, mediaSource);

            var val = builder.BuildVideoItem(options);
            Assert.NotNull(val);

            if (playMethod != null)
            {
                Assert.Equal(playMethod, val.PlayMethod);
            }

            var videoStreams = options.MediaSources.SelectMany(source => source.MediaStreams).Where(stream => stream.Type == MediaStreamType.Video);
            var audioStreams = options.MediaSources.SelectMany(source => source.MediaStreams).Where(stream => stream.Type == MediaStreamType.Audio);

            var url = new UriBuilder(val.ToUrl("https://server/", "ACCESSTOKEN"));
            var query = System.Web.HttpUtility.ParseQueryString(url.Query);

            if (playMethod == PlayMethod.DirectPlay)
            {
                // Assert.Contains(query.Get("VidoeCodec"), videoStreams.Select(stream => stream.Codec));
                // Assert.Contains(query.Get("AudioCodec"), audioStreams.Select(stream => stream.Codec));
                Assert.Contains(
                    videoStreams,
                    stream => val.TargetVideoCodec.Contains(stream.Codec));
                Assert.Contains(
                    audioStreams,
                    stream => val.TargetAudioCodec.Contains(stream.Codec));
            }

            if (playMethod == PlayMethod.DirectStream)
            {
                Assert.Matches("stream[.][^.]+$", url.Path);
            }

            if (playMethod == PlayMethod.Transcode)
            {
                if (fullTranscode)
                {
                    Assert.Equal("hls", val.SubProtocol);
                    Assert.EndsWith("master.m3u8", url.Path, StringComparison.InvariantCulture);

                    // Assert.All(
                    //     videoStreams,
                    //     stream => Assert.DoesNotContain(stream.Codec, val.TargetVideoCodec));
                }
                else
                {
                    Assert.Equal("hls", val.SubProtocol);
                    Assert.EndsWith("master.m3u8", url.Path, StringComparison.InvariantCulture);

                    Assert.Contains(
                        videoStreams,
                        stream => val.TargetVideoCodec.Contains(stream.Codec));
                    // Assert.All(
                    //     audioStreams,
                    //     stream => Assert.DoesNotContain(stream.Codec, val.TargetAudioCodec));

                    Assert.False(val.EstimateContentLength);
                    Assert.Equal(TranscodeSeekInfo.Auto, val.TranscodeSeekInfo);
                    // Assert.True(val.CopyTimestamps);

                    var videoStream = videoStreams.First(stream => val.TargetVideoCodec.Contains(stream.Codec));

                    Assert.Contains(videoStream.Codec, val.TargetVideoCodec);
                    // Assert.Contains(videoStream.Profile.ToLowerInvariant(), val.TargetVideoProfile.Split(","));
                    // Assert.Equal(videoStream.Level, val.TargetVideoLevel);
                    // Assert.Equal(videoStream.BitDepth, val.TargetVideoBitDepth);
                    // Assert.Equal(videoStream.BitRate, val.VideoBitrate);
                }
            }

            if (playMethod == null)
            {
                // what should the actual result be here?
                Assert.Null(val.SubProtocol);
                Assert.EndsWith("/stream", url.Path, StringComparison.InvariantCulture);

                Assert.False(val.EstimateContentLength);
                Assert.Equal(TranscodeSeekInfo.Auto, val.TranscodeSeekInfo);
                // Assert.True(val.CopyTimestamps);
            }
        }

        private static async ValueTask<T> TestData<T>(string name)
        {
            var path = Path.Join("Test Data", typeof(T).Name + "-" + name + ".json");
            using (var stream = File.OpenRead(path))
            {
                var value = await JsonSerializer.DeserializeAsync<T>(stream, JsonDefaults.Options);
                if (value != null)
                {
                    return value;
                }

                throw new Exception("Invalid test data: " + name);
            }
        }

        private StreamBuilder GetStreamBuilder()
        {
            var transcodeSupport = new Mock<ITranscoderSupport>();
            var logger = new NullLogger<StreamBuilderTests>();

            return new StreamBuilder(transcodeSupport.Object, logger);
        }

        private static async ValueTask<VideoOptions> GetVideoOptions(string deviceProfile, params string[] sources)
        {
            var mediaSources = sources.Select(src => TestData<MediaSourceInfo>(src))
                .Select(val => val.Result)
                .ToArray();
            var mediaSourceId = mediaSources[0]?.Id;

            var dp = await TestData<DeviceProfile>(deviceProfile);

            return new VideoOptions()
            {
                ItemId = new Guid("11D229B7-2D48-4B95-9F9B-49F6AB75E613"),
                MediaSourceId = mediaSourceId,
                MediaSources = mediaSources,
                DeviceId = "test-deviceId",
                Profile = dp,
            };
        }
    }
}
