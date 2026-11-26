using System.Collections.Generic;
using System.Diagnostics.Metrics;
using MediaBrowser.Common.Telemetry;

namespace MediaBrowser.Controller.Telemetry;

/// <summary>
/// Authentication instruments published on <see cref="JellyfinTelemetry.Meter"/>.
/// </summary>
public static class AuthenticationMetrics
{
    /// <summary>
    /// The authentication request was granted.
    /// </summary>
    public const string OutcomeSucceeded = "succeeded";

    /// <summary>
    /// The user name or the password did not match.
    /// </summary>
    public const string OutcomeInvalidCredentials = "invalid_credentials";

    /// <summary>
    /// The credentials were valid but the user is not allowed to start this session.
    /// </summary>
    public const string OutcomeNotPermitted = "not_permitted";

    /// <summary>
    /// The request failed for a reason other than the credentials.
    /// </summary>
    public const string OutcomeError = "error";

    private const string OutcomeTag = "jellyfin.authentication.outcome";

    private static readonly Counter<long> _attempts = JellyfinTelemetry.Meter.CreateCounter<long>(
        "jellyfin.authentication.attempts",
        "{attempt}",
        "Authentication requests, by outcome.");

    private static readonly Counter<long> _lockouts = JellyfinTelemetry.Meter.CreateCounter<long>(
        "jellyfin.authentication.lockouts",
        "{user}",
        "Users disabled after too many failed logins.");

    /// <summary>
    /// Records the outcome of an authentication request.
    /// </summary>
    /// <param name="outcome">One of the outcome constants on this class.</param>
    public static void OnAuthenticationAttempt(string outcome)
        => _attempts.Add(1, new KeyValuePair<string, object?>(OutcomeTag, outcome));

    /// <summary>
    /// Records that a user was locked out after too many failed logins.
    /// </summary>
    public static void OnUserLockedOut() => _lockouts.Add(1);
}
