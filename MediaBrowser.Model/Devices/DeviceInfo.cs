using System;
using MediaBrowser.Model.Session;

namespace MediaBrowser.Model.Devices;

/// <summary>
/// A class for device Information.
/// </summary>
public class DeviceInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeviceInfo"/> class.
    /// </summary>
    public DeviceInfo()
    {
        Capabilities = new ClientCapabilities();
    }

    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    /// <value>The name.</value>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the custom name.
    /// </summary>
    /// <value>The custom name.</value>
    public string? CustomName { get; set; }

    /// <summary>
    /// Gets or sets the access token.
    /// </summary>
    /// <value>The access token.</value>
    public string? AccessToken { get; set; }

    /// <summary>
    /// Gets or sets the identifier.
    /// </summary>
    /// <value>The identifier.</value>
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the last name of the user.
    /// </summary>
    /// <value>The last name of the user.</value>
    public string? LastUserName { get; set; }

    /// <summary>
    /// Gets or sets the name of the application.
    /// </summary>
    /// <value>The name of the application.</value>
    public string? AppName { get; set; }

    /// <summary>
    /// Gets or sets the application version.
    /// </summary>
    /// <value>The application version.</value>
    public string? AppVersion { get; set; }

    /// <summary>
    /// Gets or sets the last user identifier.
    /// </summary>
    /// <value>The last user identifier.</value>
    public Guid? LastUserId { get; set; }

    /// <summary>
    /// Gets or sets the date last modified.
    /// </summary>
    /// <value>The date last modified.</value>
    public DateTime? DateLastActivity { get; set; }

    /// <summary>
    /// Gets or sets the capabilities.
    /// </summary>
    /// <value>The capabilities.</value>
    public ClientCapabilities Capabilities { get; set; }

    /// <summary>
    /// Gets or sets the icon URL.
    /// </summary>
    /// <value>The icon URL.</value>
    public string? IconUrl { get; set; }
}
