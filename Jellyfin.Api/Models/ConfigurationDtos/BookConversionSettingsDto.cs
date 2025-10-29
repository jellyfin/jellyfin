namespace Jellyfin.Api.Models.ConfigurationDtos;

/// <summary>
/// Book conversion settings dto.
/// </summary>
public class BookConversionSettingsDto
{
    /// <summary>
    /// Gets or sets a value indicating whether PDF to CBZ conversion is enabled.
    /// </summary>
    public bool EnablePdfToCbzConversion { get; set; }

    /// <summary>
    /// Gets or sets the DPI used for rasterizing PDF pages.
    /// </summary>
    public int PdfToCbzDpi { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the original PDF should be replaced by the CBZ.
    /// </summary>
    public bool PdfToCbzReplaceOriginal { get; set; }
}
