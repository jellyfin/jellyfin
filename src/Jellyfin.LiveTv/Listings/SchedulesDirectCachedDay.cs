using System.Collections.Generic;
using Jellyfin.LiveTv.Listings.SchedulesDirectDtos;

namespace Jellyfin.LiveTv.Listings;

/// <summary>
/// One station's schedule for one day, together with the details of its programs.
/// </summary>
/// <remarks>
/// Artwork is deliberately not cached: the spec states the image uri is ephemeral and must not be
/// stored, and artwork changes are not reflected in the schedule md5 this entry is keyed by.
/// </remarks>
public class SchedulesDirectCachedDay
{
    /// <summary>
    /// Gets or sets the md5 Schedules Direct reported for this day's schedule.
    /// </summary>
    public string? Md5 { get; set; }

    /// <summary>
    /// Gets or sets the schedule.
    /// </summary>
    public DayDto? Schedule { get; set; }

    /// <summary>
    /// Gets or sets the details of the programs in the schedule.
    /// </summary>
    public IReadOnlyList<ProgramDetailsDto> Programs { get; set; } = [];
}
