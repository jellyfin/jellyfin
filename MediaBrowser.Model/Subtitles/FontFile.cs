using System;

namespace MediaBrowser.Model.Subtitles;

/// <summary>
/// Class FontFile.
/// </summary>
public class FontFile
{
    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    /// <value>The name.</value>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the size.
    /// </summary>
    /// <value>The size.</value>
    public long Size { get; set; }

    /// <summary>
    /// Gets or sets the date created.
    /// </summary>
    /// <value>The date created.</value>
    public DateTime DateCreated { get; set; }

    /// <summary>
    /// Gets or sets the date modified.
    /// </summary>
    /// <value>The date modified.</value>
    public DateTime DateModified { get; set; }
}
