namespace Jellyfin.Data.Dtos;

/// <summary>
/// A dto representing custom options for a device.
/// </summary>
public class DeviceOptionsDto
{
    /// <summary>
    /// Gets or sets the id.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the device id.
    /// </summary>
    public string? DeviceId { get; set; }

    /// <summary>
    /// Gets or sets the custom name.
    /// </summary>
    public string? CustomName { get; set; }
}
