namespace MediaBrowser.Model.Providers;

/// <summary>
/// Item external url.
/// </summary>
public class ExternalUrl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExternalUrl"/> class.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="url">The url.</param>
    public ExternalUrl(string name, string url)
    {
        Name = name;
        Url = url;
    }

    /// <summary>
    /// Gets the name.
    /// </summary>
    /// <remarks>This is the display name.</remarks>
    public string Name { get; }

    /// <summary>
    /// Gets the external url.
    /// </summary>
    public string Url { get; }
}
