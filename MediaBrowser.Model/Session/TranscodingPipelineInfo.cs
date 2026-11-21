using System.Collections.Generic;

namespace MediaBrowser.Model.Session;

/// <summary>
/// Class describing the full transcoding pipeline (the ordered chain of decode, filter and encode stages) of a running transcode.
/// </summary>
public class TranscodingPipelineInfo
{
    /// <summary>
    /// Gets or sets the ordered stages of the pipeline, from decode to encode.
    /// </summary>
    public IReadOnlyList<TranscodingPipelineStage>? Stages { get; set; }
}
