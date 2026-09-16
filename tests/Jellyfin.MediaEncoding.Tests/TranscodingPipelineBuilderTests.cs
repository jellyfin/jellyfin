using System;
using System.Linq;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.MediaEncoding.Transcoding;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Session;
using Xunit;

namespace Jellyfin.MediaEncoding.Tests;

public class TranscodingPipelineBuilderTests
{
    private static EncodingJobInfo CreateVideoState(string sourceCodec, string outputCodec)
    {
        return new EncodingJobInfo(TranscodingJobType.Progressive)
        {
            VideoStream = new MediaStream { Type = MediaStreamType.Video, Codec = sourceCodec },
            OutputVideoCodec = outputCodec
        };
    }

    [Fact]
    public void Build_ReturnsCopyStage_WhenVideoIsRemuxed()
    {
        var state = CreateVideoState("hevc", "copy");
        state.VideoStream!.BitDepth = 10;

        var result = TranscodingPipelineBuilder.Build(state, "-c:v copy out.mp4");

        // A remux still has a pipeline: the bitstream is passed through and only rewrapped.
        Assert.NotNull(result);
        var stage = Assert.Single(result.Stages!);
        Assert.Equal(TranscodeStageType.Copy, stage.Type);
        Assert.Equal("hevc", stage.Name);
        Assert.Equal("Video", stage.MediaType);
        Assert.False(stage.IsHardware);
        // A bitstream copy cannot change the format, so the source values carry over.
        Assert.Equal(10, stage.VideoBitDepth);
    }

    [Fact]
    public void Build_ReturnsNull_WhenNoVideoStream()
    {
        var state = new EncodingJobInfo(TranscodingJobType.Progressive) { OutputVideoCodec = "h264" };
        var result = TranscodingPipelineBuilder.Build(state, "-c:v h264_qsv out.mp4");

        Assert.Null(result);
    }

    [Fact]
    public void Build_ParsesQsvWithOpenClToneMapPipeline()
    {
        var state = CreateVideoState("hevc", "h264");
        // Representative Intel QSV decode + scale, OpenCL tone map, QSV encode chain.
        const string Args = "-hwaccel qsv -hwaccel_output_format qsv -c:v hevc_qsv -i input.mkv "
            + "-vf \"vpp_qsv=w=640:h=360,hwmap=derive_device=opencl,tonemap_opencl=tonemap=hable:format=nv12,hwmap=derive_device=qsv\" "
            + "-c:v h264_qsv -b:v 808000 out.ts";

        var result = TranscodingPipelineBuilder.Build(state, Args);

        Assert.NotNull(result);
        var stages = result.Stages!.ToList();

        Assert.Equal(TranscodeStageType.Decode, stages[0].Type);
        Assert.Equal("hevc_qsv", stages[0].Name);
        Assert.Equal(HardwareFramework.Qsv, stages[0].Framework);
        Assert.Equal("H.265 (HEVC)", stages[0].Detail);
        Assert.True(stages[0].IsHardware);

        var scale = stages.Single(s => s.Type == TranscodeStageType.Scale);
        Assert.Equal("vpp_qsv", scale.Name);
        Assert.Equal(HardwareFramework.Qsv, scale.Framework);
        Assert.Equal("640x360", scale.Detail);

        var tonemap = stages.Single(s => s.Type == TranscodeStageType.ToneMap);
        Assert.Equal("tonemap_opencl", tonemap.Name);
        Assert.Equal(HardwareFramework.OpenCl, tonemap.Framework);
        Assert.Equal("Hable", tonemap.Detail);

        var encode = stages.Single(s => s.Type == TranscodeStageType.Encode);
        Assert.Equal("h264_qsv", encode.Name);
        Assert.Equal(HardwareFramework.Qsv, encode.Framework);
        Assert.Equal("H.264 (AVC)", encode.Detail);
    }

