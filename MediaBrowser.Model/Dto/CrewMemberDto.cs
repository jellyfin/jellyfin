using System;

namespace MediaBrowser.Model.Dto;

/// <summary>
/// Represents a crew member entry — one per person+role combination.
/// </summary>
public class CrewMemberDto
{
    /// <summary>
    /// Gets or sets the Id of the Person item, used to construct image URLs.
    /// </summary>
    public Guid PersonId { get; set; }

    /// <summary>
    /// Gets or sets the person's display name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the role type (Director, Producer, Writer, Composer, etc.).
    /// </summary>
    public string PersonType { get; set; } = string.Empty;
}
