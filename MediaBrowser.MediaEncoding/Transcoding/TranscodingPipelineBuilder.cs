using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Session;

namespace MediaBrowser.MediaEncoding.Transcoding;

/// <summary>
/// Builds a <see cref="TranscodingPipelineInfo"/> from the ffmpeg command line that was generated
/// for a transcode. The ffmpeg arguments are the most faithful description of the real pipeline:
/// every decoder, filter and encoder is present and its name encodes the hardware framework it
/// runs on (for example <c>_qsv</c>, <c>_vaapi</c>, <c>_opencl</c>).
/// </summary>
public static partial class TranscodingPipelineBuilder
{
    private static readonly char[] _pathSeparators = ['/', '\\'];

    /// <summary>
    /// Builds the pipeline for a running ffmpeg job. Transcoded streams get their decode, filter and
    /// encode stages; streams that are only rewrapped get a single passthrough stage, so a remux is
    /// described rather than reported as nothing.
    /// </summary>
    /// <param name="state">The encoding job.</param>
    /// <param name="commandLineArguments">The full ffmpeg command line arguments.</param>
    /// <param name="filterGraphJson">Optional ffmpeg <c>-print_graphs</c> JSON output. When
    /// supplied and parseable, its filters are used in place of the command-line-derived filters.</param>
    /// <returns>The pipeline, or <c>null</c> when there is no stream to describe.</returns>
    public static TranscodingPipelineInfo? Build(EncodingJobInfo state, string? commandLineArguments, string? filterGraphJson = null)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (string.IsNullOrWhiteSpace(commandLineArguments))
        {
            return null;
        }

        var tokens = Tokenize(commandLineArguments);
        if (tokens.Count == 0)
        {
            return null;
        }

        var firstInput = tokens.FindIndex(t => string.Equals(t, "-i", StringComparison.Ordinal));
        var lastInput = tokens.FindLastIndex(t => string.Equals(t, "-i", StringComparison.Ordinal));
        var deviceFramework = GetDeviceFramework(tokens, firstInput);
        var negotiatedBitDepth = GetFilterChainBitDepth(tokens, lastInput);
        var negotiatedRange = GetFilterChainVideoRange(tokens, lastInput);

        var videoTranscoded = state.VideoStream is not null && !EncodingHelper.IsCopyCodec(state.OutputVideoCodec);
        var audioTranscoded = state.AudioStream is not null && !EncodingHelper.IsCopyCodec(state.OutputAudioCodec);

        // Prefer the authoritative ffmpeg -print_graphs dump when available: it lists the actual
        // decoders, encoders and the negotiated filter graph (with per-filter output dimensions).
        if (!string.IsNullOrWhiteSpace(filterGraphJson))
        {
            try
            {
                var graphStages = BuildFromGraph(filterGraphJson, state, tokens, firstInput, deviceFramework, GetDeclaredFilterArgs(tokens, lastInput), negotiatedBitDepth, negotiatedRange);

                // Only trust the dump when it actually describes every chain we expect. A schema
                // change or a truncated write would otherwise silently drop a whole lane, and
                // returning it as a success would suppress the command line fallback below.
                var graphIsComplete = graphStages is { Count: > 0 }
                    && (!videoTranscoded || graphStages.Any(s => s.Type == TranscodeStageType.Encode && s.MediaType == "Video"))
                    && (!audioTranscoded || graphStages.Any(s => s.Type == TranscodeStageType.Encode && s.MediaType == "Audio"));

                if (graphIsComplete)
                {
                    return new TranscodingPipelineInfo { Stages = graphStages };
                }
            }
            catch (JsonException)
            {
                // Fall back to the command-line parser below.
            }
        }

        var stages = new List<TranscodingPipelineStage>();

        if (videoTranscoded)
        {
            AddDecodeStage(stages, tokens, firstInput, state);
            AddFilterStages(stages, tokens, lastInput, state, deviceFramework);
            AddEncodeStage(stages, tokens, lastInput, state, negotiatedBitDepth, negotiatedRange);
        }
        else if (state.VideoStream is not null)
        {
            // Video is passed through: ffmpeg is only rewrapping the bitstream.
            AddCopyStage(stages, state.VideoStream);
        }

        // Everything added so far is the video chain.
        TagMediaType(stages, "Video");

        // Audio may be transcoded alongside (or instead of) video. Append its decode -> encode
        // chain so the pipeline reflects every stream that is actually being transcoded.
        AddAudioStages(stages, tokens, lastInput, state);

        if (stages.Count == 0)
        {
            return null;
        }

