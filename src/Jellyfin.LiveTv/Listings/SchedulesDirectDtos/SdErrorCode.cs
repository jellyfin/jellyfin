#pragma warning disable CA1008 // Enums should have zero value

namespace Jellyfin.LiveTv.Listings.SchedulesDirectDtos;

/// <summary>
/// Schedules Direct API error codes.
/// </summary>
public enum SdErrorCode
{
    /// <summary>
    /// Invalid user.
    /// </summary>
    InvalidUser = 4001,

    /// <summary>
    /// Invalid password hash.
    /// </summary>
    InvalidHash = 4003,

    /// <summary>
    /// Account locked or disabled.
    /// </summary>
    AccountLocked = 4004,

    /// <summary>
    /// Account expired.
    /// </summary>
    AccountExpired = 4005,

    /// <summary>
    /// Token has expired.
    /// </summary>
    TokenExpired = 4006,

    /// <summary>
    /// Password is required.
    /// </summary>
    PasswordRequired = 4008,

    /// <summary>
    /// Maximum login attempts exceeded.
    /// </summary>
    MaxLoginAttempts = 4009,

    /// <summary>
    /// Temporary lockout.
    /// </summary>
    TemporaryLockout = 4010,

    /// <summary>
    /// Maximum image downloads reached for the day.
    /// </summary>
    MaxImageDownloads = 5002,

    /// <summary>
    /// Maximum schedule/metadata requests reached for the day.
    /// </summary>
    MaxScheduleRequests = 5003
}
