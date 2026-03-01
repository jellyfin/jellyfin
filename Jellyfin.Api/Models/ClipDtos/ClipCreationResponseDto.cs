using System.Text.Json.Serialization;

namespace Jellyfin.Api.Models.ClipDtos;

/// <summary>
/// Response returned by <c>POST /Videos/{itemId}/Clip</c>.
/// </summary>
public sealed class ClipCreationResponseDto
{
    /// <summary>Gets or sets the unique clip job identifier.</summary>
    [JsonPropertyName("clipId")]
    public required string ClipId { get; set; }

    /// <summary>Gets or sets the estimated clip duration in seconds.</summary>
    [JsonPropertyName("estimatedDurationSeconds")]
    public double EstimatedDurationSeconds { get; set; }
}