    [Fact]
    public void Build_SurfacesHwDownloadAndHwUpload_AroundSoftwareFilter()
    {
        var state = CreateVideoState("h264", "h264");
        // VAAPI decode, download to system memory for a software scale, then upload back to VAAPI
        // for the hardware encode. The two memory transfers must each appear as their own stage.
        const string Args = "-hwaccel vaapi -hwaccel_output_format vaapi -c:v h264_vaapi -i input.mkv "
            + "-vf \"hwdownload,format=nv12,scale=1280:720,format=nv12,hwupload=derive_device=vaapi\" "
            + "-c:v h264_vaapi out.ts";

        var result = TranscodingPipelineBuilder.Build(state, Args);

        Assert.NotNull(result);
        var stages = result.Stages!.ToList();

        var download = stages.Single(s => s.Type == TranscodeStageType.HardwareDownload);
        Assert.Equal("hwdownload", download.Name);
        Assert.Equal(HardwareFramework.Software, download.Framework);
        Assert.False(download.IsHardware);

        var scale = stages.Single(s => s.Type == TranscodeStageType.Scale);
        Assert.Equal(HardwareFramework.Software, scale.Framework);
        Assert.False(scale.IsHardware);

        var upload = stages.Single(s => s.Type == TranscodeStageType.HardwareUpload);
        Assert.Equal("hwupload", upload.Name);
        // The target framework is taken from the derive_device argument.
        Assert.Equal(HardwareFramework.Vaapi, upload.Framework);
        Assert.True(upload.IsHardware);

        // Ordering: download precedes the software scale, which precedes the upload back to hardware.
        Assert.True(stages.IndexOf(download) < stages.IndexOf(scale));
        Assert.True(stages.IndexOf(scale) < stages.IndexOf(upload));
    }

    [Theory]
    [InlineData("hwupload_cuda", HardwareFramework.Cuda)]
    [InlineData("hwupload_vaapi", HardwareFramework.Vaapi)]
    public void Build_ResolvesHwUploadFrameworkFromNameSuffix(string uploadFilter, HardwareFramework expected)
    {
        var state = CreateVideoState("h264", "h264");
        var result = TranscodingPipelineBuilder.Build(
            state,
            $"-i input.mkv -vf \"scale=1280:720,{uploadFilter}\" -c:v h264_nvenc out.ts");

        Assert.NotNull(result);
        var upload = result.Stages!.Single(s => s.Type == TranscodeStageType.HardwareUpload);
        Assert.Equal(uploadFilter, upload.Name);
        Assert.Equal(expected, upload.Framework);
        Assert.True(upload.IsHardware);
    }

    [Fact]
    public void Build_DoesNotSurfaceHwMap_AsAMemoryTransfer()
    {
        var state = CreateVideoState("hevc", "h264");
        // hwmap remaps between two hardware contexts (QSV <-> OpenCL) and is not a software/hardware
        // memory transfer, so it must remain dropped plumbing.
        const string Args = "-hwaccel qsv -hwaccel_output_format qsv -c:v hevc_qsv -i input.mkv "
            + "-vf \"vpp_qsv=w=640:h=360,hwmap=derive_device=opencl,tonemap_opencl=tonemap=hable:format=nv12,hwmap=derive_device=qsv\" "
            + "-c:v h264_qsv out.ts";

        var result = TranscodingPipelineBuilder.Build(state, Args);

        Assert.NotNull(result);
        var stages = result.Stages!.ToList();
        Assert.DoesNotContain(stages, s => s.Type == TranscodeStageType.HardwareUpload);
        Assert.DoesNotContain(stages, s => s.Type == TranscodeStageType.HardwareDownload);
    }

