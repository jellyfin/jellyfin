using System;
using System.Collections.Specialized;
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
        [InlineData("Chrome", "mp4-h264-ac3-aac-srt-2600k", PlayMethod.Transcode, TranscodeReason.AudioCodecNotSupported)] // #6450 should be DirectPlay
        [InlineData("Chrome", "mp4-h264-ac3-aacDef-srt-2600k", PlayMethod.Transcode, TranscodeReason.SecondaryAudioNotSupported)] // #6450 should be DirectPlay
        [InlineData("Chrome", "mp4-h264-ac3-srt-2600k", PlayMethod.Transcode, TranscodeReason.AudioCodecNotSupported)] // #6450 should be DirectStream
        [InlineData("Chrome", "mp4-hevc-aac-srt-15200k", PlayMethod.Transcode, TranscodeReason.VideoCodecNotSupported, "Transcode")]
        [InlineData("Chrome", "mp4-hevc-ac3-aac-srt-15200k", PlayMethod.Transcode, TranscodeReason.VideoCodecNotSupported | TranscodeReason.AudioCodecNotSupported, "Transcode")]
        [InlineData("Chrome", "mkv-vp9-aac-srt-2600k", PlayMethod.Transcode, TranscodeReason.AudioCodecNotSupported, "Transcode")] // #6450 should be 'false'
        [InlineData("Chrome", "mkv-vp9-ac3-srt-2600k", PlayMethod.Transcode, TranscodeReason.AudioCodecNotSupported, "Transcode")] // #6450 should be 'false'
        [InlineData("Chrome", "mkv-vp9-vorbis-vtt-2600k", PlayMethod.DirectStream)] // #6450 should be DirectPlay
        // Firefox
        [InlineData("Firefox", "mp4-h264-aac-vtt-2600k", PlayMethod.DirectStream)] // #6450 should be DirectPlay
        [InlineData("Firefox", "mp4-h264-ac3-aac-srt-2600k", PlayMethod.Transcode, TranscodeReason.AudioCodecNotSupported)] // #6450 should be DirectPlay
        [InlineData("Firefox", "mp4-h264-ac3-aacDef-srt-2600k", PlayMethod.Transcode, TranscodeReason.SecondaryAudioNotSupported)] // #6450 should be DirectPlay
        [InlineData("Firefox", "mp4-h264-ac3-srt-2600k", PlayMethod.Transcode, TranscodeReason.AudioCodecNotSupported)] // #6450 should be DirectStream
        [InlineData("Firefox", "mp4-hevc-aac-srt-15200k", PlayMethod.Transcode, TranscodeReason.VideoCodecNotSupported, "Transcode")]
        [InlineData("Firefox", "mp4-hevc-ac3-aac-srt-15200k", PlayMethod.Transcode, TranscodeReason.VideoCodecNotSupported | TranscodeReason.AudioCodecNotSupported, "Transcode")]
        [InlineData("Firefox", "mkv-vp9-aac-srt-2600k", PlayMethod.Transcode, TranscodeReason.AudioCodecNotSupported, "Transcode")] // #6450 should be 'false'
        [InlineData("Firefox", "mkv-vp9-ac3-srt-2600k", PlayMethod.Transcode, TranscodeReason.AudioCodecNotSupported, "Transcode")] // #6450 should be 'false'
        [InlineData("Firefox", "mkv-vp9-vorbis-vtt-2600k", PlayMethod.DirectStream)] // #6450 should be DirectPlay
        // Safari
        [InlineData("SafariNext", "mp4-h264-aac-vtt-2600k", PlayMethod.DirectStream)] // #6450 should be DirectPlay
        [InlineData("SafariNext", "mp4-h264-ac3-aacDef-srt-2600k", PlayMethod.DirectStream)] // #6450 should be DirectPlay
        [InlineData("SafariNext", "mp4-h264-ac3-srt-2600k", PlayMethod.DirectStream)] // #6450 should be DirectPlay
        [InlineData("SafariNext", "mp4-hevc-aac-srt-15200k", PlayMethod.DirectStream)] // #6450 should probably be DirectPlay
        [InlineData("SafariNext", "mp4-hevc-ac3-aac-srt-15200k", PlayMethod.DirectStream)] // #6450 should probably be DirectPlay
        // AndroidPixel
        [InlineData("AndroidPixel", "mp4-h264-aac-srt-2600k", PlayMethod.DirectStream)] // #6450 should be DirectPlay
        [InlineData("AndroidPixel", "mp4-h264-ac3-aacDef-srt-2600k", PlayMethod.DirectStream)] // #6450 should be DirectPlay
        [InlineData("AndroidPixel", "mp4-h264-ac3-srt-2600k", PlayMethod.DirectStream)] // #6450 should be DirectPlay
        [InlineData("AndroidPixel", "mp4-hevc-aac-srt-15200k", PlayMethod.Transcode, TranscodeReason.ContainerBitrateExceedsLimit, "Transcode")]
        [InlineData("AndroidPixel", "mp4-hevc-ac3-aac-srt-15200k", PlayMethod.Transcode, TranscodeReason.ContainerBitrateExceedsLimit, "Transcode")]
        // Yatse
        [InlineData("Yatse", "mp4-h264-aac-srt-2600k", PlayMethod.DirectStream)] // #6450 should be DirectPlay
        [InlineData("Yatse", "mp4-h264-ac3-aac-srt-2600k", PlayMethod.Transcode, TranscodeReason.AudioCodecNotSupported)] // #6450 should be DirectPlay
        [InlineData("Yatse", "mp4-h264-ac3-aacDef-srt-2600k", PlayMethod.Transcode, TranscodeReason.SecondaryAudioNotSupported)] // #6450 should be DirectPlay
        [InlineData("Yatse", "mp4-h264-ac3-srt-2600k", PlayMethod.Transcode, TranscodeReason.AudioCodecNotSupported, "Transcode")]
        [InlineData("Yatse", "mp4-hevc-aac-srt-15200k", PlayMethod.DirectStream)] // #6450 should be DirectPlay
        [InlineData("Yatse", "mp4-hevc-ac3-aac-srt-15200k", PlayMethod.Transcode, TranscodeReason.AudioCodecNotSupported, "Transcode")] // #6450 should be DirectPlay
        // RokuSSPlus
        [InlineData("RokuSSPlus", "mp4-h264-aac-srt-2600k", PlayMethod.DirectStream)] // #6450 should be DirectPlay
        [InlineData("RokuSSPlus", "mp4-h264-ac3-aac-srt-2600k", PlayMethod.Transcode, TranscodeReason.AudioCodecNotSupported)] // #6450 should be DirectPlay
        [InlineData("RokuSSPlus", "mp4-h264-ac3-aacDef-srt-2600k", PlayMethod.DirectStream)] // #6450 should be DirectPlay
        [InlineData("RokuSSPlus", "mp4-h264-ac3-srt-2600k", PlayMethod.Transcode, TranscodeReason.AudioCodecNotSupported)] // #6450 should be DirectStream
        [InlineData("RokuSSPlus", "mp4-hevc-aac-srt-15200k", PlayMethod.DirectStream)] // #6450 should be DirectPlay
        [InlineData("RokuSSPlus", "mp4-hevc-ac3-aac-srt-15200k", PlayMethod.Transcode, TranscodeReason.AudioCodecNotSupported, "Transcode")] // #6450 should be DirectPlay
        [InlineData("RokuSSPlus", "mp4-hevc-ac3-srt-15200k", PlayMethod.Transcode, TranscodeReason.AudioCodecNotSupported, "Transcode")] // #6450 should be DirectStream
        // JellyfinMediaPlayer
        [InlineData("JellyfinMediaPlayer", "mp4-h264-aac-vtt-2600k", PlayMethod.DirectStream)] // #6450 should be DirectPlay
        [InlineData("JellyfinMediaPlayer", "mp4-h264-ac3-aac-srt-2600k", PlayMethod.DirectStream)] // #6450 should be DirectPlay
        [InlineData("JellyfinMediaPlayer", "mp4-h264-ac3-srt-2600k", PlayMethod.DirectStream)] // #6450 should be DirectPlay
        [InlineData("JellyfinMediaPlayer", "mp4-hevc-aac-srt-15200k", PlayMethod.Transcode, TranscodeReason.ContainerBitrateExceedsLimit)] // #6450 should be DirectPlay
        [InlineData("JellyfinMediaPlayer", "mp4-hevc-ac3-aac-srt-15200k", PlayMethod.Transcode, TranscodeReason.ContainerBitrateExceedsLimit)] // #6450 should be DirectPlay
        [InlineData("JellyfinMediaPlayer", "mkv-vp9-aac-srt-2600k", PlayMethod.DirectStream)] // #6450 should be DirectPlay
        [InlineData("JellyfinMediaPlayer", "mkv-vp9-ac3-srt-2600k", PlayMethod.DirectStream)] // #6450 should be DirectPlay
        [InlineData("JellyfinMediaPlayer", "mkv-vp9-vorbis-vtt-2600k", PlayMethod.DirectStream)] // #6450 should be DirectPlay
        // TranscodeMedia
        [InlineData("TranscodeMedia", "mp4-h264-aac-vtt-2600k", PlayMethod.Transcode, TranscodeReason.ContainerNotSupported | TranscodeReason.VideoCodecNotSupported | TranscodeReason.AudioCodecNotSupported)] // #6450 should be DirectPlay
        [InlineData("TranscodeMedia", "mp4-h264-ac3-aac-srt-2600k", PlayMethod.Transcode, TranscodeReason.ContainerNotSupported | TranscodeReason.VideoCodecNotSupported | TranscodeReason.AudioCodecNotSupported)] // #6450 should be DirectPlay
        [InlineData("TranscodeMedia", "mp4-h264-ac3-srt-2600k", PlayMethod.Transcode, TranscodeReason.ContainerNotSupported | TranscodeReason.VideoCodecNotSupported | TranscodeReason.AudioCodecNotSupported)] // #6450 should be DirectPlay
        [InlineData("TranscodeMedia", "mp4-hevc-aac-srt-15200k", PlayMethod.Transcode, TranscodeReason.ContainerNotSupported | TranscodeReason.VideoCodecNotSupported | TranscodeReason.AudioCodecNotSupported, "Transcode")] // #6450 should be DirectPlay
        [InlineData("TranscodeMedia", "mp4-hevc-ac3-aac-srt-15200k", PlayMethod.Transcode, TranscodeReason.ContainerNotSupported | TranscodeReason.VideoCodecNotSupported | TranscodeReason.AudioCodecNotSupported, "Transcode")] // #6450 should be DirectPlay
        [InlineData("TranscodeMedia", "mkv-vp9-aac-srt-2600k", PlayMethod.Transcode, TranscodeReason.ContainerNotSupported | TranscodeReason.VideoCodecNotSupported | TranscodeReason.AudioCodecNotSupported, "Transcode")] // #6450 should be DirectPlay
        [InlineData("TranscodeMedia", "mkv-vp9-ac3-srt-2600k", PlayMethod.Transcode, TranscodeReason.ContainerNotSupported | TranscodeReason.VideoCodecNotSupported | TranscodeReason.AudioCodecNotSupported, "Transcode")] // #6450 should be DirectPlay
        [InlineData("TranscodeMedia", "mkv-vp9-vorbis-vtt-2600k", PlayMethod.Transcode, TranscodeReason.ContainerNotSupported | TranscodeReason.VideoCodecNotSupported | TranscodeReason.AudioCodecNotSupported, "Transcode")] // #6450 should be DirectPlay
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
        [InlineData("LowBandwidth", "mp4-h264-aac-vtt-2600k", PlayMethod.Transcode, TranscodeReason.ContainerBitrateExceedsLimit)] // #6450 should be DirectPlay
        [InlineData("LowBandwidth", "mp4-h264-ac3-aac-srt-2600k", PlayMethod.Transcode, TranscodeReason.ContainerBitrateExceedsLimit)] // #6450 should be DirectPlay
        [InlineData("LowBandwidth", "mp4-h264-ac3-srt-2600k", PlayMethod.Transcode, TranscodeReason.ContainerBitrateExceedsLimit)] // #6450 should be DirectPlay
        [InlineData("LowBandwidth", "mp4-hevc-aac-srt-15200k", PlayMethod.Transcode, TranscodeReason.ContainerBitrateExceedsLimit, "Transcode")] // #6450 should be DirectPlay
        [InlineData("LowBandwidth", "mp4-hevc-ac3-aac-srt-15200k", PlayMethod.Transcode, TranscodeReason.ContainerBitrateExceedsLimit, "Transcode")] // #6450 should be DirectPlay
        [InlineData("LowBandwidth", "mkv-vp9-aac-srt-2600k", PlayMethod.Transcode, TranscodeReason.ContainerBitrateExceedsLimit, "Transcode")] // #6450 should be DirectPlay
        [InlineData("LowBandwidth", "mkv-vp9-ac3-srt-2600k", PlayMethod.Transcode, TranscodeReason.ContainerBitrateExceedsLimit, "Transcode")] // #6450 should be DirectPlay
        [InlineData("LowBandwidth", "mkv-vp9-vorbis-vtt-2600k", PlayMethod.Transcode, TranscodeReason.ContainerBitrateExceedsLimit, "Transcode")] // #6450 should be DirectPlay
        // Null
        [InlineData("Null", "mp4-h264-aac-vtt-2600k", null, TranscodeReason.ContainerBitrateExceedsLimit | TranscodeReason.SubtitleCodecNotSupported)] // #6450 should be DirectPlay
        [InlineData("Null", "mp4-h264-ac3-aac-srt-2600k", null, TranscodeReason.ContainerBitrateExceedsLimit | TranscodeReason.SubtitleCodecNotSupported)] // #6450 should be DirectPlay
        [InlineData("Null", "mp4-h264-ac3-srt-2600k", null, TranscodeReason.ContainerBitrateExceedsLimit | TranscodeReason.SubtitleCodecNotSupported)] // #6450 should be DirectPlay
        [InlineData("Null", "mp4-hevc-aac-srt-15200k", null, TranscodeReason.ContainerBitrateExceedsLimit | TranscodeReason.SubtitleCodecNotSupported)] // #6450 should be DirectPlay
        [InlineData("Null", "mp4-hevc-ac3-aac-srt-15200k", null, TranscodeReason.ContainerBitrateExceedsLimit | TranscodeReason.SubtitleCodecNotSupported)] // #6450 should be DirectPlay
        [InlineData("Null", "mkv-vp9-aac-srt-2600k", null, TranscodeReason.ContainerBitrateExceedsLimit | TranscodeReason.SubtitleCodecNotSupported)] // #6450 should be DirectPlay
        [InlineData("Null", "mkv-vp9-ac3-srt-2600k", null, TranscodeReason.ContainerBitrateExceedsLimit | TranscodeReason.SubtitleCodecNotSupported)] // #6450 should be DirectPlay
        [InlineData("Null", "mkv-vp9-vorbis-vtt-2600k", null, TranscodeReason.ContainerBitrateExceedsLimit | TranscodeReason.SubtitleCodecNotSupported)] // #6450 should be DirectPlay
        public async Task BuildVideoItemSimple(string deviceName, string mediaSource, PlayMethod? playMethod, TranscodeReason why = TranscodeReason.None, string transcodeMode = "DirectStream", string transcodeProtocol = "")
        {
            var options = await GetVideoOptions(deviceName, mediaSource);
            BuildVideoItemSimpleTest(options, playMethod, why, transcodeMode, transcodeProtocol);
        }

        [Theory]
        // Chrome
        [InlineData("Chrome", "mp4-h264-aac-vtt-2600k", PlayMethod.DirectStream)] // #6450 should be DirectPlay
        [InlineData("Chrome", "mp4-h264-ac3-aac-srt-2600k", PlayMethod.Transcode, TranscodeReason.AudioCodecNotSupported)] // #6450 should be DirectPlay
        [InlineData("Chrome", "mp4-h264-ac3-srt-2600k", PlayMethod.Transcode, TranscodeReason.AudioCodecNotSupported)] // #6450 should be DirectStream
        [InlineData("Chrome", "mp4-hevc-aac-srt-15200k", PlayMethod.Transcode, TranscodeReason.VideoCodecNotSupported, "Transcode")]
        [InlineData("Chrome", "mp4-hevc-ac3-aac-srt-15200k", PlayMethod.Transcode, TranscodeReason.VideoCodecNotSupported | TranscodeReason.AudioCodecNotSupported, "Transcode")]
        [InlineData("Chrome", "mkv-vp9-aac-srt-2600k", PlayMethod.Transcode, TranscodeReason.AudioCodecNotSupported, "Transcode")] // #6450 should be 'false'
        [InlineData("Chrome", "mkv-vp9-ac3-srt-2600k", PlayMethod.Transcode, TranscodeReason.AudioCodecNotSupported, "Transcode")] // #6450 should be 'false'
        [InlineData("Chrome", "mkv-vp9-vorbis-vtt-2600k", PlayMethod.DirectStream)] // #6450 should be DirectPlay
        // Firefox
        [InlineData("Firefox", "mp4-h264-aac-vtt-2600k", PlayMethod.DirectStream)] // #6450 should be DirectPlay
        [InlineData("Firefox", "mp4-h264-ac3-aac-srt-2600k", PlayMethod.Transcode, TranscodeReason.AudioCodecNotSupported)] // #6450 should be DirectPlay
        [InlineData("Firefox", "mp4-h264-ac3-srt-2600k", PlayMethod.Transcode, TranscodeReason.AudioCodecNotSupported)] // #6450 should be DirectStream
        [InlineData("Firefox", "mp4-hevc-aac-srt-15200k", PlayMethod.Transcode, TranscodeReason.VideoCodecNotSupported, "Transcode")]
        [InlineData("Firefox", "mp4-hevc-ac3-aac-srt-15200k", PlayMethod.Transcode, TranscodeReason.VideoCodecNotSupported | TranscodeReason.AudioCodecNotSupported, "Transcode")]
        [InlineData("Firefox", "mkv-vp9-aac-srt-2600k", PlayMethod.Transcode, TranscodeReason.AudioCodecNotSupported, "Transcode")] // #6450 should be 'false'
        [InlineData("Firefox", "mkv-vp9-ac3-srt-2600k", PlayMethod.Transcode, TranscodeReason.AudioCodecNotSupported, "Transcode")] // #6450 should be 'false'
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
        [InlineData("AndroidPixel", "mp4-hevc-aac-srt-15200k", PlayMethod.Transcode, TranscodeReason.ContainerBitrateExceedsLimit, "Transcode")]
        [InlineData("AndroidPixel", "mp4-hevc-ac3-aac-srt-15200k", PlayMethod.Transcode, TranscodeReason.ContainerBitrateExceedsLimit, "Transcode")]
        // Yatse
        [InlineData("Yatse", "mp4-h264-aac-srt-2600k", PlayMethod.DirectStream)] // #6450 should be DirectPlay
        [InlineData("Yatse", "mp4-h264-ac3-aac-srt-2600k", PlayMethod.Transcode, TranscodeReason.AudioCodecNotSupported)] // #6450 should be DirectPlay
        [InlineData("Yatse", "mp4-h264-ac3-srt-2600k", PlayMethod.Transcode, TranscodeReason.AudioCodecNotSupported, "Transcode")]
        [InlineData("Yatse", "mp4-hevc-aac-srt-15200k", PlayMethod.DirectStream)] // #6450 should be DirectPlay
        [InlineData("Yatse", "mp4-hevc-ac3-aac-srt-15200k", PlayMethod.Transcode, TranscodeReason.AudioCodecNotSupported, "Transcode")] // #6450 should be DirectPlay
        // RokuSSPlus
        [InlineData("RokuSSPlus", "mp4-h264-aac-srt-2600k", PlayMethod.DirectStream)] // #6450 should be DirectPlay
        [InlineData("RokuSSPlus", "mp4-h264-ac3-aac-srt-2600k", PlayMethod.Transcode, TranscodeReason.AudioCodecNotSupported)] // #6450 should be DirectPlay
        [InlineData("RokuSSPlus", "mp4-h264-ac3-srt-2600k", PlayMethod.Transcode, TranscodeReason.AudioCodecNotSupported)] // #6450 should be DirectStream
        [InlineData("RokuSSPlus", "mp4-hevc-aac-srt-15200k", PlayMethod.DirectStream)] // #6450 should be DirectPlay
        [InlineData("RokuSSPlus", "mp4-hevc-ac3-aac-srt-15200k", PlayMethod.Transcode, TranscodeReason.AudioCodecNotSupported, "Transcode")] // #6450 should be DirectPlay
        [InlineData("RokuSSPlus", "mp4-hevc-ac3-srt-15200k", PlayMethod.Transcode, TranscodeReason.AudioCodecNotSupported, "Transcode")] // #6450 should be DirectStream
        // JellyfinMediaPlayer
        [InlineData("JellyfinMediaPlayer", "mp4-h264-aac-vtt-2600k", PlayMethod.DirectStream)] // #6450 should be DirectPlay
        [InlineData("JellyfinMediaPlayer", "mp4-h264-ac3-aac-srt-2600k", PlayMethod.DirectStream)] // #6450 should be DirectPlay
        [InlineData("JellyfinMediaPlayer", "mp4-h264-ac3-srt-2600k", PlayMethod.DirectStream)] // #6450 should be DirectPlay
        [InlineData("JellyfinMediaPlayer", "mp4-hevc-aac-srt-15200k", PlayMethod.Transcode, TranscodeReason.ContainerBitrateExceedsLimit)] // #6450 should be DirectPlay
        [InlineData("JellyfinMediaPlayer", "mp4-hevc-ac3-aac-srt-15200k", PlayMethod.Transcode, TranscodeReason.ContainerBitrateExceedsLimit)] // #6450 should be DirectPlay
        [InlineData("JellyfinMediaPlayer", "mkv-vp9-aac-srt-2600k", PlayMethod.DirectStream)] // #6450 should be DirectPlay
        [InlineData("JellyfinMediaPlayer", "mkv-vp9-ac3-srt-2600k", PlayMethod.DirectStream)] // #6450 should be DirectPlay
        [InlineData("JellyfinMediaPlayer", "mkv-vp9-vorbis-vtt-2600k", PlayMethod.DirectStream)] // #6450 should be DirectPlay
        public async Task BuildVideoItemWithFirstExplicitStream(string deviceName, string mediaSource, PlayMethod?playMethod, TranscodeReason why = TranscodeReason.None, string transcodeMode = "DirectStream", string transcodeProtocol = "")
        {
            var options = await GetVideoOptions(deviceName, mediaSource);
            options.AudioStreamIndex = 1;
            options.SubtitleStreamIndex = options.MediaSources[0].MediaStreams.Count() - 1;
            BuildVideoItemSimpleTest(options, playMethod, why, transcodeMode, transcodeProtocol);
        }

        [Theory]
        // Chrome
        [InlineData("Chrome", "mp4-h264-ac3-aac-srt-2600k", PlayMethod.Transcode, TranscodeReason.SecondaryAudioNotSupported)] // #6450 should be DirectPlay
        [InlineData("Chrome", "mp4-hevc-ac3-aac-srt-15200k", PlayMethod.Transcode, TranscodeReason.VideoCodecNotSupported, "Transcode")] // #6450 should have container & profile video reasons?
        // Firefox
        [InlineData("Firefox", "mp4-h264-ac3-aac-srt-2600k", PlayMethod.Transcode, TranscodeReason.SecondaryAudioNotSupported)] // #6450 should be DirectPlay
        [InlineData("Firefox", "mp4-hevc-ac3-aac-srt-15200k", PlayMethod.Transcode, TranscodeReason.VideoCodecNotSupported, "Transcode")] // #6450 should have container & profile video reasons?
        // Yatse
        [InlineData("Yatse", "mp4-h264-ac3-aac-srt-2600k", PlayMethod.Transcode, TranscodeReason.SecondaryAudioNotSupported)] // #6450 should be DirectPlay
        [InlineData("Yatse", "mp4-hevc-ac3-aac-srt-15200k", PlayMethod.Transcode, TranscodeReason.SecondaryAudioNotSupported, "Transcode")] // #6450 should be DirectPlay
        // RokuSSPlus
        [InlineData("RokuSSPlus", "mp4-h264-ac3-aac-srt-2600k", PlayMethod.DirectStream)] // #6450 should be DirectPlay
        [InlineData("RokuSSPlus", "mp4-hevc-ac3-aac-srt-15200k", PlayMethod.DirectStream)] // #6450 should be DirectPlay
        public async Task BuildVideoItemWithDirectPlayExplicitStreams(string deviceName, string mediaSource, PlayMethod playMethod, TranscodeReason why = TranscodeReason.None, string transcodeMode = "DirectStream", string transcodeProtocol = "")
        {
            var options = await GetVideoOptions(deviceName, mediaSource);
            var streamCount = options.MediaSources[0].MediaStreams.Count();
            options.AudioStreamIndex = streamCount - 2;
            options.SubtitleStreamIndex = streamCount - 1;
            BuildVideoItemSimpleTest(options, playMethod, why, transcodeMode, transcodeProtocol);
        }

        private void BuildVideoItemSimpleTest(VideoOptions options, PlayMethod? playMethod, TranscodeReason why, string transcodeMode, string transcodeProtocol)
        {
            if (string.IsNullOrEmpty(transcodeProtocol))
            {
                transcodeProtocol = playMethod == PlayMethod.DirectStream ? "http" : "hls";
            }

            var builder = GetStreamBuilder();

            var val = builder.BuildVideoItem(options);
            Assert.NotNull(val);

            if (playMethod != null)
            {
                Assert.Equal(playMethod, val.PlayMethod);
            }

            Assert.Equal(why, val.TranscodeReasons);

            var audioStreamIndexInput = options.AudioStreamIndex;
            var targetVideoStream = val.TargetVideoStream;
            var targetAudioStream = val.TargetAudioStream;

            var mediaSource = options.MediaSources.First(source => source.Id == val.MediaSourceId);
            Assert.NotNull(mediaSource);
            var videoStreams = mediaSource.MediaStreams.Where(stream => stream.Type == MediaStreamType.Video);
            var audioStreams = mediaSource.MediaStreams.Where(stream => stream.Type == MediaStreamType.Audio);
            // TODO: check AudioStreamIndex vs options.AudioStreamIndex
            var inputAudioStream = mediaSource.GetDefaultAudioStream(audioStreamIndexInput ?? mediaSource.DefaultAudioStreamIndex);

            var uri = ParseUri(val);

            if (playMethod == PlayMethod.DirectPlay)
            {
                // check expected container
                var containers = ContainerProfile.SplitValue(mediaSource.Container);
                Assert.Contains(uri.Extension, containers);

                // check expected video codec (1)
                Assert.Contains(targetVideoStream.Codec, val.TargetVideoCodec);
                Assert.Single(val.TargetVideoCodec);

                // check expected audio codecs (1)
                Assert.Contains(targetAudioStream.Codec, val.TargetAudioCodec);
                Assert.Single(val.AudioCodecs);

                // TODO: validate transcoding options as well
            }
            else if (playMethod == PlayMethod.DirectStream || playMethod == PlayMethod.Transcode)
            {
                Assert.NotNull(val.Container);
                // Assert.NotEmpty(val.VideoCodecs);
                // Assert.NotEmpty(val.AudioCodecs);

                // check expected container (todo: this could be a test param)
                if (transcodeProtocol == "http")
                {
                    // Assert.Equal("webm", val.Container);
                    Assert.Equal(val.Container, uri.Extension);
                    Assert.Equal("stream", uri.Filename);
                    // Assert.Equal("http", val.SubProtocol);
                }
                else
                {
                    Assert.Equal("ts", val.Container);
                    Assert.Equal("m3u8", uri.Extension);
                    Assert.Equal("master", uri.Filename);
                    Assert.Equal("hls", val.SubProtocol);
                }

                // Full transcode
                if (transcodeMode == "Transcode")
                {
                    // TODO: what else to validate here
                    if ((val.TranscodeReasons & TranscodeReason.ContainerReasons) == TranscodeReason.None)
                    {
                        // Assert.All(
                        //     videoStreams,
                        //     stream => Assert.DoesNotContain(stream.Codec, val.VideoCodecs));
                    }

                    // todo: fill out tests here
                }

                // DirectStream and Remux
                else
                {
                    // check expected video codec (1)
                    Assert.Contains(targetVideoStream.Codec, val.TargetVideoCodec);
                    Assert.Single(val.TargetVideoCodec);

                    if (transcodeMode == "DirectStream")
                    {
                        if (!targetAudioStream.IsExternal)
                        {
                            // check expected audio codecs (1)
                            // Assert.DoesNotContain(targetAudioStream.Codec, val.AudioCodecs);
                        }
                    }
                    else if (transcodeMode == "Remux")
                    {
                        // check expected audio codecs (1)
                        Assert.Contains(targetAudioStream.Codec, val.AudioCodecs);
                        Assert.Single(val.AudioCodecs);
                    }

                    // video details
                    var videoStream = targetVideoStream;
                    Assert.False(val.EstimateContentLength);
                    Assert.Equal(TranscodeSeekInfo.Auto, val.TranscodeSeekInfo);
                    // Assert.Contains(videoStream.Profile?.ToLowerInvariant() ?? string.Empty, val.TargetVideoProfile?.Split(",").Select(s => s.ToLowerInvariant()) ?? new string[0]);
                    // Assert.Equal(videoStream.Level, val.TargetVideoLevel);
                    // Assert.Equal(videoStream.BitDepth, val.TargetVideoBitDepth);
                    // Assert.InRange(val.VideoBitrate.GetValueOrDefault(), videoStream.BitRate.GetValueOrDefault(), int.MaxValue);

                    // audio codec not supported
                    if ((why & TranscodeReason.AudioCodecNotSupported) != TranscodeReason.None)
                    {
                        // audio stream specified
                        if (options.AudioStreamIndex >= 0)
                        {
                            // TODO:fixme
                            if (!targetAudioStream.IsExternal)
                            {
                                Assert.DoesNotContain(targetAudioStream.Codec, val.AudioCodecs);
                            }
                        }

                        // audio stream not specified
                        else
                        {
                            // TODO:fixme
                            Assert.All(audioStreams, stream =>
                            {
                                if (!stream.IsExternal)
                                {
                                    // Assert.DoesNotContain(stream.Codec, val.AudioCodecs);
                                }
                            });
                        }
                    }
                }
            }

            if (playMethod == null)
            {
                // what should the actual result be here?
                Assert.Null(val.SubProtocol);
                Assert.EndsWith("/stream", uri.Path, StringComparison.InvariantCulture);

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

        private static (string Path, NameValueCollection Query, string Filename, string Extension) ParseUri(StreamInfo val)
        {
            var href = val.ToUrl("media:", "ACCESSTOKEN").Split("?", 2);
            var path = href[0];

            var queryString = href.ElementAtOrDefault(1);
            var query = string.IsNullOrEmpty(queryString) ? System.Web.HttpUtility.ParseQueryString(queryString ?? string.Empty) : new NameValueCollection();

            var filename = System.IO.Path.GetFileNameWithoutExtension(path);
            var extension = System.IO.Path.GetExtension(path);
            if (extension.Length > 0)
            {
                extension = extension.Substring(1);
            }

            return (path, query, filename, extension);
        }
    }
}
