namespace Jellyfin.LiveTv.Listings.SchedulesDirectDtos;

/// <summary>
/// Schedules Direct API error codes. See https://github.com/SchedulesDirect/JSON-Service/wiki/API-20141201#error-codes for details.
/// </summary>
/// <remarks>
/// A complete mirror of the documented table. Codes the client does not act on are still listed,
/// because membership is what lets a response be logged by name instead of by number.
/// </remarks>
public enum SdErrorCode
{
    /// <summary>
    /// No error.
    /// </summary>
    Ok = 0,

    /// <summary>
    /// Unable to decode JSON.
    /// </summary>
    InvalidJson = 1001,

    /// <summary>
    /// Did not receive a User-Agent in the request header.
    /// </summary>
    UserAgentRequired = 1003,

    /// <summary>
    /// Token required but not provided in the request header.
    /// </summary>
    TokenMissing = 1004,

    /// <summary>
    /// Did not recognize the submitted client.
    /// </summary>
    UnknownClient = 1005,

    /// <summary>
    /// The maximum number of elements in a single request is 5000.
    /// </summary>
    MaxChunkExceeded = 1006,

    /// <summary>
    /// The request sent to the server was empty.
    /// </summary>
    EmptyRequest = 1007,

    /// <summary>
    /// The request is improperly formatted.
    /// </summary>
    IncorrectRequest = 1008,

    /// <summary>
    /// The maximum number of elements in a single request is 500.
    /// </summary>
    MaxChunkExceededMetadata = 1009,

    /// <summary>
    /// Token is not 32 characters.
    /// </summary>
    TokenInvalid = 1010,

    /// <summary>
    /// The Content-Type header must be application/json.
    /// </summary>
    IncorrectContentType = 1011,

    /// <summary>
    /// Did not receive request.
    /// </summary>
    RequiredRequestMissing = 2002,

    /// <summary>
    /// Searching for lineups requires a 3-letter country parameter.
    /// </summary>
    CountryRequired = 2004,

    /// <summary>
    /// Searching for lineups requires a postal code parameter.
    /// </summary>
    PostalCodeRequired = 2005,

    /// <summary>
    /// The request is missing the personID / nameID in the path.
    /// </summary>
    PersonIdRequired = 2020,

    /// <summary>
    /// The country parameter must be ISO-3166-1 alpha 3.
    /// </summary>
    InvalidCountryParameter = 2050,

    /// <summary>
    /// Unknown request; available data may be requested for COUNTRIES.
    /// </summary>
    UnknownRequest = 2054,

    /// <summary>
    /// Unexpected debug connection from client.
    /// </summary>
    InvalidDebugParameter = 2055,

    /// <summary>
    /// Lineup already in account.
    /// </summary>
    DuplicateLineup = 2100,

    /// <summary>
    /// Lineup not in account; add it before requesting its mapping.
    /// </summary>
    LineupNotFound = 2101,

    /// <summary>
    /// Invalid lineup requested; check the country/postal code combination.
    /// </summary>
    UnknownLineup = 2102,

    /// <summary>
    /// Delete of a lineup that is not in the account.
    /// </summary>
    InvalidLineupDelete = 2103,

    /// <summary>
    /// Lineup must be formatted COUNTRY-LINEUP-DEVICE or COUNTRY-OTA-POSTALCODE.
    /// </summary>
    LineupWrongFormat = 2104,

    /// <summary>
    /// The requested lineup has been deleted from the server.
    /// </summary>
    LineupDeleted = 2106,

    /// <summary>
    /// The requested country is mis-typed or has no valid data.
    /// </summary>
    InvalidCountry = 2108,

    /// <summary>
    /// The requested personID does not exist.
    /// </summary>
    InvalidPersonId = 2109,

    /// <summary>
    /// The requested stationID is not in any of the configured lineups.
    /// </summary>
    StationIdNotFound = 2200,

