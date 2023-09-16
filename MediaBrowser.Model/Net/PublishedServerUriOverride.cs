namespace MediaBrowser.Model.Net;

/// <summary>
/// Class holding information for a published server URI override.
/// </summary>
public class PublishedServerUriOverride
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PublishedServerUriOverride"/> class.
    /// </summary>
    /// <param name="data">The <see cref="IPData"/>.</param>
    /// <param name="overrideUri">The override.</param>
    /// <param name="internalOverride">A value indicating whether the override is for internal requests.</param>
    /// <param name="externalOverride">A value indicating whether the override is for external requests.</param>
    public PublishedServerUriOverride(IPData data, string overrideUri, bool internalOverride, bool externalOverride)
    {
        Data = data;
        OverrideUri = overrideUri;
        IsInternalOverride = internalOverride;
        IsExternalOverride = externalOverride;
    }

    /// <summary>
    /// Gets or sets the object's IP address.
    /// </summary>
    public IPData Data { get; set; }

    /// <summary>
    /// Gets or sets the override URI.
    /// </summary>
    public string OverrideUri { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the override should be applied to internal requests.
    /// </summary>
    public bool IsInternalOverride { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the override should be applied to external requests.
    /// </summary>
    public bool IsExternalOverride { get; set; }
}
