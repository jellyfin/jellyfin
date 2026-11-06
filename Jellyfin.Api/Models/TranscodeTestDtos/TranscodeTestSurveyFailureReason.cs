namespace Jellyfin.Api.Models.TranscodeTestDtos;

/// <summary>
/// Why a stress test ended, as the Jellyfin hardware survey (hwa.jellyfin.org) names it, so results can be compared
/// with it.
/// </summary>
public enum TranscodeTestSurveyFailureReason
{
    /// <summary>
    /// A stream fell behind real time: the survey's <c>performance</c>.
    /// </summary>
    Performance,

    /// <summary>
    /// A stream produced no frames in time: the survey's <c>failed_timeout</c>.
    /// </summary>
    FailedTimeout,

    /// <summary>
    /// NVIDIA's driver refused another encoding session: the survey's <c>failed_nvenc_limit</c>.
    /// </summary>
    FailedNvencLimit
}
