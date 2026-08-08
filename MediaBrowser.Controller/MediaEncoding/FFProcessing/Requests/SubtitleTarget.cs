namespace MediaBrowser.Controller.MediaEncoding.FFProcessing.Requests;

/// <summary>
/// One subtitle stream to write out of a container.
/// </summary>
/// <param name="StreamIndex">The stream's index within the container.</param>
/// <param name="OutputPath">Where to write it, already normalised by the caller.</param>
/// <param name="CopyStream">
/// Whether the stream can be written as-is. When false it is converted to SubRip, which is the
/// only text format worth normalising to.
/// </param>
/// <param name="IsVobSub">
/// Whether the stream is VobSub. FFmpeg has no .idx/.sub muxer, so these have to be wrapped in
/// Matroska instead.
/// </param>
public readonly record struct SubtitleTarget(
    int StreamIndex,
    string OutputPath,
    bool CopyStream,
    bool IsVobSub);
