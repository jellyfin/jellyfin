using System;
using System.Collections.Generic;

namespace Jellyfin.Api.Models.TranscodeTestDtos;

/// <summary>
/// The state and results of the current or last transcoding test.
/// </summary>
public class TranscodeTestStatusDto
{
    /// <summary>
    /// Gets or sets the state.
    /// </summary>
    public TranscodeTestState State { get; set; }

    /// <summary>
    /// Gets or sets the kind of test.
    /// </summary>
    public TranscodeTestKind Kind { get; set; }

    /// <summary>
    /// Gets or sets the request the test was started with.
    /// </summary>
    public TranscodeTestRequestDto? Request { get; set; }

    /// <summary>
    /// Gets or sets when the test started.
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// Gets or sets when the test ended.
    /// </summary>
    public DateTime? EndedAt { get; set; }

    /// <summary>
    /// Gets or sets the console lines written so far.
    /// </summary>
    public IReadOnlyList<TranscodeTestLogLine> Log { get; set; } = [];

    /// <summary>
    /// Gets or sets the compatibility check results.
    /// </summary>
    public IReadOnlyList<TranscodeTestCheckResult> Checks { get; set; } = [];

    /// <summary>
    /// Gets or sets the stress test progress and result.
    /// </summary>
    public TranscodeTestStressResult? Stress { get; set; }
}