    /// <summary>
    /// The requested stationID has been marked as deleted and will be purged.
    /// </summary>
    StationIdDeleted = 2201,

    /// <summary>
    /// Server offline for maintenance.
    /// </summary>
    ServiceOffline = 3000,

    /// <summary>
    /// Server is busy processing other requests. Retry.
    /// </summary>
    ServiceBusy = 3001,

    /// <summary>
    /// Account expired.
    /// </summary>
    AccountExpired = 4001,

    /// <summary>
    /// Password hash must be a lowercase 40 character sha1_hex of the password.
    /// </summary>
    InvalidHash = 4002,

    /// <summary>
    /// Invalid username or password.
    /// </summary>
    InvalidUser = 4003,

    /// <summary>
    /// Too many login failures. Locked for 15 minutes.
    /// </summary>
    AccountTempLock = 4004,

    /// <summary>
    /// Access to the account via JSON has been disabled; the user must contact SD support.
    /// </summary>
    AccountLocked = 4005,

    /// <summary>
    /// Token has expired. Request a new token.
    /// </summary>
    TokenExpired = 4006,

    /// <summary>
    /// Application not authorized to use the data service.
    /// </summary>
    AppLocked = 4007,

    /// <summary>
    /// Account inactive.
    /// </summary>
    AccountInactive = 4008,

    /// <summary>
    /// Exceeded the maximum number of logins in 24 hours; the user must contact SD support.
    /// </summary>
    MaxLoginAttempts = 4009,

    /// <summary>
    /// Exceeded the maximum number of unique IP addresses in 24 hours; the user must contact SD support.
    /// </summary>
    MaxIPAttempts = 4010,

    /// <summary>
    /// Exceeded the maximum number of lineup changes for today.
    /// </summary>
    MaxLineupChanges = 4100,

    /// <summary>
    /// Exceeded the number of lineups this account may hold.
    /// </summary>
    MaxLineups = 4101,

    /// <summary>
    /// No lineups have been added to this account.
    /// </summary>
    NoLineups = 4102,

    /// <summary>
    /// Could not find the requested image.
    /// </summary>
    ImageNotFound = 5000,

    /// <summary>
    /// Maximum image downloads reached. The counter resets at 00:00Z.
    /// </summary>
    MaxImageDownloads = 5002,

    /// <summary>
    /// Maximum image downloads for a trial user reached. The counter resets at 00:00Z.
    /// </summary>
    MaxImageDownloadsTrial = 5003,

    /// <summary>
    /// Exceeded the maximum number of invalid image URIs in 24 hours. The application is
    /// requesting URIs which do not exist and the account is about to be blocked.
    /// </summary>
    MaxInvalidImages = 5004,

    /// <summary>
    /// The requested programID does not exist. Permanent failure, do not ask again.
    /// </summary>
    InvalidProgramId = 6000,

    /// <summary>
    /// The requested programID is queued for generation at the server. Soft failure, retry later.
    /// </summary>
    ProgramQueued = 6001,

    /// <summary>
    /// The requested schedule should be available but was not found.
    /// </summary>
    ScheduleNotFound = 7000,

    /// <summary>
    /// The server cannot determine whether the requested schedule is valid; the user has to open
    /// a support ticket.
    /// </summary>
    InvalidScheduleRequest = 7010,

    /// <summary>
    /// The requested date is outside the range the server holds for that station.
    /// </summary>
    ScheduleRangeExceeded = 7020,

    /// <summary>
    /// The requested schedule is not in any of the configured lineups.
    /// </summary>
    ScheduleNotInLineup = 7030,

    /// <summary>
    /// The schedule is queued for generation and is not ready yet. The client must wait until the
    /// retryTime carried by the response before asking for it again.
    /// </summary>
    ScheduleQueued = 7100,

    /// <summary>
    /// Unknown error; the user has to open a support ticket.
    /// </summary>
    UnknownError = 9999
}
