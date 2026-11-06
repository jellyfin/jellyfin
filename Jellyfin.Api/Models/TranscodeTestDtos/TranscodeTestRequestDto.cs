namespace Jellyfin.Api.Models.TranscodeTestDtos;

/// <summary>
/// What a transcoding test should run.
/// </summary>
public class TranscodeTestRequestDto
{
    /// <summary>
    /// Gets or sets the kind of test.
    /// </summary>
    public TranscodeTestKind Kind { get; set; }
}