    [Fact]
    public void Build_SurfacesSourceVideoRangeAndBitDepth_OnDecodeAndEncodeStages()
    {
        var state = CreateVideoState("hevc", "hevc");
        // A 4K HEVC HDR10 10-bit source (HDR10 is derived from the SMPTE 2084 transfer).
        state.VideoStream!.BitDepth = 10;
        state.VideoStream!.ColorTransfer = "smpte2084";

        var result = TranscodingPipelineBuilder.Build(state, "-c:v hevc -i input.mkv -c:v hevc_qsv out.ts");

        Assert.NotNull(result);
        var stages = result.Stages!.ToList();

        // The decode stage reports the source format so the graph can show "10-bit HDR10".
        var decode = stages.Single(s => s.Type == TranscodeStageType.Decode);
        Assert.Equal(10, decode.VideoBitDepth);
        Assert.Equal(VideoRangeType.HDR10, decode.VideoRange);

        // With no requested bit-depth cap the encode keeps the source depth (ffmpeg preserves it).
        var encode = stages.Single(s => s.Type == TranscodeStageType.Encode);
        Assert.Equal(10, encode.VideoBitDepth);
    }

    [Fact]
    public void Build_InfersSdrOutputRange_WhenToneMapping()
    {
        var state = new EncodingJobInfo(TranscodingJobType.Progressive)
        {
            VideoStream = new MediaStream { Type = MediaStreamType.Video, Codec = "hevc", BitDepth = 10, ColorTransfer = "smpte2084" },
            OutputVideoCodec = "hevc",
            BaseRequest = new BaseEncodingJobOptions()
        };

        // HDR10 source tone mapped down to SDR via an OpenCL tone map filter.
        const string Args = "-hwaccel qsv -c:v hevc_qsv -i input.mkv "
            + "-vf \"vpp_qsv=w=640:h=360,tonemap_opencl=tonemap=hable:format=nv12\" -c:v hevc_qsv out.ts";

        var result = TranscodingPipelineBuilder.Build(state, Args);

        Assert.NotNull(result);
        var stages = result.Stages!.ToList();

        Assert.Equal(VideoRangeType.HDR10, stages.Single(s => s.Type == TranscodeStageType.Decode).VideoRange);
        Assert.Equal(VideoRangeType.SDR, stages.Single(s => s.Type == TranscodeStageType.Encode).VideoRange);
    }

    [Fact]
    public void Build_PreservesSourceOutputRange_WhenNotToneMapping()
    {
        var state = new EncodingJobInfo(TranscodingJobType.Progressive)
        {
            VideoStream = new MediaStream { Type = MediaStreamType.Video, Codec = "hevc", BitDepth = 10, ColorTransfer = "smpte2084" },
            OutputVideoCodec = "hevc",
            BaseRequest = new BaseEncodingJobOptions()
        };

        // A plain scale (no tone map) keeps the HDR10 range on the output.
        var result = TranscodingPipelineBuilder.Build(state, "-c:v hevc -i input.mkv -vf \"scale=1280:720\" -c:v hevc_qsv out.ts");

        Assert.NotNull(result);
        var encode = result.Stages!.Single(s => s.Type == TranscodeStageType.Encode);
        Assert.Equal(VideoRangeType.HDR10, encode.VideoRange);
    }

    [Fact]
    public void Build_ParsesSoftwarePipeline()
    {
        var state = CreateVideoState("h264", "h264");
        const string Args = "-c:v h264 -i input.mp4 -vf \"scale=trunc(min(max(iw,ih),1280)):-2\" -c:v libx264 out.ts";

        var result = TranscodingPipelineBuilder.Build(state, Args);

        Assert.NotNull(result);
        var stages = result.Stages!.ToList();

        Assert.Equal(HardwareFramework.Software, stages[0].Framework);
        Assert.False(stages[0].IsHardware);

        var encode = stages.Single(s => s.Type == TranscodeStageType.Encode);
        Assert.Equal("libx264", encode.Name);
        Assert.Equal(HardwareFramework.Software, encode.Framework);
        Assert.False(encode.IsHardware);
    }

