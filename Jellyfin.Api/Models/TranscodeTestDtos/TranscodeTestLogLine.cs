namespace Jellyfin.Api.Models.TranscodeTestDtos;

/// <summary>
/// One console line.
/// </summary>
public class TranscodeTestLogLine
{
    /// <summary>
    /// Gets or sets the seconds since the test started.
    /// </summary>
    public double Seconds { get; set; }

    /// <summary>
    /// Gets or sets the message.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}
