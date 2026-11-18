using System.Text.Json.Serialization;

namespace MediaBrowser.Providers.Books.ComicBookInfo.Models;

/// <summary>
/// ComicBookInfo format.
/// </summary>
public class ComicBookInfoFormat
{
    /// <summary>
    /// Gets or sets the app ID.
    /// </summary>
    [JsonPropertyName("appID")]
    public string? AppId { get; set; }

    /// <summary>
    /// Gets or sets the last modified timestamp.
    /// </summary>
    [JsonPropertyName("lastModified")]
    public string? LastModified { get; set; }

    /// <summary>
    /// Gets or sets the metadata.
    /// </summary>
    [JsonPropertyName("ComicBookInfo/1.0")]
    public ComicBookInfoMetadata? Metadata { get; set; }
}
