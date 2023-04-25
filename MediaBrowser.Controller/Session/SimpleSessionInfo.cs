#nullable disable
using System;

namespace MediaBrowser.Controller.Session;

#pragma warning disable CS1591
/// <summary>
/// Class SimpleSessionInfo to display a subset of <see cref="SessionInfo"/>.
/// </summary>
public sealed class SimpleSessionInfo
{
    /// <summary>
    /// Gets or sets UserName.
    /// </summary>
    public string UserName { get; set; }

    /// <summary>
    /// Gets or sets content now playing.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets Device that is playing content.
    /// </summary>
    /// <value>The name of the device playing.</value>
    public string DeviceName { get; set; }

    /// <summary>
    /// Gets or sets the device id.
    /// </summary>
    /// <value>The device id.</value>
    public string DeviceId { get; set; }

    /// <summary>
    /// Gets or sets the last activity date.
    /// </summary>
    /// <value>The last activity date.</value>
    public DateTime LastActivityDate { get; set; }
}
