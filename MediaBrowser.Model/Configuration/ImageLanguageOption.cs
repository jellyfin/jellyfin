namespace MediaBrowser.Model.Configuration;

/// <summary>
/// Type of the image language option.
/// </summary>
public enum ImageLanguageType
{
    /// <summary>
    /// Specific language code.
    /// </summary>
    LanguageCode,

    /// <summary>
    /// No language.
    /// </summary>
    NoLanguage,

    /// <summary>
    /// Original language of the media.
    /// </summary>
    OriginalLanguage
}

/// <summary>
/// Class ImageLanguageOption.
/// </summary>
public class ImageLanguageOption
{
    /// <summary>
    /// Gets or sets the language code.
    /// Only relevant if OptionType is LanguageCode.
    /// </summary>
    public string? Language { get; set; }

    /// <summary>
    /// Gets or sets the type of this option.
    /// </summary>
    public ImageLanguageType OptionType { get; set; }
}
