namespace MediaBrowser.Model.Configuration;

/// <summary>
/// The metadata configuration DTO for API use.
/// </summary>
public class MetadataConfigurationDto
{
    /// <summary>
    /// Gets or sets a value indicating whether to use file creation time for date added.
    /// </summary>
    public bool UseFileCreationTimeForDateAdded { get; set; } = true;
}