    [Fact]
    public void Build_InfersDecodeFrameworkFromHwaccel_WhenNoExplicitDecoder()
    {
        var state = CreateVideoState("h264", "hevc");
        const string Args = "-hwaccel cuda -hwaccel_output_format cuda -i input.mp4 "
            + "-vf \"scale_cuda=w=1920:h=1080\" -c:v hevc_nvenc out.ts";

        var result = TranscodingPipelineBuilder.Build(state, Args);

        Assert.NotNull(result);
        var stages = result.Stages!.ToList();

        Assert.Equal(TranscodeStageType.Decode, stages[0].Type);
        Assert.Equal(HardwareFramework.Cuda, stages[0].Framework);

        var scale = stages.Single(s => s.Type == TranscodeStageType.Scale);
        Assert.Equal(HardwareFramework.Cuda, scale.Framework);

        var encode = stages.Single(s => s.Type == TranscodeStageType.Encode);
        Assert.Equal("hevc_nvenc", encode.Name);
        Assert.Equal(HardwareFramework.Cuda, encode.Framework);
    }

    [Theory]
    [InlineData("h264_qsv", HardwareFramework.Qsv)]
    [InlineData("hevc_nvenc", HardwareFramework.Cuda)]
    [InlineData("av1_vaapi", HardwareFramework.Vaapi)]
    [InlineData("hevc_amf", HardwareFramework.Amf)]
    [InlineData("hevc_videotoolbox", HardwareFramework.VideoToolbox)]
    [InlineData("h264_rkmpp", HardwareFramework.Rkmpp)]
    [InlineData("h264_v4l2m2m", HardwareFramework.V4l2m2m)]
    [InlineData("libx264", HardwareFramework.Software)]
    public void Build_MarksHardwareEncoders(string encoder, HardwareFramework expected)
    {
        var state = CreateVideoState("h264", "hevc");
        var result = TranscodingPipelineBuilder.Build(state, $"-i input.mkv -codec:v:0 {encoder} out.ts");

        Assert.NotNull(result);
        var encode = result.Stages!.Single(s => s.Type == TranscodeStageType.Encode);
        Assert.Equal(expected, encode.Framework);
        Assert.Equal(expected != HardwareFramework.Software, encode.IsHardware);
    }

    [Fact]
    public void Build_ParsesStreamIndexedCodecOption_VideoToolbox()
    {
        var state = CreateVideoState("hevc", "hevc");
        // Real Jellyfin commands use stream-indexed codec options (e.g. -codec:v:0).
        const string Args = "-init_hw_device videotoolbox=vt -hwaccel videotoolbox "
            + "-hwaccel_output_format videotoolbox_vld -i input.mkv "
            + "-filter_complex \"[0:0]scale_vt=w=1920:h=1080:format=nv12[main]\" "
            + "-codec:v:0 hevc_videotoolbox -tag:v:0 hvc1 out.ts";

        var result = TranscodingPipelineBuilder.Build(state, Args);

        Assert.NotNull(result);
        var stages = result.Stages!.ToList();

        Assert.Equal(TranscodeStageType.Decode, stages[0].Type);
        Assert.Equal(HardwareFramework.VideoToolbox, stages[0].Framework);

        var scale = stages.Single(s => s.Type == TranscodeStageType.Scale);
        Assert.Equal("scale_vt", scale.Name);
        Assert.Equal(HardwareFramework.VideoToolbox, scale.Framework);

        var encode = stages.Single(s => s.Type == TranscodeStageType.Encode);
        Assert.Equal("hevc_videotoolbox", encode.Name);
        Assert.Equal(HardwareFramework.VideoToolbox, encode.Framework);
    }

