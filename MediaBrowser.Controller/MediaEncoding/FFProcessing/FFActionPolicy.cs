using System;
using System.Diagnostics;

namespace MediaBrowser.Controller.MediaEncoding.FFProcessing;

/// <summary>
/// The process policy an <see cref="FFAction"/> carries.
/// </summary>
/// <param name="ProbeOnly">Whether the action runs ffprobe rather than ffmpeg.</param>
/// <param name="Overwrite">
/// Whether existing output may be replaced. Always emitted, because with neither <c>-y</c> nor
/// <c>-n</c> and non-interactive stdin FFmpeg fatals with "File ... already exists. Exiting".
/// ffprobe registers neither option.
/// </param>
/// <param name="Stdin">Whether the process is steerable through runtime keys.</param>
/// <param name="Timeout">The wall-clock deadline when the request does not supply one.</param>
/// <param name="IdleTimeout">
/// How long the process may report no progress before it is killed. Only applies when the request
/// supplies a progress signal; CPU time is not used as one, since work blocked on a slow network
/// read accrues none.
/// </param>
/// <param name="GracefulStopTimeout">
/// How long a steerable process is given to exit after being sent <c>q</c>, before it is killed.
/// </param>
/// <param name="Priority">The scheduling priority when the request does not supply one.</param>
/// <param name="RequiresProgressStats">
/// Whether the action needs FFmpeg's periodic status line. That line is suppressed below
/// <c>info</c> unless <c>-stats</c> is asked for explicitly, so an action parsing it for progress
/// gets nothing at the level a default logger derives.
/// </param>
/// <param name="StderrIsPayload">
/// Whether the action reads its own standard error as the answer rather than as a diagnostic. Such
/// an action needs a log level high enough for the answer to be emitted at all, and needs the whole
/// of it retained rather than a trailing window.
/// </param>
public readonly record struct FFActionPolicy(
    bool ProbeOnly,
    bool Overwrite,
    FFStdinMode Stdin,
    TimeSpan Timeout,
    TimeSpan IdleTimeout,
    TimeSpan GracefulStopTimeout,
    ProcessPriorityClass Priority,
    bool RequiresProgressStats,
    bool StderrIsPayload)
{
    /// <summary>
    /// Fallback when <c>ServerConfiguration.ImageExtractionTimeoutMs</c> is unset. Matches
    /// <c>MediaEncoder.DefaultHdrImageExtractionTimeout</c>; SDR callers pass the shorter 10s.
    /// </summary>
    public static readonly TimeSpan DefaultImageExtraction = TimeSpan.FromSeconds(20);

    /// <summary>
    /// Fallback when <c>EncodingOptions.SubtitleExtractionTimeoutMinutes</c> is unset, matching
    /// that setting's own default.
    /// </summary>
    public static readonly TimeSpan DefaultSubtitleExtraction = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Grace after <c>q</c>, matching the wait in <c>EncodedRecorder.Stop</c>.
    /// </summary>
    public static readonly TimeSpan RecordingStopGrace = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Grace after <c>q</c> for a delivery stream, matching the wait in <c>TranscodingJob.Stop</c>.
    /// Shorter than a recording's: nobody is watching any more, and the segments already written
    /// stay valid.
    /// </summary>
    public static readonly TimeSpan StreamStopGrace = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Deadline for reading metadata out of a container. Generous because the read may cross a
    /// slow network mount, and it is the only bound these actions have.
    /// </summary>
    public static readonly TimeSpan MetadataRead = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Deadline for reading an entire file through. The work is I/O bound, so the real cost is size
    /// divided by link speed: a 90 GB remux is a quarter of an hour on gigabit but over two hours on
    /// a slow share. Loose enough that only a hung or badly degraded process reaches it.
    /// </summary>
    public static readonly TimeSpan FullFileScan = TimeSpan.FromHours(6);

    /// <summary>
    /// Deadline for decoding an entire audio stream, which runs many times faster than realtime
    /// even across a whole album.
    /// </summary>
    public static readonly TimeSpan FullAudioScan = TimeSpan.FromHours(1);

    /// <summary>
    /// Deadline for a startup interrogation. The work is trivial, so slow here means something is wrong.
    /// </summary>
    public static readonly TimeSpan StartupInterrogation = TimeSpan.FromMinutes(2);

    /// <summary>
    /// What almost every action wants: run FFmpeg, replace whatever output is already there, never
    /// speak to it once started, bound it by the wall clock alone, and run it below the priority of
    /// whatever the user is actually watching.
    /// </summary>
    private static readonly FFActionPolicy Background = new(
        ProbeOnly: false,
        Overwrite: true,
        Stdin: FFStdinMode.FireAndForget,
        Timeout: MetadataRead,
        IdleTimeout: FFDefaults.Unbounded,
        GracefulStopTimeout: TimeSpan.Zero,
        Priority: ProcessPriorityClass.BelowNormal,
        RequiresProgressStats: false,
        StderrIsPayload: false);

    /// <summary>
    /// Gets the policy for an action. Each arm states only what makes that action unusual; the rest
    /// comes from <see cref="Background"/>.
    /// <para>
    /// Two rules run across the whole table. The idle watchdog is only armed where the request
    /// supplies a real progress signal, so every action but one is bounded by the wall clock alone.
    /// And <c>StderrIsPayload</c> means the action reads its own standard error as the answer, which
    /// both floors the log level and retains all of it rather than a trailing window.
    /// </para>
    /// <para>
    /// <c>FFActionPolicyTests</c> pins the distinctive values per action, so an arm that silently
    /// loses one fails the build rather than shipping.
    /// </para>
    /// </summary>
    /// <param name="action">The action.</param>
    /// <returns>Its policy.</returns>
    public static FFActionPolicy For(FFAction action) => action switch
    {
        FFAction.Probe => Background with
        {
            ProbeOnly = true,
            Overwrite = false
        },

        FFAction.ScanKeyframes => Background with
        {
            ProbeOnly = true,
            Overwrite = false,
            Timeout = FullFileScan
        },

        // Runs an executable the administrator nominated, which has not been shown to behave, so it
        // is the interrogation most likely to hang. Bounding it is the whole point.
        FFAction.ValidateBinary => Background with
        {
            Overwrite = false,
            Timeout = StartupInterrogation,
            Priority = FFDefaults.InheritPriority
        },

        // The device probes answer by scraping their own stderr. Those needing more than info ask
        // for it in their own arguments, which land after the level the payload floor sets.
        FFAction.Capabilities => Background with
        {
            Overwrite = false,
            Timeout = StartupInterrogation,
            Priority = FFDefaults.InheritPriority,
            StderrIsPayload = true
        },

        // Steerable because the probe writes a key to stdin and reads the reply off stderr; a
        // process told not to read stdin cannot answer the question being asked of it.
        FFAction.ProbeRuntimeKeys => Background with
        {
            Overwrite = false,
            Stdin = FFStdinMode.ControlChannel,
            Timeout = StartupInterrogation,
            Priority = FFDefaults.InheritPriority,
            StderrIsPayload = true
        },

        // ebur128 writes its loudness summary to stderr at info, so the result vanishes entirely
        // under a quieter level.
        FFAction.MeasureLoudness => Background with
        {
            Overwrite = false,
            Timeout = FullAudioScan,
            StderrIsPayload = true
        },

        FFAction.ExtractAttachment => Background,

        FFAction.ExtractSubtitle => Background with
        {
            Timeout = DefaultSubtitleExtraction
        },

        FFAction.ExtractImage => Background with
        {
            Timeout = DefaultImageExtraction
        },

        // Runs as long as the media is long, so no wall clock can bound it. The one action with a
        // progress signal — each new tile — so the idle watchdog does the bounding instead.
        FFAction.GenerateTrickplay => Background with
        {
            Timeout = FFDefaults.Unbounded,
            IdleTimeout = DefaultImageExtraction
        },

        // Bounded by the recording's own duration rather than a deadline, and stopped by asking
        // rather than killing, so the container is finalised and the file stays playable.
        FFAction.Record => Background with
        {
            Stdin = FFStdinMode.ControlChannel,
            Timeout = FFDefaults.Unbounded,
            GracefulStopTimeout = RecordingStopGrace,
            Priority = ProcessPriorityClass.Normal
        },

        // Lives as long as the viewer watches. Stopped by asking, so an HLS segment in flight is
        // finished rather than truncated. Normal priority because someone is waiting on it, and
        // -stats because JobLogSink parses that line for progress and throttling.
        // Overwrite is stated rather than inherited, and StreamRequest pins it on regardless of what
        // is written here. Delivery routinely writes over what an abandoned session left behind, and
        // FFmpeg refusing to overwrite still exits 0, so turning this off would silently disable
        // playback. See the remarks on StreamRequest.
        FFAction.Stream => Background with
        {
            Overwrite = true,
            Stdin = FFStdinMode.ControlChannel,
            Timeout = FFDefaults.Unbounded,
            GracefulStopTimeout = StreamStopGrace,
            Priority = ProcessPriorityClass.Normal,
            RequiresProgressStats = true
        },

        _ => throw new ArgumentOutOfRangeException(nameof(action), action, "No policy defined.")
    };
}
