namespace MediaBrowser.Model.Lyrics;

/// <summary>
/// The information for a raw lyrics file before parsing.
/// </summary>
public class LyricFile
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LyricFile"/> class.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="content">The content, must not be empty.</param>
    public LyricFile(string name, string content)
    {
        Name = name;
        Content = content;
    }

    /// <summary>
    /// Gets or sets the name of the lyrics file. This must include the file extension.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the contents of the file.
    /// </summary>
    public string Content { get; set; }
}
