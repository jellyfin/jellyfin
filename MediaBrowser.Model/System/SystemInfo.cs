#nullable disable

#pragma warning disable CA1819 // Properties should not return arrays
#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.ComponentModel;
using MediaBrowser.Model.Updates;

namespace MediaBrowser.Model.System;

/// <summary>
/// Class SystemInfo.
/// </summary>
public class SystemInfo : PublicSystemInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SystemInfo" /> class.
    /// </summary>
    public SystemInfo()
    {
        CompletedInstallations = Array.Empty<InstallationInfo>();
    }

    /// <summary>
    /// Gets or sets the display name of the operating system.
    /// </summary>
    /// <value>The display name of the operating system.</value>
    [Obsolete("This is no longer set")]
    public string OperatingSystemDisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the package name.
    /// </summary>
    /// <value>The value of the '-package' command line argument.</value>
    public string PackageName { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this instance has pending restart.
    /// </summary>
    /// <value><c>true</c> if this instance has pending restart; otherwise, <c>false</c>.</value>
    public bool HasPendingRestart { get; set; }

    public bool IsShuttingDown { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether [supports library monitor].
    /// </summary>
    /// <value><c>true</c> if [supports library monitor]; otherwise, <c>false</c>.</value>
    public bool SupportsLibraryMonitor { get; set; }

    /// <summary>
    /// Gets or sets the web socket port number.
    /// </summary>
    /// <value>The web socket port number.</value>
    public int WebSocketPortNumber { get; set; }

    /// <summary>
    /// Gets or sets the completed installations.
    /// </summary>
    /// <value>The completed installations.</value>
    public InstallationInfo[] CompletedInstallations { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this instance can self restart.
    /// </summary>
    /// <value><c>true</c>.</value>
    [Obsolete("This is always true")]
    [DefaultValue(true)]
    public bool CanSelfRestart { get; set; } = true;

    [Obsolete("This is always false")]
    [DefaultValue(false)]
    public bool CanLaunchWebBrowser { get; set; } = false;

    /// <summary>
    /// Gets or sets the program data path.
    /// </summary>
    /// <value>The program data path.</value>
    [Obsolete("Use the newer SystemStorageDto instead")]
    public string ProgramDataPath { get; set; }

    /// <summary>
    /// Gets or sets the web UI resources path.
    /// </summary>
    /// <value>The web UI resources path.</value>
    [Obsolete("Use the newer SystemStorageDto instead")]
    public string WebPath { get; set; }

    /// <summary>
    /// Gets or sets the items by name path.
    /// </summary>
    /// <value>The items by name path.</value>
    [Obsolete("Use the newer SystemStorageDto instead")]
    public string ItemsByNamePath { get; set; }

    /// <summary>
    /// Gets or sets the cache path.
    /// </summary>
    /// <value>The cache path.</value>
    [Obsolete("Use the newer SystemStorageDto instead")]
    public string CachePath { get; set; }

    /// <summary>
    /// Gets or sets the log path.
    /// </summary>
    /// <value>The log path.</value>
    [Obsolete("Use the newer SystemStorageDto instead")]
    public string LogPath { get; set; }

    /// <summary>
    /// Gets or sets the internal metadata path.
    /// </summary>
    /// <value>The internal metadata path.</value>
    [Obsolete("Use the newer SystemStorageDto instead")]
    public string InternalMetadataPath { get; set; }

    /// <summary>
    /// Gets or sets the transcode path.
    /// </summary>
    /// <value>The transcode path.</value>
    [Obsolete("Use the newer SystemStorageDto instead")]
    public string TranscodingTempPath { get; set; }

    /// <summary>
    /// Gets or sets the list of cast receiver applications.
    /// </summary>
    public IReadOnlyList<CastReceiverApplication> CastReceiverApplications { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this instance has update available.
    /// </summary>
    /// <value><c>true</c> if this instance has update available; otherwise, <c>false</c>.</value>
    [Obsolete("This should be handled by the package manager")]
    [DefaultValue(false)]
    public bool HasUpdateAvailable { get; set; }

    [Obsolete("This isn't set correctly anymore")]
    [DefaultValue("System")]
    public string EncoderLocation { get; set; } = "System";

    [Obsolete("This is no longer set")]
    [DefaultValue("X64")]
    public string SystemArchitecture { get; set; } = "X64";
}
