using System;
using System.Globalization;
using System.IO;
using Jellyfin.Data.Enums;
using Jellyfin.Extensions;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Controller.Streaming;

/// <summary>
/// Builds the ffmpeg arguments for HLS transcodes, shared by playback and anything that must run the same command.
/// </summary>
public class HlsCommandBuilder
{
    private const EncoderPreset DefaultVodEncoderPreset = EncoderPreset.veryfast;
    private const EncoderPreset DefaultEventEncoderPreset = EncoderPreset.superfast;
    private readonly Version _minFFmpegFlacInMp4 = new Version(6, 0);
    private readonly Version _minFFmpegX265BframeInFmp4 = new Version(7, 0, 1);
    private readonly Version _minFFmpegHlsSegmentOptions = new Version(5, 0);
    private readonly EncodingHelper _encodingHelper;
    private readonly IMediaEncoder _mediaEncoder;
    private readonly ILogger<HlsCommandBuilder> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="HlsCommandBuilder"/> class.
    /// </summary>
    /// <param name="encodingHelper">Instance of <see cref="EncodingHelper"/>.</param>
    /// <param name="mediaEncoder">Instance of the <see cref="IMediaEncoder"/> interface.</param>
    /// <param name="logger">Instance of the <see cref="ILogger{HlsCommandBuilder}"/> interface.</param>
    public HlsCommandBuilder(EncodingHelper encodingHelper, IMediaEncoder mediaEncoder, ILogger<HlsCommandBuilder> logger)
    {
        _encodingHelper = encodingHelper;
        _mediaEncoder = mediaEncoder;
        _logger = logger;
    }

    /// <summary>
    /// Gets the complete ffmpeg arguments for an HLS transcode.
    /// </summary>
    /// <param name="outputPath">The playlist path.</param>
    /// <param name="state">The <see cref="StreamState"/>.</param>
    /// <param name="encodingOptions">The encoding options to build the arguments with.</param>
    /// <param name="isEventPlaylist">Whether the playlist is EVENT or VOD.</param>
    /// <param name="startNumber">The first number in the hls sequence.</param>
    /// <returns>The ffmpeg command line arguments.</returns>
    public string GetCommandLineArguments(string outputPath, StreamState state, EncodingOptions encodingOptions, bool isEventPlaylist, int startNumber)
    {
        var videoCodec = _encodingHelper.GetVideoEncoder(state, encodingOptions);
        var threads = EncodingHelper.GetNumberOfThreads(state, encodingOptions, videoCodec);

        var mapArgs = state.IsOutputVideo ? _encodingHelper.GetMapArgs(state) : string.Empty;

        var directory = Path.GetDirectoryName(outputPath) ?? throw new ArgumentException($"Provided path ({outputPath}) is not valid.", nameof(outputPath));
        var outputFileNameWithoutExtension = Path.GetFileNameWithoutExtension(outputPath);
        var outputPrefix = Path.Combine(directory, outputFileNameWithoutExtension);
        var outputExtension = EncodingHelper.GetSegmentFileExtension(state.Request.SegmentContainer);
        var outputTsArg = outputPrefix + "%d" + outputExtension;

        var segmentFormat = string.Empty;
        var segmentContainer = outputExtension.TrimStart('.');
        var inputModifier = _encodingHelper.GetInputModifier(state, encodingOptions, segmentContainer);
        var hlsArguments = $"-hls_playlist_type {(isEventPlaylist ? "event" : "vod")} -hls_list_size 0";

        if (string.Equals(segmentContainer, "ts", StringComparison.OrdinalIgnoreCase))
        {
            segmentFormat = "mpegts";
        }
        else if (string.Equals(segmentContainer, "mp4", StringComparison.OrdinalIgnoreCase))
        {
            var outputFmp4HeaderArg = OperatingSystem.IsWindows() switch
            {
                // on Windows, the path of fmp4 header file needs to be configured
                true => " -hls_fmp4_init_filename \"" + outputPrefix + "-1" + outputExtension + "\"",
                // on Linux/Unix, ffmpeg generate fmp4 header file to m3u8 output folder
                false => " -hls_fmp4_init_filename \"" + outputFileNameWithoutExtension + "-1" + outputExtension + "\""
            };

            var useLegacySegmentOption = _mediaEncoder.EncoderVersion < _minFFmpegHlsSegmentOptions;

            if (state.VideoStream is not null && state.IsOutputVideo)
            {
                // fMP4 needs frag_discont to write the audio packet DTS/PTS including the initial delay into MOOF::TRAF::TFDT
                // HLS does not use SIDX, and skipping it avoids FFmpeg rewriting open-GOP boundary packet PTS
                hlsArguments += $" {(useLegacySegmentOption ? "-hls_ts_options" : "-hls_segment_options")} movflags=+frag_discont+skip_sidx";
            }

            segmentFormat = "fmp4" + outputFmp4HeaderArg;
        }
        else
        {
            _logger.LogError("Invalid HLS segment container: {SegmentContainer}, default to mpegts", segmentContainer);
            segmentFormat = "mpegts";
        }

        var maxMuxingQueueSize = encodingOptions.MaxMuxingQueueSize > 128
            ? encodingOptions.MaxMuxingQueueSize.ToString(CultureInfo.InvariantCulture)
            : "128";

        var baseUrlParam = string.Empty;
        if (isEventPlaylist)
        {
            baseUrlParam = string.Format(
                CultureInfo.InvariantCulture,
                " -hls_base_url \"hls/{0}/\"",
                Path.GetFileNameWithoutExtension(outputPath));
        }

        return string.Format(
            CultureInfo.InvariantCulture,
            "{0} {1} -map_metadata -1 -map_chapters -1 -threads {2} {3} {4} {5} -copyts -avoid_negative_ts disabled -max_muxing_queue_size {6} -f hls -max_delay 5000000 -hls_time {7} -hls_segment_type {8} -start_number {9}{10} -hls_segment_filename \"{11}\" {12} -y \"{13}\"",
            inputModifier,
            _encodingHelper.GetInputArgument(state, encodingOptions, segmentContainer),
            threads,
            mapArgs,
            GetVideoArguments(state, encodingOptions, startNumber, isEventPlaylist, segmentContainer),
            GetAudioArguments(state, encodingOptions),
            maxMuxingQueueSize,
            state.SegmentLength.ToString(CultureInfo.InvariantCulture),
            segmentFormat,
            startNumber.ToString(CultureInfo.InvariantCulture),
            baseUrlParam,
            outputTsArg.EscapeProcessArgument(),
            hlsArguments,
            outputPath.EscapeProcessArgument()).Trim();
    }

