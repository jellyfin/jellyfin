using System;

namespace MediaBrowser.Model;

/// <summary>
///     Model containing the arguments for enumerating the requested media item.
/// </summary>
public class MediaSegmentGenerationRequest
{
    public string MediaPath { get; set; }
}
