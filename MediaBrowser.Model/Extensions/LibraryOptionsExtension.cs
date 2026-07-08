using System;
using System.Linq;
using MediaBrowser.Model.Configuration;

namespace MediaBrowser.Model.Extensions;

/// <summary>
/// Extensions for <see cref="LibraryOptions"/>.
/// </summary>
public static class LibraryOptionsExtension
{
    /// <summary>
    /// Get the custom tag delimiters.
    /// </summary>
    /// <param name="options">This LibraryOptions.</param>
    /// <returns>CustomTagDelimiters in char[].</returns>
    public static char[] GetCustomTagDelimiters(this LibraryOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var delimiterList = options.CustomTagDelimiters
            .Where(x => !string.IsNullOrEmpty(x))
            .SelectMany(x => x.ToCharArray())
            .Distinct()
            .ToList();
        delimiterList.Add('\0');
        return delimiterList.ToArray();
    }
}
