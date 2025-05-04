namespace MediaBrowser.Model.System;

/// <summary>
/// The cast receiver application model.
/// </summary>
public class CastReceiverApplication
{
    /// <summary>
    /// Gets or sets the cast receiver application id.
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// Gets or sets the cast receiver application name.
    /// </summary>
    public required string Name { get; set; }
}
