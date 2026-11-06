namespace Jellyfin.Api.Models.TranscodeTestDtos;

/// <summary>
/// What to change in the settings after a check.
/// </summary>
public enum TranscodeTestAdvice
{
    /// <summary>
    /// Nothing to change.
    /// </summary>
    None,

    /// <summary>
    /// Turn the matching setting on.
    /// </summary>
    TurnOn,

    /// <summary>
    /// Turn the matching setting off.
    /// </summary>
    TurnOff
}
