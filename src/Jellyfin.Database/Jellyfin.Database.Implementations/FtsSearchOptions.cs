using System;

namespace Jellyfin.Database.Implementations;

/// <summary>
/// Options for configuring full-text search behavior.
/// </summary>
public class FtsSearchOptions
{
    /// <summary>
    /// Gets or sets the columns to search in.
    /// </summary>
    public string[] SearchableColumns { get; set; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether to use stemming (e.g., "running" matches "run").
    /// </summary>
    public bool UseStemming { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to use prefix matching (e.g., "run*" matches "running").
    /// </summary>
    public bool UsePrefixMatching { get; set; } = true;

    /// <summary>
    /// Gets or sets a limit.
    /// </summary>
    public int Limit { get; set; }
}