    /// <summary>
    /// Gets the audio arguments for transcoding.
    /// </summary>
    /// <param name="state">The <see cref="StreamState"/>.</param>
    /// <param name="encodingOptions">The encoding options to build the arguments with.</param>
    /// <returns>The command line arguments for audio transcoding.</returns>
    internal string GetAudioArguments(StreamState state, EncodingOptions encodingOptions)
    {
        if (state.AudioStream is null)
        {
            return string.Empty;
        }

        var audioCodec = _encodingHelper.GetAudioEncoder(state);
        var bitStreamArgs = _encodingHelper.GetAudioBitStreamArguments(state, state.Request.SegmentContainer, state.MediaSource.Container);

        // opus, dts, truehd and flac (in FFmpeg 5 and older) are experimental in mp4 muxer
        var strictArgs = string.Empty;
        var actualOutputAudioCodec = state.ActualOutputAudioCodec;
        if (string.Equals(actualOutputAudioCodec, "opus", StringComparison.OrdinalIgnoreCase)
            || string.Equals(actualOutputAudioCodec, "dts", StringComparison.OrdinalIgnoreCase)
            || string.Equals(actualOutputAudioCodec, "truehd", StringComparison.OrdinalIgnoreCase)
            || (string.Equals(actualOutputAudioCodec, "flac", StringComparison.OrdinalIgnoreCase)
                && _mediaEncoder.EncoderVersion < _minFFmpegFlacInMp4))
        {
            strictArgs = " -strict -2";
        }

        if (!state.IsOutputVideo)
        {
            var audioTranscodeParams = string.Empty;

            // -vn to drop any video streams
            audioTranscodeParams += "-vn";

            if (EncodingHelper.IsCopyCodec(audioCodec))
            {
                return audioTranscodeParams + " -acodec copy" + bitStreamArgs + strictArgs;
            }

            audioTranscodeParams += " -acodec " + audioCodec + bitStreamArgs + strictArgs;

            var audioBitrate = state.OutputAudioBitrate;
            var audioChannels = state.OutputAudioChannels;

            if (audioBitrate.HasValue && !EncodingHelper.LosslessAudioCodecs.Contains(state.ActualOutputAudioCodec, StringComparison.OrdinalIgnoreCase))
            {
                var vbrParam = _encodingHelper.GetAudioVbrModeParam(audioCodec, audioBitrate.Value, audioChannels ?? 2);
                if (encodingOptions.EnableAudioVbr && state.EnableAudioVbrEncoding && vbrParam is not null)
                {
                    audioTranscodeParams += vbrParam;
                }
                else
                {
                    audioTranscodeParams += " -ab " + audioBitrate.Value.ToString(CultureInfo.InvariantCulture);
                }
            }

            if (audioChannels.HasValue)
            {
                audioTranscodeParams += " -ac " + audioChannels.Value.ToString(CultureInfo.InvariantCulture);
            }

            if (state.OutputAudioSampleRate.HasValue)
            {
                audioTranscodeParams += " -ar " + state.OutputAudioSampleRate.Value.ToString(CultureInfo.InvariantCulture);
            }

            return audioTranscodeParams;
        }

        if (EncodingHelper.IsCopyCodec(audioCodec))
        {
            var videoCodec = _encodingHelper.GetVideoEncoder(state, encodingOptions);
            var copyArgs = "-codec:a:0 copy" + bitStreamArgs + strictArgs;

            return copyArgs;
        }

        var args = "-codec:a:0 " + audioCodec + bitStreamArgs + strictArgs;

        var channels = state.OutputAudioChannels;

        var useDownMixAlgorithm = DownMixAlgorithmsHelper.AlgorithmFilterStrings.ContainsKey((encodingOptions.DownMixStereoAlgorithm, DownMixAlgorithmsHelper.InferChannelLayout(state.AudioStream)));

        if (channels.HasValue
            && (channels.Value != 2
                || (state.AudioStream?.Channels is not null && !useDownMixAlgorithm)))
        {
            args += " -ac " + channels.Value;
        }

        var bitrate = state.OutputAudioBitrate;
        if (bitrate.HasValue && !EncodingHelper.LosslessAudioCodecs.Contains(actualOutputAudioCodec, StringComparison.OrdinalIgnoreCase))
        {
            var vbrParam = _encodingHelper.GetAudioVbrModeParam(audioCodec, bitrate.Value, channels ?? 2);
            if (encodingOptions.EnableAudioVbr && state.EnableAudioVbrEncoding && vbrParam is not null)
            {
                args += vbrParam;
            }
            else
            {
                args += " -ab " + bitrate.Value.ToString(CultureInfo.InvariantCulture);
            }
        }

        if (state.OutputAudioSampleRate.HasValue)
        {
            args += " -ar " + state.OutputAudioSampleRate.Value.ToString(CultureInfo.InvariantCulture);
        }
        else if (state.AudioStream?.CodecTag is not null && state.AudioStream.CodecTag.Equals("ac-4", StringComparison.Ordinal))
        {
            // ac-4 audio tends to have a super weird sample rate that will fail most encoders
            // force resample it to 48KHz
            args += " -ar 48000";
        }

        args += _encodingHelper.GetAudioFilterParam(state, encodingOptions);

        return args;
    }

