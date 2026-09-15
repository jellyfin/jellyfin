using System;
using Jellyfin.Data.Enums;

namespace MediaBrowser.Model.Dto;

/// <summary>
/// A company credited on an item, with the id of the company's own item.
/// </summary>
public class CompanyDto
{
    /// <summary>
    /// Gets or sets the id of the company.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the company.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the kind of company.
    /// </summary>
    public CompanyKind Type { get; set; }
}