    [Fact]
    public void Build_DropsSubtitlePrepChain_MarksBurnIn_AndAppendsAudioChain()
    {
        var state = new EncodingJobInfo(TranscodingJobType.Progressive)
        {
            VideoStream = new MediaStream { Type = MediaStreamType.Video, Codec = "hevc" },
            OutputVideoCodec = "hevc",
            AudioStream = new MediaStream { Type = MediaStreamType.Audio, Codec = "dts" },
            OutputAudioCodec = "aac",
            SubtitleStream = new MediaStream { Type = MediaStreamType.Subtitle, Codec = "PGSSUB", Index = 4 }
        };

        // Graphical subtitle burn-in: a "[sub]" pre-processing chain feeds the overlay, plus an
        // audio transcode. The pre-processing scales must not pollute the video lane.
        const string Args = "-hwaccel videotoolbox -i input.mkv -map 0:0 -map 0:2 "
            + "-codec:v:0 hevc_videotoolbox "
            + "-filter_complex \"[0:4]scale,scale=-1:1636:fast_bilinear,crop,pad=max(3840\\,iw):max(1636\\,ih),format=bgra,hwupload[sub];"
            + "[0:0]scale_vt=format=nv12:color_matrix=bt709[main];[main][sub]overlay_videotoolbox=eof_action=pass\" "
            + "-codec:a:0 aac_at -ac 6 out.ts";

        var result = TranscodingPipelineBuilder.Build(state, Args);

        Assert.NotNull(result);
        var stages = result.Stages!.ToList();

        // No Scale stage at all: the subtitle-prep chain's scales belong to the burn-in tributary,
        // and the main scale_vt names no dimensions, so it only converts the format.
        Assert.DoesNotContain(stages, s => s.Type == TranscodeStageType.Scale);

        var subtitle = stages.Single(s => s.Type == TranscodeStageType.Subtitle);
        Assert.Equal("overlay_videotoolbox", subtitle.Name);
        Assert.Equal("Burn-in", subtitle.Detail);

        // Video chain (decode/scale/subtitle/encode) followed by the audio chain (decode/encode).
        var decodes = stages.Where(s => s.Type == TranscodeStageType.Decode).ToList();
        Assert.Equal(2, decodes.Count);
        Assert.Equal("hevc", decodes[0].Name);
        Assert.Equal(HardwareFramework.VideoToolbox, decodes[0].Framework);
        Assert.Equal("dts", decodes[1].Name);
        Assert.Equal(HardwareFramework.Software, decodes[1].Framework);

        var encodes = stages.Where(s => s.Type == TranscodeStageType.Encode).ToList();
        Assert.Equal(2, encodes.Count);
        Assert.Equal("hevc_videotoolbox", encodes[0].Name);
        Assert.Equal("aac_at", encodes[1].Name);
    }

