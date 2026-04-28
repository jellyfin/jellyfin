using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MediaBrowser.Providers.Books.ComicBookInfo.Models;

/// <summary>
/// ComicBookInfo metadata.
/// </summary>
public class ComicBookInfoMetadata
{
    /// <summary>
    /// Gets or sets the series.
    /// </summary>
    [JsonPropertyName("series")]
    public string? Series { get; set; }

    /// <summary>
    /// Gets or sets the title.
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the publisher.
    /// </summary>
    [JsonPropertyName("publisher")]
    public string? Publisher { get; set; }

    /// <summary>
    /// Gets or sets the publication month.
    /// </summary>
    [JsonPropertyName("publicationMonth")]
    public int? PublicationMonth { get; set; }

    /// <summary>
    /// Gets or sets the publication year.
    /// </summary>
    [JsonPropertyName("publicationYear")]
    public int? PublicationYear { get; set; }

    /// <summary>
    /// Gets or sets the issue number.
    /// </summary>
    [JsonPropertyName("issue")]
    public int? Issue { get; set; }

    /// <summary>
    /// Gets or sets the number of issues.
    /// </summary>
    [JsonPropertyName("numberOfIssues")]
    public int? NumberOfIssues { get; set; }

    /// <summary>
    /// Gets or sets the volume number.
    /// </summary>
    [JsonPropertyName("volume")]
    public int? Volume { get; set; }

    /// <summary>
    /// Gets or sets the number of volumes.
    /// </summary>
    [JsonPropertyName("numberOfVolumes")]
    public int? NumberOfVolumes { get; set; }

    /// <summary>
    /// Gets or sets the rating.
    /// </summary>
    [JsonPropertyName("rating")]
    public int? Rating { get; set; }

    /// <summary>
    /// Gets or sets the genre.
    /// </summary>
    [JsonPropertyName("genre")]
    public string? Genre { get; set; }

    /// <summary>
    /// Gets or sets the language.
    /// </summary>
    [JsonPropertyName("language")]
    public string? Language { get; set; }

    /// <summary>
    /// Gets or sets the country.
    /// </summary>
    [JsonPropertyName("country")]
    public string? Country { get; set; }

    /// <summary>
    /// Gets or sets the list of credits.
    /// </summary>
    [JsonPropertyName("credits")]
    public IReadOnlyList<ComicBookInfoCredit> Credits { get; set; } = Array.Empty<ComicBookInfoCredit>();

    /// <summary>
    /// Gets or sets the list of tags.
    /// </summary>
    [JsonPropertyName("tags")]
    public IReadOnlyList<string> Tags { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the comments.
    /// </summary>
    [JsonPropertyName("comments")]
    public string? Comments { get; set; }
}
