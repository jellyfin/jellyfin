using System;

namespace MediaBrowser.Controller.SystemBackupService;

/// <summary>
/// Defines the optional contents of the backup archive.
/// </summary>
public class BackupOptionsDto
{
    /// <summary>
    /// Gets or sets a value indicating whether the archive contains the Metadata contents.
    /// </summary>
    public bool Metadata { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the archive contains the Trickplay contents.
    /// </summary>
    public bool Trickplay { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the archive contains the Subtitle contents.
    /// </summary>
    public bool Subtitles { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the archive contains the Database contents.
    /// </summary>
    public bool Database { get; set; } = true;
}
