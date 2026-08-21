namespace MediaBrowser.Controller.MediaEncoding.FFProcessing;

/// <summary>
/// The operations this server performs with FFmpeg. The action determines which binary runs, the
/// clobber policy, the stdin disposition, the deadlines and the priority.
/// </summary>
public enum FFAction
{
    /// <summary>Container and stream metadata, as JSON.</summary>
    Probe,

    /// <summary>Keyframe packet timings, as CSV.</summary>
    ScanKeyframes,

    /// <summary>
    /// Version check of a candidate binary, run before that path is accepted. Uniquely, it names
    /// the executable to launch rather than using the resolved one, because deciding whether a
    /// path is usable is precisely what it exists to do.
    /// </summary>
    ValidateBinary,

    /// <summary>Interrogation of the binary's own feature support.</summary>
    Capabilities,

    /// <summary>
    /// Test of whether the encoder responds to runtime keys. Steerable by definition: it cannot
    /// run with <c>-nostdin</c>, which would disable what it is testing.
    /// </summary>
    ProbeRuntimeKeys,

    /// <summary>EBU R128 loudness measurement, reported on stderr.</summary>
    MeasureLoudness,

    /// <summary>Embedded attachments dumped to disk.</summary>
    ExtractAttachment,

    /// <summary>An embedded subtitle track written to a sidecar file.</summary>
    ExtractSubtitle,

    /// <summary>A single decoded frame written as a still image.</summary>
    ExtractImage,

    /// <summary>Frames decoded at a fixed interval for trickplay tiles.</summary>
    GenerateTrickplay,

    /// <summary>A live stream recorded to a file.</summary>
    Record,

    /// <summary>
    /// Media delivered to a client, for as long as that client keeps watching. Steerable, since
    /// the throttler pauses and resumes it and playback ending has to stop it.
    /// </summary>
    Stream
}
