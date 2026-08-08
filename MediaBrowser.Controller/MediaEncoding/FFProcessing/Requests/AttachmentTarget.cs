namespace MediaBrowser.Controller.MediaEncoding.FFProcessing.Requests;

/// <summary>
/// One attachment to extract.
/// </summary>
/// <param name="StreamIndex">The attachment's stream index within the container.</param>
/// <param name="OutputPath">Where to write it, already normalised by the caller.</param>
public readonly record struct AttachmentTarget(int StreamIndex, string OutputPath);
