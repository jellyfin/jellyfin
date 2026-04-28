using System.Text.Json.Serialization;

namespace MediaBrowser.Providers.Books.ComicBookInfo.Models;

/// <summary>
/// ComicBookInfo credit.
/// </summary>
public class ComicBookInfoCredit
{
    /// <summary>
    /// Gets or sets the person name.
    /// </summary>
    [JsonPropertyName("person")]
    public string? Person { get; set; }

    /// <summary>
    /// Gets or sets the role.
    /// </summary>
    [JsonPropertyName("role")]
    public string? Role { get; set; }
}
