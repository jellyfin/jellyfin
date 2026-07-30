#pragma warning disable CS1591, SA1402, SA1649

using System;
using System.Collections.Generic;

namespace MediaBrowser.Controller.CustomNetflix;

public sealed class CustomNetflixItemFeedbackRequest
{
    public string Feedback { get; set; } = string.Empty;
}

public sealed class CustomNetflixItemFeedbackDto
{
    public Guid ProfileId { get; set; }

    public Guid ItemId { get; set; }

    public string? Feedback { get; set; }

    public DateTime? UpdatedAt { get; set; }
}

public sealed class CustomNetflixMediaSegmentCoverageDto
{
    public DateTime GeneratedAt { get; set; }

    public int EligibleItems { get; set; }

    public IReadOnlyList<CustomNetflixMediaSegmentTypeCoverageDto> Types { get; set; } =
        Array.Empty<CustomNetflixMediaSegmentTypeCoverageDto>();
}

public sealed class CustomNetflixMediaSegmentTypeCoverageDto
{
    public string Type { get; set; } = string.Empty;

    public int CoveredItems { get; set; }

    public double CoveragePercent { get; set; }
}