    /// <summary>
    /// Selects the HEVC Dolby Vision sample entry tag.
    /// </summary>
    /// <param name="stream">The video stream.</param>
    /// <returns>The MP4 sample entry tag.</returns>
    internal static string GetDolbyVisionHevcCodecTag(MediaStream stream)
    {
        return stream.DvProfile == 8 ? "hvc1" : "dvh1";
    }

    /// <summary>
    /// Gets the video arguments for transcoding.
    /// </summary>
    /// <param name="state">The <see cref="StreamState"/>.</param>
    /// <param name="encodingOptions">The encoding options to build the arguments with.</param>
    /// <param name="startNumber">The first number in the hls sequence.</param>
    /// <param name="isEventPlaylist">Whether the playlist is EVENT or VOD.</param>
    /// <param name="segmentContainer">The segment container.</param>
    /// <returns>The command line arguments for video transcoding.</returns>
    internal string GetVideoArguments(StreamState state, EncodingOptions encodingOptions, int startNumber, bool isEventPlaylist, string segmentContainer)
    {
        if (state.VideoStream is null)
        {
            return string.Empty;
        }

        if (!state.IsOutputVideo)
        {
            return string.Empty;
        }

        var codec = _encodingHelper.GetVideoEncoder(state, encodingOptions);

        var args = "-codec:v:0 " + codec;

        var isActualOutputVideoCodecAv1 = string.Equals(state.ActualOutputVideoCodec, "av1", StringComparison.OrdinalIgnoreCase);
        var isActualOutputVideoCodecHevc = string.Equals(state.ActualOutputVideoCodec, "h265", StringComparison.OrdinalIgnoreCase)
                                           || string.Equals(state.ActualOutputVideoCodec, "hevc", StringComparison.OrdinalIgnoreCase);

        if (isActualOutputVideoCodecHevc || isActualOutputVideoCodecAv1)
        {
            var requestedRange = state.GetRequestedRangeTypes(state.ActualOutputVideoCodec);
            // Clients reporting Dolby Vision capabilities with fallbacks may only support the fallback layer.
            // Only enable Dolby Vision remuxing if the client explicitly declares support for profiles without fallbacks.
            var clientSupportsDoVi = requestedRange.Contains(VideoRangeType.DOVI.ToString(), StringComparison.OrdinalIgnoreCase);
            var videoIsDoVi = EncodingHelper.IsDovi(state.VideoStream);

            if (EncodingHelper.IsCopyCodec(codec)
                && (videoIsDoVi && clientSupportsDoVi)
                && !_encodingHelper.IsDoviRemoved(state))
            {
                if (isActualOutputVideoCodecHevc)
                {
                    var codecTag = GetDolbyVisionHevcCodecTag(state.VideoStream);
                    args += $" -tag:v:0 {codecTag} -strict -2";
                }
                else if (isActualOutputVideoCodecAv1)
                {
                    args += " -tag:v:0 dav1 -strict -2";
                }
            }
            else if (isActualOutputVideoCodecHevc)
            {
                // Prefer hvc1 to hev1
                args += " -tag:v:0 hvc1";
            }
        }

        // if  (state.EnableMpegtsM2TsMode)
        // {
        //     args += " -mpegts_m2ts_mode 1";
        // }

        // See if we can save come cpu cycles by avoiding encoding.
        if (EncodingHelper.IsCopyCodec(codec))
        {
            // If h264_mp4toannexb is ever added, do not use it for live tv.
            if (state.VideoStream is not null && !string.Equals(state.VideoStream.NalLengthSize, "0", StringComparison.OrdinalIgnoreCase))
            {
                string bitStreamArgs = _encodingHelper.GetBitStreamArgs(state, MediaStreamType.Video);
                if (!string.IsNullOrEmpty(bitStreamArgs))
                {
                    args += " " + bitStreamArgs;
                }
            }

            args += " -start_at_zero";
        }
        else
        {
            args += _encodingHelper.GetVideoQualityParam(state, codec, encodingOptions, isEventPlaylist ? DefaultEventEncoderPreset : DefaultVodEncoderPreset);

            // Set the key frame params for video encoding to match the hls segment time.
            args += _encodingHelper.GetHlsVideoKeyFrameArguments(state, codec, state.SegmentLength, isEventPlaylist, startNumber);

            // Currently b-frames in libx265 breaks the FMP4-HLS playback on iOS, disable it for now.
            if (string.Equals(codec, "libx265", StringComparison.OrdinalIgnoreCase)
                && _mediaEncoder.EncoderVersion < _minFFmpegX265BframeInFmp4)
            {
                args += " -bf 0";
            }

            // video processing filters.
            var videoProcessParam = _encodingHelper.GetVideoProcessingFilterParam(state, encodingOptions, codec);

            var negativeMapArgs = _encodingHelper.GetNegativeMapArgsByFilters(state, videoProcessParam);

            args = negativeMapArgs + args + videoProcessParam;

            // -start_at_zero is necessary to use with -ss when seeking,
            // otherwise the target position cannot be determined.
            if (state.SubtitleStream is not null)
            {
                // Disable start_at_zero for external graphical subs
                if (!(state.SubtitleStream.IsExternal && !state.SubtitleStream.IsTextSubtitleStream))
                {
                    args += " -start_at_zero";
                }
            }
        }

        // TODO why was this not enabled for VOD?
        if (isEventPlaylist && string.Equals(segmentContainer, "ts", StringComparison.OrdinalIgnoreCase))
        {
            args += " -flags -global_header";
        }

        if (!string.IsNullOrEmpty(state.OutputVideoSync))
        {
            args += EncodingHelper.GetVideoSyncOption(state.OutputVideoSync, _mediaEncoder.EncoderVersion);
        }

        args += _encodingHelper.GetOutputFFlags(state);

        return args;
    }
}
