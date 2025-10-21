namespace MediaBrowser.Model.Configuration;

/// <summary>
/// Sort direction for media sources.
/// </summary>
public enum MediaSourceSortOrder
{
    /// <summary>
    /// Sort by descending resolution (highest first).
    /// </summary>
    Descending = 0,

    /// <summary>
    /// Sort by ascending resolution (lowest first).
    /// </summary>
    Ascending = 1
}

/// <summary>
/// Configuration options for media source selection and ordering.
/// </summary>
public class MediaSourceOptions
{
    /// <summary>
    /// Gets or sets the sort order for media sources when multiple versions are available.
    /// </summary>
    public MediaSourceSortOrder SortOrder { get; set; } = MediaSourceSortOrder.Descending;

    /// <summary>
    /// Gets or sets the preferred video resolution height (e.g., 1080, 720, 2160).
    /// If set, this resolution will be prioritized as the first option, followed by others sorted by SortOrder.
    /// Null means no preference.
    /// </summary>
    public int? PreferredVideoHeight { get; set; }
}
