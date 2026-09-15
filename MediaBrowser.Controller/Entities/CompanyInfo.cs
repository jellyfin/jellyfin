using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Data.Enums;

namespace MediaBrowser.Controller.Entities;

/// <summary>
/// A company credited on an item.
/// </summary>
public class CompanyInfo
{
    /// <summary>
    /// Gets or sets the name of the company.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the kind of company.
    /// </summary>
    public required CompanyKind Type { get; set; }

    /// <summary>
    /// Packs companies into the single column an item stores them in.
    /// </summary>
    /// <param name="companies">The companies.</param>
    /// <returns>The packed companies, or <c>null</c> when there are none.</returns>
    /// <remarks>
    /// One <c>Type:Name</c> entry per company, entries separated by <c>|</c>. As with the other
    /// packed columns a name containing the separator cannot be stored.
    /// </remarks>
    public static string? Pack(IEnumerable<CompanyInfo>? companies)
    {
        if (companies is null)
        {
            return null;
        }

        var packed = string.Join('|', companies
            .Where(e => !string.IsNullOrWhiteSpace(e.Name))
            .DistinctBy(e => (e.Type, e.Name.ToUpperInvariant()))
            .Select(e => e.Type + ":" + e.Name));

        return packed.Length == 0 ? null : packed;
    }

    /// <summary>
    /// Unpacks companies from the single column an item stores them in.
    /// </summary>
    /// <param name="packed">The packed companies.</param>
    /// <returns>The companies.</returns>
    public static CompanyInfo[] Unpack(string? packed)
    {
        if (string.IsNullOrEmpty(packed))
        {
            return [];
        }

        var entries = packed.Split('|', StringSplitOptions.RemoveEmptyEntries);
        var companies = new List<CompanyInfo>(entries.Length);
        foreach (var entry in entries)
        {
            // Only the first separator is the type; a name is free to contain more of them.
            var split = entry.IndexOf(':', StringComparison.Ordinal);
            if (split <= 0 || split == entry.Length - 1 || !Enum.TryParse<CompanyKind>(entry[..split], out var type))
            {
                continue;
            }

            companies.Add(new CompanyInfo { Name = entry[(split + 1)..], Type = type });
        }

        return companies.ToArray();
    }
}
