#pragma warning disable CA1008 // Enums should have zero value

namespace Jellyfin.LiveTv.Listings.SchedulesDirectDtos;

/// <summary>
/// Schedules Direct API error codes. See https://github.com/SchedulesDirect/JSON-Service/wiki/API-20141201#error-response for details.
/// </summary>
public enum SdErrorCode
{
    /// <summary>
    /// Schedules Direct unavailable/out of service.
    /// </summary>
    SvcUnavailable = 3000,

    /// <summary>
    /// Schedules Direct busy.
    /// </summary>
    SvcBusy = 3001,

    /// <summary>
    /// Account expired.
    /// </summary>
    AccountExpired = 4001,

    /// <summary>
    /// Invalid password hash.
    /// </summary>
    InvalidHash = 4002,

    /// <summary>
    /// Invalid user or password.
    /// </summary>
    InvalidUser = 4003,

    /// <summary>
    /// Account temporarily locked due to login failures.
    /// </summary>
    AccountTempLock = 4004,

    /// <summary>
    /// Account permanently locked due to abuse.
    /// </summary>
    AccountLocked = 4005,

    /// <summary>
    /// Token has expired. Request a new one.
    /// </summary>
    TokenExpired = 4006,

    /// <summary>
    /// Application locked out.
    /// </summary>
    AppLocked = 4007,

    /// <summary>
    /// Account not active.
    /// </summary>
    AccountInactive = 4008,

    /// <summary>
    /// Maximum login attempts exceeded.
    /// </summary>
    MaxLoginAttempts = 4009,

    /// <summary>
    /// Maximum unique IP attempts reached.
    /// </summary>
    MaxIPAttempts = 4010,

    /// <summary>
    /// Lineup change maximum reached.
    /// </summary>
    MaxScheduleRequests = 4100,

    /// <summary>
    /// Requested image not found.
    /// </summary>
    ImageNotFound = 5000,

    /// <summary>
    /// Maximum image downloads reached for the day.
    /// </summary>
    MaxImageDownloads = 5002,

    /// <summary>
    /// Trial specific maximum image downloads reached for the day.
    /// </summary>
    MaxImageDownloadsTrial = 5003,

    /// <summary>
    /// Maximum schedule/metadata requests reached for the day.
    /// </summary>
    MaxInvalidImages = 5004
}