    [Fact]
    public void BuildGraphProbeArguments_RedirectsOutputDir_AddsPrintGraphsAndExitsImmediately()
    {
        const string Args = "-hwaccel videotoolbox -i file:\"/movies/x.mkv\" -map 0:0 "
            + "-codec:v:0 hevc_videotoolbox -filter_complex \"[0:0]scale_vt[main]\" "
            + "-hls_segment_filename \"/cache/transcodes/HASH%d.mp4\" "
            + "-y \"/cache/transcodes/HASH.m3u8\"";

        var probe = TranscodingPipelineBuilder.BuildGraphProbeArguments(
            Args,
            "/cache/transcodes/HASH.m3u8",
            "/cache/transcodes/graphprobe-1",
            "/cache/transcodes/graphprobe-1/graph.json");

        Assert.NotNull(probe);

        // Graph dump is requested up front.
        Assert.Contains("-print_graphs -print_graphs_format json -print_graphs_file \"/cache/transcodes/graphprobe-1/graph.json\"", probe, StringComparison.Ordinal);
        // ffmpeg is told to exit right after graph init.
        Assert.Contains("-t 0 \"/cache/transcodes/graphprobe-1/HASH.m3u8\"", probe, StringComparison.Ordinal);
        // All transcode-dir outputs are redirected into the throwaway probe dir...
        Assert.Contains("\"/cache/transcodes/graphprobe-1/HASH%d.mp4\"", probe, StringComparison.Ordinal);
        // ...while the input (outside that dir) is untouched.
        Assert.Contains("-i file:\"/movies/x.mkv\"", probe, StringComparison.Ordinal);
        // The original transcode output must not survive un-redirected.
        Assert.DoesNotContain("\"/cache/transcodes/HASH.m3u8\"", probe, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildGraphProbeArguments_WindowsPaths_RedirectsOutputDir()
    {
        // The probe paths are rebuilt from the command line's own separators, so a command line
        // written with backslashes must be redirected just as one written with forward slashes -
        // and neither may depend on the separator of the host the test runs on.
        const string Args = "-i file:\"C:\\movies\\x.mkv\" -map 0:0 -codec:v:0 hevc_qsv "
            + "-hls_segment_filename \"C:\\cache\\transcodes\\HASH%d.mp4\" "
            + "-y \"C:\\cache\\transcodes\\HASH.m3u8\"";

        var probe = TranscodingPipelineBuilder.BuildGraphProbeArguments(
            Args,
            "C:\\cache\\transcodes\\HASH.m3u8",
            "C:\\cache\\transcodes\\graphprobe-1",
            "C:\\cache\\transcodes\\graphprobe-1\\graph.json");

        Assert.NotNull(probe);
        Assert.Contains("-t 0 \"C:\\cache\\transcodes\\graphprobe-1\\HASH.m3u8\"", probe, StringComparison.Ordinal);
        Assert.Contains("\"C:\\cache\\transcodes\\graphprobe-1\\HASH%d.mp4\"", probe, StringComparison.Ordinal);
        Assert.Contains("-i file:\"C:\\movies\\x.mkv\"", probe, StringComparison.Ordinal);
        Assert.DoesNotContain("\"C:\\cache\\transcodes\\HASH.m3u8\"", probe, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_UsesGraphJson_DecodersEncodersAndFilters_DroppingSubtitlePrep()
    {
        var state = new EncodingJobInfo(TranscodingJobType.Progressive)
        {
            VideoStream = new MediaStream { Type = MediaStreamType.Video, Codec = "hevc" },
            OutputVideoCodec = "hevc",
            AudioStream = new MediaStream { Type = MediaStreamType.Audio, Codec = "dts" },
            OutputAudioCodec = "aac"
        };
        const string Args = "-hwaccel videotoolbox -i input.mkv -codec:v:0 hevc_videotoolbox -codec:a:0 aac_at out.ts";

        // Real ffmpeg 8 -print_graphs shape: top-level decoders/encoders + graphs[].filters, where
        // each filter's own id ("filter_id") lives inside its input/output pad entries, linked via
        // source_filter_id/dest_filter_id. The "scale" on the subtitle branch (fed by "sdec") feeds
        // the overlay's 2nd input and must be excluded as it is not forward-reachable from "vdec".
        const string GraphJson = @"{
            ""graphs"": [{ ""filters"": [
                { ""filter_name"": ""scale_vt"", ""filter_inputs"": [{""source_filter_id"": ""vdec"", ""filter_id"": ""sc"", ""format"": ""p010le"", ""width"": 3840, ""height"": 1636}], ""filter_outputs"": [{""dest_filter_id"": ""ov"", ""filter_id"": ""sc"", ""format"": ""nv12"", ""width"": 1920, ""height"": 1080}] },
                { ""filter_name"": ""scale"", ""filter_inputs"": [{""source_filter_id"": ""sdec"", ""filter_id"": ""subsc""}], ""filter_outputs"": [{""dest_filter_id"": ""ov"", ""filter_id"": ""subsc"", ""width"": 1920, ""height"": 1080}] },
                { ""filter_name"": ""overlay_videotoolbox"", ""filter_inputs"": [{""source_filter_id"": ""sc"", ""filter_id"": ""ov""}, {""source_filter_id"": ""subsc"", ""filter_id"": ""ov""}], ""filter_outputs"": [{""dest_filter_id"": ""out_v"", ""filter_id"": ""ov"", ""format"": ""nv12"", ""width"": 1920, ""height"": 1080}] }
            ]}],
            ""decoders"": [{ ""id"": ""vdec"", ""name"": ""hevc"", ""media_type"": ""video"" }, { ""id"": ""adec"", ""name"": ""dts"", ""media_type"": ""audio"" }],
            ""encoders"": [{ ""id"": ""out_v"", ""name"": ""hevc_videotoolbox"", ""media_type"": ""video"" }, { ""name"": ""aac_at"", ""media_type"": ""audio"" }]
        }";

        var result = TranscodingPipelineBuilder.Build(state, Args, GraphJson);

        Assert.NotNull(result);
        var stages = result.Stages!.ToList();

        // Video decode framework comes from -hwaccel since the decoder name ("hevc") has no suffix.
        var decode = stages.First(s => s.Type == TranscodeStageType.Decode);
        Assert.Equal("hevc", decode.Name);
        Assert.Equal(HardwareFramework.VideoToolbox, decode.Framework);
        // Edge leaving the decoder = the first filter's input pad (format + resolution).
        Assert.Equal("p010le 3840x1636", decode.EdgeLabel);

        // Only the main-path scale survives; the subtitle-prep "scale" is dropped.
        var scales = stages.Where(s => s.Type == TranscodeStageType.Scale).ToList();
        Assert.Single(scales);
        Assert.Equal("scale_vt", scales[0].Name);
        Assert.Equal("1920x1080", scales[0].Detail);
        Assert.Equal("nv12 1920x1080", scales[0].EdgeLabel);

        var subtitle = stages.Single(s => s.Type == TranscodeStageType.Subtitle);
        Assert.Equal("overlay_videotoolbox", subtitle.Name);
        Assert.Equal("Burn-in", subtitle.Detail);

        var encodes = stages.Where(s => s.Type == TranscodeStageType.Encode).ToList();
        Assert.Equal("hevc_videotoolbox", encodes[0].Name);
        Assert.Equal(HardwareFramework.VideoToolbox, encodes[0].Framework);
        Assert.Equal("aac_at", encodes[1].Name);

        // Audio chain present from the graph's audio decoder/encoder.
        var audioDecode = stages.Where(s => s.Type == TranscodeStageType.Decode).ToList()[1];
        Assert.Equal("dts", audioDecode.Name);
    }

    [Fact]
    public void Build_FallsBackToCommandLine_WhenGraphJsonIsInvalid()
    {
        var state = CreateVideoState("hevc", "h264");
        const string Args = "-i input.mkv -vf \"scale_vt=w=640:h=360\" -codec:v:0 h264_videotoolbox out.ts";

        var result = TranscodingPipelineBuilder.Build(state, Args, "not valid json");

        Assert.NotNull(result);
        var scale = result.Stages!.Single(s => s.Type == TranscodeStageType.Scale);
        Assert.Equal("scale_vt", scale.Name);
    }

    [Fact]
    public void Build_ReturnsAudioChain_WhenVideoCopiedAndAudioTranscoded()
    {
        // Video is direct-streamed (copy), only the audio is transcoded to AAC.
        var state = new EncodingJobInfo(TranscodingJobType.Progressive)
        {
            VideoStream = new MediaStream { Type = MediaStreamType.Video, Codec = "hevc" },
            OutputVideoCodec = "copy",
            AudioStream = new MediaStream { Type = MediaStreamType.Audio, Codec = "dts" },
            OutputAudioCodec = "aac"
        };
        const string Args = "-i input.mkv -map 0:0 -map 0:1 -codec:v:0 copy -codec:a:0 aac_at -ac 6 out.ts";

        var result = TranscodingPipelineBuilder.Build(state, Args);

        Assert.NotNull(result);
        var stages = result.Stages!.ToList();

        var decode = stages.Single(s => s.Type == TranscodeStageType.Decode);
        Assert.Equal("dts", decode.Name);
        Assert.Equal("DTS", decode.Detail);
        Assert.Equal(HardwareFramework.Software, decode.Framework);

        // aac_at is Apple AudioToolbox, which is hardware accelerated.
        var encode = stages.Single(s => s.Type == TranscodeStageType.Encode);
        Assert.Equal("aac_at", encode.Name);
        Assert.Equal("AAC", encode.Detail);
        Assert.Equal(HardwareFramework.AudioToolbox, encode.Framework);
        Assert.True(encode.IsHardware);
    }
}