        return new TranscodingPipelineInfo
        {
            Stages = stages
        };
    }

    /// <summary>
    /// Derives a short-lived "graph probe" command from a transcode command line. ffmpeg's
    /// <c>-print_graphs</c> only flushes the filter graph when ffmpeg exits, so the long-running
    /// transcode cannot be used. Instead this builds an equivalent command that initializes the
    /// graph and exits immediately (<c>-t 0</c>).
    /// </summary>
    /// <param name="commandLineArguments">The real transcode command line.</param>
    /// <param name="outputPath">The real transcode output path (its directory is redirected).</param>
    /// <param name="probeDirectory">A dedicated throwaway directory for the probe's output.</param>
    /// <param name="graphFilePath">The path the graph JSON should be written to.</param>
    /// <returns>The probe command line, or <c>null</c> if it cannot be derived safely.</returns>
    public static string? BuildGraphProbeArguments(string commandLineArguments, string outputPath, string probeDirectory, string graphFilePath)
    {
        if (string.IsNullOrWhiteSpace(commandLineArguments) || string.IsNullOrEmpty(outputPath))
        {
            return null;
        }

        var separatorIndex = outputPath.LastIndexOfAny(_pathSeparators);
        if (separatorIndex <= 0)
        {
            return null;
        }

        var outputDirectory = outputPath[..separatorIndex];
        var probeCommand = commandLineArguments.Replace(outputDirectory, probeDirectory, StringComparison.Ordinal);
        var probeOutput = probeDirectory + outputPath[separatorIndex..];
        var outputIndex = probeCommand.LastIndexOf(probeOutput, StringComparison.Ordinal);
        if (outputIndex < 0)
        {
            return null;
        }

        var insertIndex = outputIndex > 0 && probeCommand[outputIndex - 1] == '"' ? outputIndex - 1 : outputIndex;
        probeCommand = probeCommand.Insert(insertIndex, "-t 0 ");

        return $"-print_graphs -print_graphs_format json -print_graphs_file \"{graphFilePath}\" {probeCommand}";
    }

    private static void AddCopyStage(List<TranscodingPipelineStage> stages, MediaStream stream)
    {
        var codec = stream.Codec;
        if (string.IsNullOrEmpty(codec))
        {
            return;
        }

        var isVideo = stream.Type == MediaStreamType.Video;

        stages.Add(new TranscodingPipelineStage
        {
            Type = TranscodeStageType.Copy,
            Framework = HardwareFramework.Software,
            Name = codec,
            // A passthrough keeps every extension of the source format, so the detail is the full
            // one (DTS-HD MA, Dolby TrueHD Atmos) rather than the base codec.
            Detail = isVideo ? GetCodecDisplayName(codec) : GetAudioCodecDisplayName(stream),
            IsHardware = false,
            MediaType = isVideo ? "Video" : "Audio",
            VideoBitDepth = isVideo ? stream.BitDepth : null,
            VideoRange = isVideo ? stream.VideoRangeType : null,
            VideoDoViTitle = isVideo ? stream.VideoDoViTitle : null
        });
    }

    /// <summary>
    /// The display name of a source audio stream, including the format extensions that the base
    /// codec name hides: the DTS family all report a codec of <c>dts</c>/<c>dca</c>, and Dolby Atmos
    /// rides inside TrueHD or E-AC-3. Both are only distinguishable from the stream's profile.
    /// </summary>
    private static string? GetAudioCodecDisplayName(MediaStream stream)
    {
        var codec = stream.Codec;
        if (string.IsNullOrEmpty(codec))
        {
            return null;
        }

        var name = GetCodecDisplayName(codec);
        var profile = stream.Profile ?? string.Empty;

        if (string.Equals(codec, "dts", StringComparison.OrdinalIgnoreCase)
            || string.Equals(codec, "dca", StringComparison.OrdinalIgnoreCase))
        {
            // ffmpeg profiles: "DTS", "DTS-ES", "DTS 96/24", "DTS-HD HRA", "DTS-HD MA",
            // "DTS-HD MA + DTS:X", "DTS-HD MA + DTS:X IMAX", "DTS Express".
            name = stream.AudioSpatialFormat == AudioSpatialFormat.DTSX || profile.Contains("DTS:X", StringComparison.OrdinalIgnoreCase)
                ? "DTS:X"
                : profile switch
                {
                    _ when profile.Contains("HD MA", StringComparison.OrdinalIgnoreCase) => "DTS-HD MA",
                    _ when profile.Contains("HD HRA", StringComparison.OrdinalIgnoreCase) => "DTS-HD HRA",
                    _ when profile.Contains("Express", StringComparison.OrdinalIgnoreCase) => "DTS Express",
                    _ when profile.Contains("96/24", StringComparison.Ordinal) => "DTS 96/24",
                    _ when profile.Contains("DTS-ES", StringComparison.OrdinalIgnoreCase) => "DTS-ES",
                    _ => name
                };
        }

        // Atmos sits on top of TrueHD or E-AC-3 and is reported through the profile only.
        if (stream.AudioSpatialFormat == AudioSpatialFormat.DolbyAtmos
            && name is not null
            && !name.Contains("Atmos", StringComparison.OrdinalIgnoreCase))
        {
            // Insert before any codec parenthetical so it reads "Dolby Digital Plus Atmos (E-AC-3)"
            // rather than "Dolby Digital Plus (E-AC-3) Atmos".
            var paren = name.IndexOf(" (", StringComparison.Ordinal);
            name = paren < 0 ? name + " Atmos" : name.Insert(paren, " Atmos");
        }

        return name;
    }

    private static void AddAudioStages(List<TranscodingPipelineStage> stages, IReadOnlyList<string> tokens, int lastInput, EncodingJobInfo state)
    {
        if (state.AudioStream is null)
        {
            return;
        }

        if (EncodingHelper.IsCopyCodec(state.OutputAudioCodec))
        {
            AddCopyStage(stages, state.AudioStream);
            return;
        }

        // Audio decode is implicit (ffmpeg picks the decoder), so name the decode stage after the
        // source codec. The framework is derived from the name.
        var sourceCodec = state.AudioStream.Codec;
        if (!string.IsNullOrEmpty(sourceCodec))
        {
            stages.Add(MakeAudioStage(TranscodeStageType.Decode, sourceCodec, GetAudioCodecDisplayName(state.AudioStream)));
        }

        var start = lastInput < 0 ? 0 : lastInput;
        var encoder = FindAudioCodecValue(tokens, start, tokens.Count);
        if (!string.IsNullOrEmpty(encoder) && !EncodingHelper.IsCopyCodec(encoder))
        {
            stages.Add(MakeAudioStage(TranscodeStageType.Encode, encoder, GetCodecDisplayName(state.ActualOutputAudioCodec)));
        }
    }

    private static TranscodingPipelineStage MakeAudioStage(TranscodeStageType type, string name, string? detail)
    {
        var framework = FrameworkFromName(name);
        return new TranscodingPipelineStage
        {
            Type = type,
            Framework = framework,
            Name = name,
            Detail = detail,
            IsHardware = framework != HardwareFramework.Software,
            MediaType = "Audio"
        };
    }

    private static void TagMediaType(List<TranscodingPipelineStage> stages, string mediaType)
    {
        foreach (var stage in stages)
        {
            stage.MediaType ??= mediaType;
        }
    }

    private static void AddDecodeStage(List<TranscodingPipelineStage> stages, IReadOnlyList<string> tokens, int firstInput, EncodingJobInfo state)
    {
        // An explicit input decoder is given as "-c:v <decoder>" before the first "-i".
        var inputEnd = firstInput < 0 ? tokens.Count : firstInput;
        var decoder = FindCodecValue(tokens, 0, inputEnd);
        var hwaccel = FindOptionValue(tokens, 0, inputEnd, "-hwaccel");

        var sourceCodec = state.VideoStream?.Codec;
        var name = decoder ?? sourceCodec;
        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        // When there is no explicit decoder the framework is inferred from -hwaccel, otherwise from the decoder name suffix.
        var framework = decoder is null
            ? FrameworkFromHwaccel(hwaccel)
            : FrameworkFromName(decoder);

        stages.Add(new TranscodingPipelineStage
        {
            Type = TranscodeStageType.Decode,
            Framework = framework,
            Name = name,
            Detail = GetCodecDisplayName(sourceCodec),
            IsHardware = framework != HardwareFramework.Software,
            VideoBitDepth = state.VideoStream?.BitDepth,
            VideoRange = state.VideoStream?.VideoRangeType,
            VideoDoViTitle = state.VideoStream?.VideoDoViTitle
        });
    }

    private static string? FindFilterGraph(IReadOnlyList<string> tokens, int lastInput)
    {
        var start = lastInput < 0 ? 0 : lastInput;
        return FindOptionValue(tokens, start, tokens.Count, "-vf")
            ?? FindOptionValue(tokens, start, tokens.Count, "-filter:v")
            ?? FindOptionValue(tokens, start, tokens.Count, "-filter_complex");
    }

    // ffmpeg's -print_graphs dump names each filter but does not repeat the arguments it was
    // configured with, and some filters can only be classified from those (libplacebo and vpp_qsv
    // both scale and tone map). This recovers them from the command line, keyed by filter name.
    private static Dictionary<string, string> GetDeclaredFilterArgs(IReadOnlyList<string> tokens, int lastInput)
    {
        var declared = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var filterGraph = FindFilterGraph(tokens, lastInput);
        if (string.IsNullOrWhiteSpace(filterGraph))
        {
            return declared;
        }

        foreach (var chain in SplitGraph(filterGraph, ';'))
        {
            foreach (var rawFilter in SplitGraph(StreamLabelRegex().Replace(chain, string.Empty), ','))
            {
                var filter = rawFilter.Trim();
                var eq = filter.IndexOf('=', StringComparison.Ordinal);
                if (eq <= 0)
                {
                    continue;
                }

                // First occurrence wins; a repeated filter name (two scales) is indistinguishable
                // here anyway and the leading one is the main-path instance.
                declared.TryAdd(filter[..eq].Trim(), filter[(eq + 1)..]);
            }
        }

        return declared;
    }

    private static void AddFilterStages(List<TranscodingPipelineStage> stages, IReadOnlyList<string> tokens, int lastInput, EncodingJobInfo state, HardwareFramework deviceFramework)
    {
        var filterGraph = FindFilterGraph(tokens, lastInput);
        if (string.IsNullOrWhiteSpace(filterGraph))
        {
            return;
        }

        // A -filter_complex is split into sub-chains by ';'. The graphical-subtitle pre-processing
        // chain (scale/pad/crop/hwupload of the subtitle image) outputs a "[sub]" pad that is later
        // consumed by the overlay. That chain is plumbing for the burn-in, not part of the main
        // video processing path, so skip it - the overlay stage alone denotes the burn-in.
        foreach (var rawChain in SplitGraph(filterGraph, ';'))
        {
            if (IsSubtitlePrepChain(rawChain))
            {
                continue;
            }

            var cleaned = StreamLabelRegex().Replace(rawChain, string.Empty);
            foreach (var rawFilter in SplitGraph(cleaned, ','))
            {
                var filter = rawFilter.Trim();
                if (filter.Length == 0)
                {
                    continue;
                }

                var eq = filter.IndexOf('=', StringComparison.Ordinal);
                var filterName = (eq < 0 ? filter : filter[..eq]).Trim();
                var filterArgs = eq < 0 ? string.Empty : filter[(eq + 1)..];

                stages.AddRange(ClassifyFilter(filterName, filterArgs, state, deviceFramework));
            }
        }
    }

    // Splits a filter graph on a separator, honouring ffmpeg's escaping rules. Filter arguments
    // routinely contain literal separators - EncodingHelper emits
    // "scale=trunc(min(max(iw\,ih*dar)\,...))" with backslash-escaped commas, and options such as
    // "alphasrc=start='0'" or "subtitles=f='/path/a,b.ass'" quote their values. A plain string
    // split tears those single filters into fragments that then parse as bogus filter names.
    private static List<string> SplitGraph(string graph, char separator)
    {
        var parts = new List<string>();
        var current = new System.Text.StringBuilder();
        var escaped = false;
        var quoted = false;

        foreach (var c in graph)
        {
            if (escaped)
            {
                current.Append(c);
                escaped = false;
                continue;
            }

            if (c == '\\')
            {
                escaped = true;
                current.Append(c);
            }
            else if (c == '\'')
            {
                quoted = !quoted;
                current.Append(c);
            }
            else if (c == separator && !quoted)
            {
                if (current.Length > 0)
                {
                    parts.Add(current.ToString());
                    current.Clear();
                }
            }
            else
            {
                current.Append(c);
            }
        }

        if (current.Length > 0)
        {
            parts.Add(current.ToString());
        }

        return parts;
    }

    // A subtitle pre-processing chain is identified by its trailing output pad label ("[sub]").
    private static bool IsSubtitlePrepChain(string chain)
    {
        var match = TrailingLabelRegex().Match(chain.Trim());
        return match.Success && match.Groups[1].Value.Equals("sub", StringComparison.OrdinalIgnoreCase);
    }

    // Builds the full pipeline from the ffmpeg 8.0+ -print_graphs JSON dump, which lists the actual
    // decoders, encoders and the negotiated filter graph. Decoders/encoders are taken from the
    // top-level "decoders"/"encoders" arrays; filters from "graphs[].filters". To keep the video
    // lane clean we only include filters reachable forward from the video decoder, which naturally
    // drops subtitle-prep tributaries that merely feed an overlay's secondary input.
    private static List<TranscodingPipelineStage>? BuildFromGraph(
        string filterGraphJson,
        EncodingJobInfo state,
        IReadOnlyList<string> tokens,
        int firstInput,
        HardwareFramework deviceFramework,
        Dictionary<string, string> declaredFilterArgs,
        int? negotiatedBitDepth,
        VideoRangeType? negotiatedRange)
    {
        using var document = JsonDocument.Parse(filterGraphJson);
        var root = document.RootElement;

        // Index filters by id and keep graph order (which mirrors chain order).
        var filtersById = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        var orderedFilters = new List<JsonElement>();
        if (root.TryGetProperty("graphs", out var graphs) && graphs.ValueKind == JsonValueKind.Array)
        {
            foreach (var graph in graphs.EnumerateArray())
            {
                if (graph.TryGetProperty("filters", out var filterList) && filterList.ValueKind == JsonValueKind.Array)
                {
                    foreach (var filter in filterList.EnumerateArray())
                    {
                        orderedFilters.Add(filter);
                        var id = GetFilterId(filter);
                        if (!string.IsNullOrEmpty(id))
                        {
                            filtersById[id] = filter;
                        }
                    }
                }
            }
        }

        var stages = new List<TranscodingPipelineStage>();

        // Video chain: decode -> filters -> encode.
        var (videoDecoder, videoDecoderId) = FindCodec(root, "decoders", "video");
        var (videoEncoder, _) = FindCodec(root, "encoders", "video");
        if (!string.IsNullOrEmpty(videoEncoder))
        {
            if (!string.IsNullOrEmpty(videoDecoder))
            {
                // The decoder name rarely encodes the hwaccel, so fall back to the -hwaccel flag.
                var framework = FrameworkFromName(videoDecoder);
                if (framework == HardwareFramework.Software)
                {
                    var inputEnd = firstInput < 0 ? tokens.Count : firstInput;
                    framework = FrameworkFromHwaccel(FindOptionValue(tokens, 0, inputEnd, "-hwaccel"));
                }

                stages.Add(new TranscodingPipelineStage
                {
                    Type = TranscodeStageType.Decode,
                    Framework = framework,
                    Name = videoDecoder,
                    Detail = GetCodecDisplayName(videoDecoder),
                    IsHardware = framework != HardwareFramework.Software,
                    // The data leaving the decoder is the first main-path filter's input pad.
                    EdgeLabel = GetSeedInputLabel(orderedFilters, videoDecoderId),
                    VideoBitDepth = state.VideoStream?.BitDepth,
                    VideoRange = state.VideoStream?.VideoRangeType,
                    VideoDoViTitle = state.VideoStream?.VideoDoViTitle
                });
            }

            var mainPath = GetForwardReachableFilters(filtersById, orderedFilters, videoDecoderId);
            foreach (var filter in orderedFilters)
            {
                var id = GetFilterId(filter);
                if (id is null || !mainPath.Contains(id))
                {
                    continue;
                }

                var name = GetStringProperty(filter, "filter_name");
                if (string.IsNullOrEmpty(name))
                {
                    continue;
                }

                var (width, height) = GetFilterOutputSize(filter);
                var args = width > 0 && height > 0
                    ? string.Format(CultureInfo.InvariantCulture, "w={0}:h={1}", width, height)
                    : string.Empty;

                // Append the arguments the filter was declared with, so options the dump omits
                // (libplacebo's "tonemapping=", vpp_qsv's "tonemap=") still classify. The
                // negotiated size stays in front so it wins over the requested one.
                if (declaredFilterArgs.TryGetValue(name, out var declared) && !string.IsNullOrEmpty(declared))
                {
                    args = args.Length == 0 ? declared : args + ":" + declared;
                }

                var filterStages = ClassifyFilter(name, args, state, deviceFramework);
                if (filterStages.Count > 0)
                {
                    // The connector after the filter carries its output pad's format/resolution, so
                    // it belongs on the last stage a dual purpose filter produced.
                    filterStages[^1].EdgeLabel = GetPadLabel(GetFirstPad(filter, "filter_outputs"));
                    stages.AddRange(filterStages);
                }
            }

            var encoderFramework = FrameworkFromName(videoEncoder);
            stages.Add(new TranscodingPipelineStage
            {
                Type = TranscodeStageType.Encode,
                Framework = encoderFramework,
                Name = videoEncoder,
                Detail = GetCodecDisplayName(state.ActualOutputVideoCodec) ?? GetCodecDisplayName(videoEncoder),
                IsHardware = encoderFramework != HardwareFramework.Software,
                VideoBitDepth = GetTargetVideoBitDepth(state, negotiatedBitDepth),
                VideoRange = GetTargetVideoRange(state, stages, negotiatedRange)
            });
        }

        // Everything added so far is the video chain; the audio stages below tag themselves.
        TagMediaType(stages, "Video");

        // Audio chain: decode -> encode. The framework comes from the codec name.
        var (audioDecoder, _) = FindCodec(root, "decoders", "audio");
        var (audioEncoder, _) = FindCodec(root, "encoders", "audio");
        if (!string.IsNullOrEmpty(audioEncoder))
        {
            if (!string.IsNullOrEmpty(audioDecoder))
            {
                // The source detail comes from the stream, not the decoder name: only the stream's
                // profile distinguishes DTS-HD MA / DTS:X and Atmos from their base codec.
                var sourceDetail = state.AudioStream is null
                    ? GetCodecDisplayName(audioDecoder)
                    : GetAudioCodecDisplayName(state.AudioStream);

                stages.Add(MakeAudioStage(TranscodeStageType.Decode, audioDecoder, sourceDetail));
            }

            stages.Add(MakeAudioStage(TranscodeStageType.Encode, audioEncoder, GetCodecDisplayName(state.ActualOutputAudioCodec) ?? GetCodecDisplayName(audioEncoder)));
        }

        return stages.Count > 0 ? stages : null;
    }

    // Finds the first decoder/encoder of the given media type, returning its (name, id).
    private static (string? Name, string? Id) FindCodec(JsonElement root, string arrayName, string mediaType)
    {
        if (root.TryGetProperty(arrayName, out var array) && array.ValueKind == JsonValueKind.Array)
        {
            foreach (var entry in array.EnumerateArray())
            {
                if (string.Equals(GetStringProperty(entry, "media_type"), mediaType, StringComparison.OrdinalIgnoreCase))
                {
                    return (GetStringProperty(entry, "name"), GetStringProperty(entry, "id"));
                }
            }
        }

        return (null, null);
    }

    // Collects every filter id reachable by following output links forward from the given source
    // (a decoder id). Filters on a secondary input branch (e.g. subtitle pre-processing feeding an
    // overlay) are not forward-reachable from the video decoder and are therefore excluded.
    private static HashSet<string> GetForwardReachableFilters(
        IReadOnlyDictionary<string, JsonElement> filtersById,
        IReadOnlyList<JsonElement> orderedFilters,
        string? sourceId)
    {
        var reachable = new HashSet<string>(StringComparer.Ordinal);
        if (string.IsNullOrEmpty(sourceId))
        {
            return reachable;
        }

        var queue = new Queue<string>();
        // Seed with filters whose input is the decoder itself.
        foreach (var filter in orderedFilters)
        {
            if (FilterHasInputSource(filter, sourceId))
            {
                var id = GetFilterId(filter);
                if (id is not null && reachable.Add(id))
                {
                    queue.Enqueue(id);
                }
            }
        }

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!filtersById.TryGetValue(current, out var filter)
                || !filter.TryGetProperty("filter_outputs", out var outputs)
                || outputs.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var output in outputs.EnumerateArray())
            {
                var dest = GetStringProperty(output, "dest_filter_id");
                if (dest is not null && filtersById.ContainsKey(dest) && reachable.Add(dest))
                {
                    queue.Enqueue(dest);
                }
            }
        }

        return reachable;
    }

    private static bool FilterHasInputSource(JsonElement filter, string sourceId)
    {
        if (filter.TryGetProperty("filter_inputs", out var inputs) && inputs.ValueKind == JsonValueKind.Array)
        {
            foreach (var input in inputs.EnumerateArray())
            {
                if (string.Equals(GetStringProperty(input, "source_filter_id"), sourceId, StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static (int Width, int Height) GetFilterOutputSize(JsonElement filter)
    {
        if (filter.TryGetProperty("filter_outputs", out var outputs)
            && outputs.ValueKind == JsonValueKind.Array
            && outputs.GetArrayLength() > 0)
        {
            var first = outputs[0];
            if (first.TryGetProperty("width", out var w) && first.TryGetProperty("height", out var h)
                && w.TryGetInt32(out var width) && h.TryGetInt32(out var height))
            {
                return (width, height);
            }
        }

        return (0, 0);
    }

    // The edge leaving the decoder is the input pad of the first main-path filter.
    private static string? GetSeedInputLabel(IReadOnlyList<JsonElement> orderedFilters, string? decoderId)
    {
        if (string.IsNullOrEmpty(decoderId))
        {
            return null;
        }

        foreach (var filter in orderedFilters)
        {
            if (FilterHasInputSource(filter, decoderId))
            {
                return GetPadLabel(GetFirstPad(filter, "filter_inputs"));
            }
        }

        return null;
    }

    private static JsonElement? GetFirstPad(JsonElement filter, string padArray)
    {
        if (filter.TryGetProperty(padArray, out var pads)
            && pads.ValueKind == JsonValueKind.Array
            && pads.GetArrayLength() > 0)
        {
            return pads[0];
        }

        return null;
    }

    // Formats a pad as "<format> <width>x<height>" (omitting any missing part) - the per-edge label shown on the connectors, mirroring ffmpeg's mermaid graph.
    private static string? GetPadLabel(JsonElement? pad)
    {
        if (pad is not { } element)
        {
            return null;
        }

        var format = GetStringProperty(element, "format");
        // Hardware frames are reported as "<hw_surface> | <sw_format>" (e.g. "videotoolbox_vld | p010le"); keep only the meaningful pixel format.
        if (!string.IsNullOrEmpty(format) && format.Contains('|', StringComparison.Ordinal))
        {
            format = format.Split('|')[^1].Trim();
        }

        string? size = null;
        if (element.TryGetProperty("width", out var w) && element.TryGetProperty("height", out var h)
            && w.TryGetInt32(out var width) && h.TryGetInt32(out var height) && width > 0 && height > 0)
        {
            size = string.Format(CultureInfo.InvariantCulture, "{0}x{1}", width, height);
        }

        var label = string.Join(' ', new[] { format, size }.Where(s => !string.IsNullOrEmpty(s)));
        return string.IsNullOrEmpty(label) ? null : label;
    }

    // A filter's own id is not a top-level field; ffmpeg records it as "filter_id" inside each of the filter's input and output pad entries.
    private static string? GetFilterId(JsonElement filter)
    {
        foreach (var padArray in new[] { "filter_outputs", "filter_inputs" })
        {
            if (filter.TryGetProperty(padArray, out var pads)
                && pads.ValueKind == JsonValueKind.Array
                && pads.GetArrayLength() > 0)
            {
                var id = GetStringProperty(pads[0], "filter_id");
                if (!string.IsNullOrEmpty(id))
                {
                    return id;
                }
            }
        }

        return null;
    }

    private static string? GetStringProperty(JsonElement element, string name)
    {
        return element.ValueKind == JsonValueKind.Object
            && element.TryGetProperty(name, out var value)
            && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;
    }

    private static void AddEncodeStage(List<TranscodingPipelineStage> stages, IReadOnlyList<string> tokens, int lastInput, EncodingJobInfo state, int? negotiatedBitDepth, VideoRangeType? negotiatedRange)
    {
        var start = lastInput < 0 ? 0 : lastInput;
        var encoder = FindCodecValue(tokens, start, tokens.Count);
        if (string.IsNullOrEmpty(encoder) || EncodingHelper.IsCopyCodec(encoder))
        {
            return;
        }

        var framework = FrameworkFromName(encoder);
        stages.Add(new TranscodingPipelineStage
        {
            Type = TranscodeStageType.Encode,
            Framework = framework,
            Name = encoder,
            Detail = GetCodecDisplayName(state.ActualOutputVideoCodec),
            IsHardware = framework != HardwareFramework.Software,
            VideoBitDepth = GetTargetVideoBitDepth(state, negotiatedBitDepth),
            VideoRange = GetTargetVideoRange(state, stages, negotiatedRange)
        });
    }

    /// <summary>
    /// Maps one ffmpeg filter onto the pipeline stages it represents. Usually that is one stage or
    /// none, but a dual purpose hardware filter performs two distinct operations in a single pass
    /// and gets a node for each - collapsing it to one would hide real work from the graph.
    /// </summary>
    private static List<TranscodingPipelineStage> ClassifyFilter(string filterName, string filterArgs, EncodingJobInfo state, HardwareFramework deviceFramework)
    {
        var stages = new List<TranscodingPipelineStage>();
        var lower = filterName.ToLowerInvariant();
        var framework = FrameworkFromName(lower);
        var isHardware = framework != HardwareFramework.Software;

        // vpp_qsv, libplacebo and scale_vt all scale and tone map in one pass, each spelling the
        // tone mapping option differently. scale_vt has no tone mapping option at all: VideoToolbox
        // is asked for the conversion through the target colour parameters, so the presence of an
        // output transfer is what marks it (EncodingHelper appends the three colour parameters to
        // the scaler only when tone mapping).
        var isToneMap = lower.Contains("tonemap", StringComparison.Ordinal)
            || (lower.Equals("vpp_qsv", StringComparison.Ordinal) && MatchNamed(filterArgs, "tonemap") is not null)
            || (lower.Equals("libplacebo", StringComparison.Ordinal) && MatchNamed(filterArgs, "tonemapping") is not null)
            || (lower.Equals("scale_vt", StringComparison.Ordinal) && MatchNamed(filterArgs, "color_transfer") is not null);

        var isScaler = lower.StartsWith("scale", StringComparison.Ordinal)
            || lower.Equals("vpp_qsv", StringComparison.Ordinal)
            || lower.Equals("zscale", StringComparison.Ordinal)
            || lower.Equals("libplacebo", StringComparison.Ordinal);

        // Only a resize earns a Scale node. A scaler that names no output dimensions is converting
        // the pixel format or colour and nothing else - "scale_vt=format=nv12:color_transfer=bt709"
        // hands on frames at their source resolution, and "zscale=t=linear" is a working-space
        // conversion. Both are plumbing, like the format filters already dropped below. A tone
        // mapper that does carry dimensions is genuinely doing both jobs and gets both nodes.
        if (isScaler && HasScaleDimensions(filterArgs))
        {
            stages.Add(new TranscodingPipelineStage
            {
                Type = TranscodeStageType.Scale,
                Framework = framework,
                Name = filterName,
                Detail = GetScaleDetail(filterArgs, state),
                IsHardware = isHardware
            });
        }

        if (isToneMap)
        {
            stages.Add(new TranscodingPipelineStage
            {
                Type = TranscodeStageType.ToneMap,
                Framework = framework,
                Name = filterName,
                Detail = GetToneMapDetail(filterArgs),
                IsHardware = isHardware
            });
        }

        if (stages.Count > 0)
        {
            return stages;
        }

        if (lower.StartsWith("yadif", StringComparison.Ordinal)
            || lower.StartsWith("bwdif", StringComparison.Ordinal)
            || lower.StartsWith("deinterlace", StringComparison.Ordinal)
            || lower.StartsWith("estdif", StringComparison.Ordinal))
        {
            stages.Add(new TranscodingPipelineStage
            {
                Type = TranscodeStageType.Deinterlace,
                Framework = framework,
                Name = filterName,
                IsHardware = isHardware
            });

            return stages;
        }

        if (lower.StartsWith("overlay", StringComparison.Ordinal)
            || lower.Equals("subtitles", StringComparison.Ordinal)
            || lower.Equals("ass", StringComparison.Ordinal))
        {
            // overlay/subtitles/ass all burn the subtitle into the video frames.
            stages.Add(new TranscodingPipelineStage
            {
                Type = TranscodeStageType.Subtitle,
                Framework = framework,
                Name = filterName,
                Detail = "Burn-in",
                IsHardware = isHardware
            });

            return stages;
        }

        // hwupload/hwdownload move frame data across the software/hardware boundary. Unlike the
        // other plumbing filters - pure format conversion, or hwmap which remaps between two
        // hardware contexts (e.g. QSV <-> OpenCL) and is typically zero-copy - these are real
        // memory transfers that switch between software and hardware memory and can noticeably
        // affect performance, so they are surfaced as their own stages.
        if (lower.StartsWith("hwupload", StringComparison.Ordinal))
        {
            // The upload produces hardware frames; the target framework is encoded either in the
            // name suffix (hwupload_vaapi, hwupload_cuda) or in a "derive_device=<framework>" arg.
            // A bare "hwupload" uploads into the command's -init_hw_device/-hwaccel device.
            var uploadFramework = FrameworkForTransfer(lower, filterArgs, deviceFramework);
            stages.Add(new TranscodingPipelineStage
            {
                Type = TranscodeStageType.HardwareUpload,
                Framework = uploadFramework,
                Name = filterName,
                Detail = "System -> hardware memory",
                IsHardware = uploadFramework != HardwareFramework.Software
            });

            return stages;
        }

        if (lower.StartsWith("hwdownload", StringComparison.Ordinal))
        {
            // The download produces system-memory frames, so the resulting data is in software.
            stages.Add(new TranscodingPipelineStage
            {
                Type = TranscodeStageType.HardwareDownload,
                Framework = HardwareFramework.Software,
                Name = filterName,
                Detail = "Hardware -> system memory",
                IsHardware = false
            });

            return stages;
        }

        // The remaining plumbing filters (format conversion, hwmap) are omitted to keep the
        // pipeline focused on the user visible processing steps.
        return stages;
    }

    // Whether a scaling filter actually names output dimensions, either as named options
    // ("scale_vt=w=1920:h=1080") or positionally ("scale=1280:720", "scale=trunc(...):-2").
    private static bool HasScaleDimensions(string filterArgs)
    {
        if (MatchNamed(filterArgs, "w") is not null
            || MatchNamed(filterArgs, "h") is not null
            || MatchNamed(filterArgs, "width") is not null
            || MatchNamed(filterArgs, "height") is not null)
        {
            return true;
        }

        // Positional form: the first argument is a bare value rather than a "key=value" option.
        var first = filterArgs.Split(':', 2)[0];
        return first.Length > 0 && !first.Contains('=', StringComparison.Ordinal);
    }

    // Resolves the hardware framework an hwupload targets: the name suffix wins (hwupload_vaapi,
    // hwupload_cuda), then the filter's "derive_device=<framework>" argument, and finally the
    // device the command line initialized (a bare "hwupload" uploads into that one).
    private static HardwareFramework FrameworkForTransfer(string lowerName, string filterArgs, HardwareFramework deviceFramework)
    {
        var fromName = FrameworkFromName(lowerName);
        if (fromName != HardwareFramework.Software)
        {
            return fromName;
        }

        var device = MatchNamed(filterArgs, "derive_device");
        if (!string.IsNullOrEmpty(device))
        {
            return FrameworkFromHwaccel(device);
        }

        return deviceFramework;
    }

    private static string? GetScaleDetail(string filterArgs, EncodingJobInfo state)
    {
        var width = MatchDimension(filterArgs, 'w') ?? MatchNamed(filterArgs, "width");
        var height = MatchDimension(filterArgs, 'h') ?? MatchNamed(filterArgs, "height");

        if (width is not null && height is not null && !width.Contains("-1", StringComparison.Ordinal) && !height.Contains("-1", StringComparison.Ordinal))
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}x{1}", width, height);
        }

        if (state.OutputWidth.HasValue && state.OutputHeight.HasValue)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}x{1}", state.OutputWidth.Value, state.OutputHeight.Value);
        }

        return null;
    }

    // The bit depth the encoder actually produces. The pixel format the filter chain ends on is
    // authoritative - tone mapping to SDR lands on an 8-bit format even from a 10-bit source, and
    // without it the encode stage would keep advertising the source depth. Failing that, the
    // request only carries the *maximum* depth the client accepts (MaxVideoBitDepth, or the
    // profile's "videobitdepth" condition), so it can only cap the source depth: taking it verbatim
    // would claim a 10-bit output for an 8-bit source played to a 10-bit capable client.
    private static int? GetTargetVideoBitDepth(EncodingJobInfo state, int? negotiatedBitDepth)
    {
        if (negotiatedBitDepth.HasValue)
        {
            return negotiatedBitDepth;
        }

        var sourceDepth = state.VideoStream?.BitDepth;
        var maxDepth = state.BaseRequest is null ? null : state.GetRequestedVideoBitDepth(state.ActualOutputVideoCodec);

        if (maxDepth is null)
        {
            return sourceDepth;
        }

        return sourceDepth is null ? maxDepth : Math.Min(maxDepth.Value, sourceDepth.Value);
    }

    // The bit depth of the last pixel format the filter chain names - what physically reaches the
    // encoder. Both a standalone "format=nv12" filter and a "format=" option on a scaler or tone
    // mapper count; the last one wins.
    private static int? GetFilterChainBitDepth(IReadOnlyList<string> tokens, int lastInput)
    {
        var graph = FindFilterGraph(tokens, lastInput);
        if (string.IsNullOrWhiteSpace(graph))
        {
            return null;
        }

        int? depth = null;
        foreach (var chain in SplitGraph(graph, ';'))
        {
            if (IsSubtitlePrepChain(chain))
            {
                continue;
            }

            foreach (var rawFilter in SplitGraph(StreamLabelRegex().Replace(chain, string.Empty), ','))
            {
                var filter = rawFilter.Trim();
                var eq = filter.IndexOf('=', StringComparison.Ordinal);
                if (eq <= 0)
                {
                    continue;
                }

                var name = filter[..eq].Trim();
                var args = filter[(eq + 1)..];

                // "format=nv12" and "format=pix_fmts=nv12" are the bare filter; everything else
                // carries the pixel format as a "format=" option.
                var pixelFormat = string.Equals(name, "format", StringComparison.OrdinalIgnoreCase)
                    ? (MatchNamed(args, "pix_fmts") ?? args.Split(':')[0])
                    : MatchNamed(args, "format");

                depth = GetPixelFormatBitDepth(pixelFormat) ?? depth;
            }
        }

        return depth;
    }

    // ffmpeg pixel format names encode their depth as a suffix (yuv420p10le, p010le); anything
    // without one is 8-bit. Returns null for names that aren't recognisable pixel formats, so an
    // unrelated "format=" option can't be mistaken for one.
    private static int? GetPixelFormatBitDepth(string? pixelFormat)
    {
        if (string.IsNullOrEmpty(pixelFormat))
        {
            return null;
        }

        var format = pixelFormat.Trim().ToLowerInvariant();

        foreach (var (marker, depth) in new[] { ("16", 16), ("14", 14), ("12", 12), ("10", 10), ("9", 9) })
        {
            // "yuv420p10le" / "p010le" / "yuv444p12".
            if (format.Contains('p' + marker, StringComparison.Ordinal))
            {
                return depth;
            }
        }

        return format.StartsWith("yuv", StringComparison.Ordinal)
            || format.StartsWith("nv", StringComparison.Ordinal)
            || format.StartsWith("gbr", StringComparison.Ordinal)
            || format is "bgra" or "rgba" or "argb" or "abgr" or "rgb24" or "bgr24"
                ? 8
                : null;
    }

    // The range the encoder actually produces. This is a property of the pipeline, not of the
    // request: the request only lists the ranges the client is willing to accept.
    //
    // The transfer characteristics the filter chain converts to are the authority, because every
    // tone mapping path names them (t=bt709, color_trc=bt709, color_transfer=bt709) whether or not
    // its filter is recognisable as a tone mapper. Deciding this from the classified stages alone
    // is what let a scale_vt tone map report its source range: on VideoToolbox the scaler and the
    // tone mapper are the same filter, so a missed classification silently reported HDR out of an
    // SDR transcode. Falling back to the stages keeps the frameworks that convert internally
    // (vpp_qsv) correct, and to the source range when nothing touches the signal.
    private static VideoRangeType? GetTargetVideoRange(EncodingJobInfo state, IEnumerable<TranscodingPipelineStage> stages, VideoRangeType? negotiatedRange)
    {
        if (negotiatedRange.HasValue)
        {
            return negotiatedRange;
        }

        if (stages.Any(s => s.Type == TranscodeStageType.ToneMap))
        {
            return VideoRangeType.SDR;
        }

        return state.VideoStream?.VideoRangeType;
    }

    // The range implied by the last transfer characteristic the filter chain converts to. Values it
    // does not recognise (such as the "linear" working space the software tone mapper passes
    // through) are ignored rather than treated as an answer.
    private static VideoRangeType? GetFilterChainVideoRange(IReadOnlyList<string> tokens, int lastInput)
    {
        var graph = FindFilterGraph(tokens, lastInput);
        if (string.IsNullOrWhiteSpace(graph))
        {
            return null;
        }

        VideoRangeType? range = null;
        foreach (var chain in SplitGraph(graph, ';'))
        {
            if (IsSubtitlePrepChain(chain))
            {
                continue;
            }

            foreach (var rawFilter in SplitGraph(StreamLabelRegex().Replace(chain, string.Empty), ','))
            {
                var eq = rawFilter.IndexOf('=', StringComparison.Ordinal);
                if (eq <= 0)
                {
                    continue;
                }

                var args = rawFilter[(eq + 1)..];

                // scale_vt spells it "color_transfer", libplacebo "color_trc", and the tone mappers
                // and zscale use the short "t".
                var transfer = MatchNamed(args, "color_transfer")
                    ?? MatchNamed(args, "color_trc")
                    ?? MatchNamed(args, "transfer")
                    ?? MatchNamed(args, "t");

                range = TransferToVideoRange(transfer) ?? range;
            }
        }

        return range;
    }

    private static VideoRangeType? TransferToVideoRange(string? transfer)
    {
        if (string.IsNullOrEmpty(transfer))
        {
            return null;
        }

        return transfer.Trim().ToLowerInvariant() switch
        {
            "bt709" or "bt601" or "smpte170m" or "smpte240m" or "bt470bg" or "bt470m" or "srgb" or "iec61966-2-1" => VideoRangeType.SDR,
            "smpte2084" or "pq" => VideoRangeType.HDR10,
            "arib-std-b67" or "hlg" => VideoRangeType.HLG,
            _ => null
        };
    }

    private static string? GetToneMapDetail(string filterArgs)
    {
        // libplacebo spells the option "tonemapping", everything else "tonemap".
        var algo = MatchNamed(filterArgs, "tonemap") ?? MatchNamed(filterArgs, "tonemapping");
        if (!string.IsNullOrEmpty(algo) && !char.IsDigit(algo[0]))
        {
            return char.ToUpperInvariant(algo[0]) + algo[1..];
        }

        // scale_vt names no algorithm (VideoToolbox picks one) and vpp_qsv only takes "tonemap=1",
        // so fall back to naming the conversion itself.
        var target = TransferToVideoRange(
            MatchNamed(filterArgs, "color_transfer")
            ?? MatchNamed(filterArgs, "color_trc")
            ?? MatchNamed(filterArgs, "t"));

        return target is null ? null : "HDR to " + target;
    }

    private static HardwareFramework FrameworkFromName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return HardwareFramework.Software;
        }

        var lower = name.ToLowerInvariant();
        if (lower.EndsWith("_qsv", StringComparison.Ordinal) || lower.Equals("vpp_qsv", StringComparison.Ordinal))
        {
            return HardwareFramework.Qsv;
        }

        if (lower.EndsWith("_vaapi", StringComparison.Ordinal))
        {
            return HardwareFramework.Vaapi;
        }

        if (lower.EndsWith("_cuda", StringComparison.Ordinal)
            || lower.EndsWith("_npp", StringComparison.Ordinal)
            || lower.EndsWith("_nvenc", StringComparison.Ordinal)
            || lower.EndsWith("_cuvid", StringComparison.Ordinal))
        {
            return HardwareFramework.Cuda;
        }

        if (lower.EndsWith("_opencl", StringComparison.Ordinal))
        {
            return HardwareFramework.OpenCl;
        }

        // libplacebo is a Vulkan filter; its name carries no framework suffix.
        if (lower.EndsWith("_vulkan", StringComparison.Ordinal) || lower.Equals("libplacebo", StringComparison.Ordinal))
        {
            return HardwareFramework.Vulkan;
        }

        if (lower.EndsWith("_vt", StringComparison.Ordinal)
            || lower.EndsWith("_videotoolbox", StringComparison.Ordinal))
        {
            return HardwareFramework.VideoToolbox;
        }

        if (lower.EndsWith("_amf", StringComparison.Ordinal))
        {
            return HardwareFramework.Amf;
        }

        if (lower.EndsWith("_rkmpp", StringComparison.Ordinal) || lower.EndsWith("_rkrga", StringComparison.Ordinal))
        {
            return HardwareFramework.Rkmpp;
        }

        if (lower.EndsWith("_v4l2m2m", StringComparison.Ordinal))
        {
            return HardwareFramework.V4l2m2m;
        }

        if (lower.EndsWith("_at", StringComparison.Ordinal))
        {
            return HardwareFramework.AudioToolbox;
        }

        return HardwareFramework.Software;
    }

    // The hardware device the command line set up. Filters that don't name a framework themselves
    // (a bare "hwupload") attach to this one. "-init_hw_device <type>[=name[:opts]]" is checked
    // first because that is the device the filter graph is bound to.
    private static HardwareFramework GetDeviceFramework(IReadOnlyList<string> tokens, int firstInput)
    {
        var inputEnd = firstInput < 0 ? tokens.Count : firstInput;

        var initDevice = FindOptionValue(tokens, 0, inputEnd, "-init_hw_device");
        if (!string.IsNullOrEmpty(initDevice))
        {
            var framework = FrameworkFromHwaccel(initDevice.Split('=', 2)[0]);
            if (framework != HardwareFramework.Software)
            {
                return framework;
            }
        }

        return FrameworkFromHwaccel(FindOptionValue(tokens, 0, inputEnd, "-hwaccel"));
    }

    private static HardwareFramework FrameworkFromHwaccel(string? hwaccel)
    {
        if (string.IsNullOrEmpty(hwaccel))
        {
            return HardwareFramework.Software;
        }

        return hwaccel.ToLowerInvariant() switch
        {
            "qsv" => HardwareFramework.Qsv,
            "cuda" or "cuvid" or "nvdec" => HardwareFramework.Cuda,
            "vaapi" or "drm" => HardwareFramework.Vaapi,
            "d3d11va" => HardwareFramework.D3D11Va,
            "dxva2" => HardwareFramework.Dxva2,
            "videotoolbox" => HardwareFramework.VideoToolbox,
            "opencl" => HardwareFramework.OpenCl,
            "vulkan" => HardwareFramework.Vulkan,
            "rkmpp" => HardwareFramework.Rkmpp,
            "v4l2m2m" or "v4l2" => HardwareFramework.V4l2m2m,
            _ => HardwareFramework.Software
        };
    }

    private static string? GetCodecDisplayName(string? codec)
    {
        if (string.IsNullOrEmpty(codec))
        {
            return null;
        }

        return codec.ToLowerInvariant() switch
        {
            "hevc" or "h265" => "H.265 (HEVC)",
            "h264" or "avc" => "H.264 (AVC)",
            "av1" => "AV1",
            "vp9" => "VP9",
            "vp8" => "VP8",
            "mpeg2video" or "mpeg2" => "MPEG-2",
            "mpeg4" => "MPEG-4",
            "vc1" => "VC-1",
            "aac" => "AAC",
            "ac3" => "Dolby Digital (AC-3)",
            "eac3" => "Dolby Digital Plus (E-AC-3)",
            "dts" or "dca" => "DTS",
            "truehd" => "Dolby TrueHD",
            "flac" => "FLAC",
            "opus" => "Opus",
            "mp3" => "MP3",
            "vorbis" => "Vorbis",
            _ => codec.ToUpperInvariant()
        };
    }

    // Finds the value of a video codec option (handles indexed forms such as "-codec:v:0").
    private static string? FindCodecValue(IReadOnlyList<string> tokens, int start, int end)
    {
        return FindCodecValueForType(tokens, start, end, 'v');
    }

    // Finds the value of an audio codec option (handles indexed forms such as "-codec:a:0").
    private static string? FindAudioCodecValue(IReadOnlyList<string> tokens, int start, int end)
    {
        return FindCodecValueForType(tokens, start, end, 'a');
    }

    // Matches "-c:<t>", "-codec:<t>", "-<t>codec" and their stream-indexed variants ("-c:<t>:0").
    private static string? FindCodecValueForType(IReadOnlyList<string> tokens, int start, int end, char type)
    {
        end = Math.Min(end, tokens.Count);
        var cShort = "-c:" + type;
        var cLong = "-codec:" + type;
        var cAlt = "-" + (type == 'v' ? "vcodec" : "acodec");

        for (var i = Math.Max(0, start); i < end - 1; i++)
        {
            var t = tokens[i];
            if (string.Equals(t, cShort, StringComparison.Ordinal)
                || string.Equals(t, cLong, StringComparison.Ordinal)
                || string.Equals(t, cAlt, StringComparison.Ordinal)
                || t.StartsWith(cShort + ":", StringComparison.Ordinal)
                || t.StartsWith(cLong + ":", StringComparison.Ordinal))
            {
                return tokens[i + 1].Trim('"');
            }
        }

        return null;
    }

    private static string? FindOptionValue(IReadOnlyList<string> tokens, int start, int end, string option)
    {
        end = Math.Min(end, tokens.Count);
        for (var i = Math.Max(0, start); i < end - 1; i++)
        {
            if (string.Equals(tokens[i], option, StringComparison.Ordinal))
            {
                return tokens[i + 1].Trim('"');
            }
        }

        return null;
    }

    private static string? MatchDimension(string input, char dim)
    {
        return MatchNamed(input, dim.ToString());
    }

    // Matches "<key>=<value>" either at the start of the argument list or right after a ':'
    // separator. The boundary matters: without it "w=" would match inside "downscaler=none" and
    // "tonemap=" inside libplacebo's "tonemapping=".
    private static string? MatchNamed(string input, string key)
    {
        if (string.IsNullOrEmpty(input))
        {
            return null;
        }

        var match = Regex.Match(
            input,
            @"(?:^|:)" + Regex.Escape(key) + @"=([^:]+)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        return match.Success ? match.Groups[1].Value.Trim() : null;
    }

    // Splits a command line into tokens, treating double quoted spans as part of the value that follows an option (so the filter graph after -vf "..." stays a single token).
    private static List<string> Tokenize(string commandLine)
    {
        var tokens = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuotes = false;

        foreach (var c in commandLine)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (c == ' ' && !inQuotes)
            {
                if (current.Length > 0)
                {
                    tokens.Add(current.ToString());
                    current.Clear();
                }

                continue;
            }

            current.Append(c);
        }

        if (current.Length > 0)
        {
            tokens.Add(current.ToString());
        }

        return tokens;
    }

    [GeneratedRegex(@"\[[^\]]*\]")]
    private static partial Regex StreamLabelRegex();

    [GeneratedRegex(@"\[([^\]]*)\]\s*$")]
    private static partial Regex TrailingLabelRegex();
}
